using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 단검 인벤토리 + 던지기/회수. 던진 단검은 GridDagger 엔티티로 맵에 남음.
/// 회수는 GridPlayerMovement가 이동 시 TryRetrieveAt 호출.
/// </summary>
public class GridPlayerCombat : MonoBehaviour
{
    /// <summary>
    /// 공격이 실제로 성공했을 때 호출된다.
    /// 사용 예: GridPlayerAnimationController가 구독해서 공격 애니메이션을 재생한다.
    /// </summary>
    public event Action<Vector2Int> OnAttackPerformed;

    [Header("기본 무기")]
    [Tooltip("플레이어가 던지고 회수할 단검 데이터입니다. 시작 개수, 내구도, 피해량, 스프라이트를 여기서 읽습니다.")]
    [SerializeField] private WeaponData daggerData;

    [Tooltip("던지기 성공 시 맵 위에 생성할 단검 엔티티 프리팹입니다.")]
    [SerializeField] private GridDagger daggerPrefab;

    [Tooltip("인접한 적을 근접 공격할 때 사용하는 기본 피해량입니다. 실제 피해는 대상 방어 계산을 거칠 수 있습니다.")]
    [SerializeField] private int meleeDamage = 1;

    [Header("Attack Speed")]
    [Tooltip("GDD 기준 근접 공격 기본 딜레이입니다. AttackSpeed 1일 때 이 값을 그대로 사용합니다.")]
    [SerializeField, Min(0.01f)] private float baseMeleeCooldown = 1.0f;

    [Tooltip("AttackSpeed가 1보다 높을 때, 1 증가당 근접 공격 쿨다운과 행동 딜레이를 줄이는 비율입니다.")]
    [SerializeField, Min(0f)] private float attackSpeedCooldownMultiplier = 0.12f;

    [Tooltip("AttackSpeed가 높아져도 근접 공격 쿨다운이 이 값보다 짧아지지 않게 막습니다.")]
    [SerializeField, Min(0.01f)] private float minMeleeCooldown = 0.1f;

    [Tooltip("AttackSpeed가 높아져도 근접 공격 시간 비용이 이 값보다 낮아지지 않게 막습니다.")]
    [SerializeField, Min(0.01f)] private float minMeleeTimeCost = 0.1f;

    [Header("Debug")]
    [Tooltip("켜면 플레이어가 공격으로 준 피해량을 Console에 출력합니다.")]
    [SerializeField] private bool logDamageDealt = true;

    // 인벤토리: 각 단검의 현재 내구도 리스트
    private readonly List<int> _inventory = new();

    // 맵에 떨어진 단검 위치 → 단검 엔티티 매핑
    private readonly Dictionary<Vector2Int, GridDagger> _thrownDaggers = new();

    private GridPlayer _player;
    private NYHPlayerStats _stats;
    private float _nextMeleeAttackTime;

    public int InventoryCount => _inventory.Count;
    public IReadOnlyList<int> InventoryDurabilities => _inventory;

    /// <summary>
    /// GridPlayer에서 호출하는 초기화 진입점입니다.
    /// 시작 단검을 인벤토리에 넣고, 전투 계산에 필요한 플레이어 참조를 보관합니다.
    /// </summary>
    public void Init(GridPlayer player)
    {
        _player = player;
        _stats = GetComponent<NYHPlayerStats>();
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;

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
        if (_player.ActionDelay != null && !_player.ActionDelay.CanAct)
            return;

        if (!CanMeleeAttack())
            return;

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
        if (entity is IDamageable damageable && (UnityEngine.Object)damageable != _player)
        {
            int damage = GetModifiedOutgoingDamage(meleeDamage);
            int beforeHp = damageable.CurrentHp;
            damageable.TakeDamage(damage);
            LogDamageDealt("근접 공격", damageable, damage, beforeHp);
            StartMeleeCooldown();
            OnAttackPerformed?.Invoke(dir);
            float actionDelay = GetModifiedMeleeTimeCost();
            _player.ActionDelay?.StartDelay(actionDelay);
            TurnManager.Instance.OnPlayerActionCompleted(actionDelay);
        }
        // 적 없으면 빈 휘두름 — 시간 소모 X (회전만 함)
    }

    /// <summary>
    /// 현재 바라보는 방향으로 단검을 던집니다.
    /// 단검이 없거나 배치할 칸이 막혀 있으면 실패하고, 성공 시 행동 비용을 TurnManager에 전달합니다.
    /// </summary>
    public void TryThrow()
    {
        if (_player.ActionDelay != null && !_player.ActionDelay.CanAct)
            return;

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
            int damage = GetModifiedOutgoingDamage(daggerData.damage);
            int beforeHp = hitTarget.CurrentHp;
            hitTarget.TakeDamage(damage);
            LogDamageDealt("단검 투척", hitTarget, damage, beforeHp);
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
        OnAttackPerformed?.Invoke(facingDir);
        float actionDelay = _player.Config.throwTimeDelay;
        _player.ActionDelay?.StartDelay(actionDelay);
        TurnManager.Instance.OnPlayerActionCompleted(actionDelay);
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
            if (entity is IDamageable damageable && (UnityEngine.Object)damageable != _player)
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

    private bool CanMeleeAttack()
    {
        return Time.time >= _nextMeleeAttackTime;
    }

    /// <summary>
    /// AttackSpeed 스탯을 반영해 다음 근접 공격 가능 시각을 계산합니다.
    /// 값이 높을수록 쿨다운이 짧아지지만 minMeleeCooldown 아래로는 내려가지 않습니다.
    /// </summary>
    private void StartMeleeCooldown()
    {
        _nextMeleeAttackTime = Time.time + GetMeleeCooldown();
    }

    private float GetMeleeCooldown()
    {
        float cooldown = Mathf.Max(minMeleeCooldown, baseMeleeCooldown / GetAttackSpeedFactor());
        StatusController statusController = _player.GetComponent<StatusController>();
        return statusController != null ? statusController.ModifyAttackCooldown(cooldown) : cooldown;
    }

    private float GetModifiedMeleeTimeCost()
    {
        float timeCost = Mathf.Max(minMeleeTimeCost, _player.Config.meleeTimeDealy / GetAttackSpeedFactor());
        StatusController statusController = _player.GetComponent<StatusController>();
        return statusController != null ? statusController.ModifyAttackCooldown(timeCost) : timeCost;
    }

    private float GetAttackSpeedFactor()
    {
        int attackSpeed = GetAttackSpeed();
        return 1f + Mathf.Max(0, attackSpeed - 1) * attackSpeedCooldownMultiplier;
    }

    private int GetAttackSpeed()
    {
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;

        return _stats != null ? _stats.AttackSpeed : 0;
    }

    /// <summary>
    /// 요청 피해량과 실제 HP 감소량을 비교해서 디버그 로그로 남깁니다.
    /// 방어/회피 때문에 실제 피해가 달라지는 상황을 확인할 때 씁니다.
    /// </summary>
    private void LogDamageDealt(string attackName, IDamageable target, int requestedDamage, int beforeHp)
    {
        if (!logDamageDealt || target == null) return;

        int hpDelta = Mathf.Max(0, beforeHp - target.CurrentHp);
        string targetName = target is UnityEngine.Object targetObject ? targetObject.name : target.GetType().Name;

        Debug.Log($"[PlayerAttack] {attackName} -> {targetName}, 요청 피해 {requestedDamage}, 실제 HP 감소 {hpDelta}, HP {target.CurrentHp}/{target.MaxHp}");
    }
}
