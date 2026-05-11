using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 단검 인벤토리 + 던지기/회수. 던진 단검은 GridDagger 엔티티로 맵에 남음.
/// 회수는 GridPlayerMovement가 이동 시 TryRetrieveAt 호출.
/// </summary>
public class GridPlayerCombat : MonoBehaviour
{
    [SerializeField] private WeaponData daggerData;
    [SerializeField] private GridDagger daggerPrefab;
    [SerializeField] private int meleeDamage = 1;

    // 인벤토리: 각 단검의 현재 내구도 리스트
    private readonly List<int> _inventory = new();

    // 맵에 떨어진 단검 위치 → 단검 엔티티 매핑
    private readonly Dictionary<Vector2Int, GridDagger> _thrownDaggers = new();

    private GridPlayer _player;

    public int InventoryCount => _inventory.Count;
    public IReadOnlyList<int> InventoryDurabilities => _inventory;

    public void Init(GridPlayer player)
    {
        _player = player;

        if (daggerData == null)
        {
            Debug.LogError("GridPlayerCombat: WeaponData(dagger)가 할당되지 않았습니다.");
            return;
        }

        // 시작 단검 채우기 (durability 모두 max)
        for (int i = 0; i < daggerData.startCount; i++)
            _inventory.Add(daggerData.maxDurability);
    }

    /// <summary>
    /// 마우스 클릭 위치를 받아 상하좌우 중 우세축으로 스냅 → 인접 칸 근접 공격.
    /// 적 적중 시 데미지 + 시간 소모. 빈 셀이면 시간 소모 없이 facing만 갱신.
    /// </summary>
    public void TryMeleeAt(Vector2Int clickedCell)
    {
        var diff = clickedCell - _player.GridPosition;
        if (diff == Vector2Int.zero) return;

        // 우세축 판정 — |dx| >= |dy|면 가로, 아니면 세로 (대각선 정확히 45도면 가로 우선)
        Vector2Int dir = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y)
            ? new Vector2Int(System.Math.Sign(diff.x), 0)
            : new Vector2Int(0, System.Math.Sign(diff.y));

        if (dir == Vector2Int.zero) return;  // diff.x == 0 && diff.y == 0 방어

        _player.Movement.SetFacing(dir);

        var target = _player.GridPosition + dir;

        var entity = TurnManager.Instance.GetEntityAt(target);
        if (entity is IDamageable damageable && (Object)damageable != _player)
        {
            damageable.TakeDamage(GetModifiedOutgoingDamage(meleeDamage));
            TurnManager.Instance.OnPlayerActionCompleted(_player.Config.meleeTimeCost);
        }
        // 적 없으면 빈 휘두름 — 시간 소모 X (회전만 함)
    }

    public void TryThrow()
    {
        if (_inventory.Count == 0) return;

        // 시야 안 모든 적 중 가장 가까운 타겟 (대각선 포함)
        if (!FindBestAutoAimTarget(out var landingCell, out var hitTarget))
        {
            // 시야 안에 적 없음 — 단검/시간 소모 X, cancel
            return;
        }

        var origin = _player.GridPosition;

        // 인벤토리에서 단검 1개 꺼냄 (현재 내구도 보존)
        int durability = _inventory[0];
        _inventory.RemoveAt(0);

        // 데미지는 즉시 적용 (비행은 순수 시각 효과)
        if (hitTarget != null)
        {
            hitTarget.TakeDamage(GetModifiedOutgoingDamage(daggerData.damage));
        }

        // facing 갱신 — 8방향 sign으로 정규화
        var diff = landingCell - origin;
        var facingDir = new Vector2Int(System.Math.Sign(diff.x), System.Math.Sign(diff.y));
        _player.Movement.SetFacing(facingDir);

        // 단검 인스턴스화 + 비행 시작 (착지 후 콜백으로 사전 등록)
        if (daggerPrefab != null)
        {
            var dagger = Instantiate(daggerPrefab);
            var fromWorld = GridSystem.Instance.GridToWorld(origin);
            var landing = landingCell;
            var daggerRef = dagger;
            dagger.Throw(fromWorld, landing, durability, daggerData.sprite, () =>
            {
                _thrownDaggers[landing] = daggerRef;
            });
        }

        GameEvents.OnDaggerThrown?.Invoke(origin, landingCell);
        TurnManager.Instance.OnPlayerActionCompleted(_player.Config.throwTimeCost);
    }

    /// <summary>
    /// 이동 시 도착 칸에 단검이 있으면 회수. 회수 성공 시 true.
    /// 내구도 -1, 0 이하면 사라짐. 호출자가 추가 시간 비용 합산.
    /// </summary>
    public bool TryRetrieveAt(Vector2Int cell)
    {
        if (!_thrownDaggers.TryGetValue(cell, out var dagger)) return false;

        int newDurability = dagger.Durability - 1;
        _thrownDaggers.Remove(cell);
        Destroy(dagger.gameObject);

        if (newDurability > 0)
        {
            _inventory.Add(newDurability);
        }

        GameEvents.OnDaggerRetrieved?.Invoke(cell);
        return true;
    }

    /// <summary>
    /// 시야 안의 모든 IDamageable 적 중 가장 가까운 (Euclidean^2) 타겟 선택.
    /// 대각선 포함. 시야는 LOS 기반이라 벽이 가로막은 적은 자연 제외됨.
    /// 시야 안 적 없으면 false.
    /// </summary>
    private bool FindBestAutoAimTarget(out Vector2Int landingCell, out IDamageable hitTarget)
    {
        landingCell = _player.GridPosition;
        hitTarget = null;

        if (VisionSystem.Instance == null || TurnManager.Instance == null) return false;

        float bestDistSq = float.MaxValue;
        Vector2Int bestCell = Vector2Int.zero;
        IDamageable bestTarget = null;

        foreach (var cell in VisionSystem.Instance.Visible)
        {
            if (cell == _player.GridPosition) continue;

            var entity = TurnManager.Instance.GetEntityAt(cell);
            if (entity is IDamageable damageable && (Object)damageable != _player)
            {
                var diff = cell - _player.GridPosition;
                float distSq = diff.x * diff.x + diff.y * diff.y;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestCell = cell;
                    bestTarget = damageable;
                }
            }
        }

        if (bestTarget == null) return false;

        landingCell = bestCell;
        hitTarget = bestTarget;
        return true;
    }

    private int GetModifiedOutgoingDamage(int baseDamage)
    {
        StatusController statusController = _player.GetComponent<StatusController>();
        return statusController != null ? statusController.ModifyOutgoingDamage(baseDamage) : baseDamage;
    }
}
