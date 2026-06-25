using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrototypeRT;

/// <summary>
/// 칼 든 고블린 도적 (EnemyGoblinThief) [Common] — Area 2 (던전, 6~10층)
///
/// 특이 행동 패턴:
///   1. 한 턴에 2칸 고속 추적 이동 (박쥐와 동일 메커니즘)
///   2. 공격 명중 시 플레이어 인벤토리에서 랜덤 아이템 1개 훔침
///   3. 아이템 탈취 성공 후 즉시 도주 AI로 전환 (플레이어에게서 멀어짐)
///   4. 고블린 사살 시 훔쳐간 아이템 인벤토리로 자동 복구
///
/// override 목록:
///   ExecutePendingAttack()  → 공격 + TryStealItem 결합
///   Die()                   → 아이템 복구 후 부모 Die 호출
///   PerformMovement()       → 2칸 고속 이동 + 도주/추적 AI 분기
///   HasCustomMovementVisual() → true (Walk 수동 제어)
/// </summary>
public class EnemyGoblinThief : EnemyBase
{
    /// <summary> 아이템 탈취 성공 여부. true이면 도주 AI 활성화. </summary>
    private bool _hasStolenItem = false;

    /// <summary> 현재 보관 중인 훔친 아이템 데이터 참조. 사망 시 복구에 사용. </summary>
    private ItemInstance _stolenItem = null;

    /// <summary> 이번 턴 시각 보간용 이동 경로 버퍼. 0번은 출발지. </summary>
    private readonly List<Vector2Int> _movePath = new List<Vector2Int>();

    /// <summary> 이동 코루틴 참조 </summary>
    private Coroutine _moveCoroutine;

    /// <summary>
    /// 공격 발동 override.
    /// 기본 공격 + 훔치기 미완료 상태이면 TryStealItem 시도.
    /// </summary>
    protected override IEnumerator ExecutePendingAttack(Vector2Int targetCell, float delaySeconds)
    {
        _isAttacking = true;
        ShowTelegraph(targetCell);

        yield return new WaitForSeconds(delaySeconds);

        if (!IsDead)
        {
            UpdateSpriteDirection(targetCell);
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }
            if (Player.GridPosition == targetCell)
            {
                int damage = RollAttackDamage();
                if (TryHitPlayer(damage) && !_hasStolenItem)
                {
                    // 빗나간 공격에는 아이템 탈취가 일어나지 않도록 명중 성공 뒤에만 시도합니다.
                    TryStealItem();
                }
            }
        }
        ClearTelegraphs();
        _isAttacking = false;
        _pendingAttackCell = null;
    }

    /// <summary>
    /// 플레이어 인벤토리(InventoryController)에서 랜덤 아이템 1개 제거 후 _stolenItemData에 기록.
    /// 인벤토리가 비어있으면 아무것도 안 함.
    /// 성공 시 _hasStolenItem = true → 이후 PerformMovement에서 도주 AI 발동.
    /// </summary>
    private void TryStealItem()
    {
        var invController = FindFirstObjectByType<InventoryController>();
        if (invController == null)
        {
            Debug.LogWarning($"[GoblinThief] InventoryController를 찾을 수 없음!");
            return;
        }
        if (invController.Items.Count == 0)
        {
            Debug.Log($"[GoblinThief] 인벤토리 비어있어 탈취 불가.");
            return;
        }

        // 임의의 슬롯 선택
        int index = Random.Range(0, invController.Items.Count);
        var targetItem = invController.Items[index];

        if (targetItem != null && targetItem.ItemInstance != null)
        {
            _stolenItem = targetItem.ItemInstance;

            bool removed = invController.TryRemoveItem(targetItem);
            if (removed)
            {
                // 인벤토리 UI 오브젝트도 같이 소멸
                Destroy(targetItem.gameObject);
                _hasStolenItem = true;
                Debug.Log($"[GoblinThief] {name}이(가) 플레이어의 {_stolenItem.data.Name} 훔쳐 도망 시작.");
            }
        }
    }

    /// <summary>
    /// 사망 override.
    /// 훔친 아이템이 있으면 현재 그리드 위치에 바닥 드롭 후 부모 Die() 호출.
    /// 플레이어가 직접 주워야 아이템을 회수할 수 있습니다.
    /// </summary>
    protected override void Die()
    {
        if (_hasStolenItem && _stolenItem != null)
        {
            var groundItemController = FindFirstObjectByType<GroundItemController>();
            if (groundItemController != null)
            {
                groundItemController.DropItem(_stolenItem, 1, GridPosition);
                Debug.Log($"[GoblinThief] {name} 처치 → {_stolenItem.data.Name}을(를) {GridPosition}에 드롭.");
            }
            else
            {
                Debug.LogError($"[GoblinThief] {name} 처치 후 GroundItemController가 없어 {_stolenItem.data.Name} 영구 분실!");
            }
        }

        base.Die();
    }

    /// <summary>
    /// 이동 override.
    /// 박쥐와 동일하게 2칸 논리 이동 후 SmoothMoveVisual 코루틴으로 시각 보간.
    /// 이동 방향은 도주/추적 AI 상태에 따라 GetNextStepDirection()이 결정.
    /// </summary>
    protected override void PerformMovement()
    {
        _movePath.Clear();
        _movePath.Add(GridPosition);

        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            for (int i = 0; i < 2; i++)
            {
                if (TryStepLogical())
                {
                    _movePath.Add(GridPosition);
                }
            }
        }
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
        }
        _moveCoroutine = StartCoroutine(SmoothMoveVisual(0.08f));
    }

    /// <summary>
    /// 논리 좌표만 즉시 갱신. 시각 이동은 SmoothMoveVisual에서 처리.
    /// </summary>
    private bool TryStepLogical()
    {
        Vector2Int dir = GetNextStepDirection();
        if (dir == Vector2Int.zero) return false;

        Vector2Int next = GridPosition + dir;

        if (!GridSystem.Instance.IsWalkable(next)) return false;
        if (next == Player.GridPosition) return false;
        //if (TurnManager.Instance.GetEntityAt(next) != null) return false;
        if (GridSystem.Instance.IsCellOccupied(next)) return false; // 타 엔티티
        if (GridSystem.Instance != null)
        {
            GridSystem.Instance.UnRegisterOccupancy(GridPosition);
            GridSystem.Instance.RegisterOccupancy(next, this);
        }
        GridPosition = next;
        TrapTerrainController.TryApplyTerrainAt(this, next);
        return true;
    }

    /// <summary>
    /// _movePath를 따라 월드 좌표를 delay(초) 간격으로 순차 스냅 이동.
    /// Walk 파라미터를 이 코루틴이 직접 켜고 끔.
    /// </summary>
    private IEnumerator SmoothMoveVisual(float delay)
    {
        if (_animator != null) _animator.SetBool("Walk", true);
        for (int i = 1; i < _movePath.Count; i++)
        {
            Vector3 targetWorldPos = GridSystem.Instance.GridToWorld(_movePath[i]);
            transform.position = targetWorldPos;
            UpdateVisibility();
            UpdateSpriteDirection(_movePath[i]);
            yield return new WaitForSeconds(delay);
        }
        if (_animator != null) _animator.SetBool("Walk", false);
    }

    /// <summary>
    /// 현재 AI 상태에 따라 이동 방향 벡터 결정.
    /// _hasStolenItem == true → 도주 (GetEscapeStepDirection)
    /// _hasStolenItem == false → 추적 (GetChaseStepDirection)
    /// </summary>
    private Vector2Int GetNextStepDirection()
    {
        if (_hasStolenItem)
        {
            return GetEscapeStepDirection(); // 도주 AI
        }
        else
        {
            return GetChaseStepDirection();  // 추적 AI
        }
    }

    /// <summary>
    /// 플레이어를 향해 접근하는 방향 벡터 반환.
    /// X/Y 중 더 먼 축을 주축으로 먼저 시도.
    /// </summary>
    private Vector2Int GetChaseStepDirection()
    {
        var diff = Player.GridPosition - GridPosition;
        if (diff == Vector2Int.zero) return Vector2Int.zero;

        var dirX = diff.x != 0 ? new Vector2Int(System.Math.Sign(diff.x), 0) : Vector2Int.zero;
        var dirY = diff.y != 0 ? new Vector2Int(0, System.Math.Sign(diff.y)) : Vector2Int.zero;

        bool xPrimary = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);
        var primary = xPrimary ? dirX : dirY;
        var secondary = xPrimary ? dirY : dirX;

        if (primary != Vector2Int.zero && CanMoveTo(GridPosition + primary)) return primary;
        if (secondary != Vector2Int.zero && CanMoveTo(GridPosition + secondary)) return secondary;
        return Vector2Int.zero;
    }

    /// <summary>
    /// 플레이어와의 거리를 최대로 늘리는 방향 벡터 반환.
    /// 4방향을 셔플 후 순회하여 이동 후 거리가 가장 큰 방향을 선택.
    /// 갈 곳이 완전히 막히면 Vector2Int.zero 반환.
    /// </summary>
    private Vector2Int GetEscapeStepDirection()
    {
        Vector2Int bestDir = Vector2Int.zero;
        float maxDist = (Player.GridPosition - GridPosition).sqrMagnitude;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        // 같은 거리에서 항상 같은 방향으로만 도망치지 않도록 셔플
        for (int i = 0; i < dirs.Length; i++)
        {
            int randIdx = Random.Range(i, dirs.Length);
            var temp = dirs[i];
            dirs[i] = dirs[randIdx];
            dirs[randIdx] = temp;
        }

        foreach (var dir in dirs)
        {
            Vector2Int next = GridPosition + dir;
            if (CanMoveTo(next))
            {
                float dist = (Player.GridPosition - next).sqrMagnitude;
                if (dist > maxDist) // 플레이어와 더 멀어지는 방향
                {
                    maxDist = dist;
                    bestDir = dir;
                }
            }
        }

        if (bestDir != Vector2Int.zero) return bestDir;

        // 더 멀어질 수 없는 막힌 상황 → 현재 거리와 같은 방향으로 이동
        foreach (var dir in dirs)
        {
            Vector2Int next = GridPosition + dir;
            if (CanMoveTo(next))
            {
                float dist = (Player.GridPosition - next).sqrMagnitude;
                if (dist == maxDist) return dir;
            }
        }

        return Vector2Int.zero; // 완전히 갇힌 경우
    }

    private bool CanMoveTo(Vector2Int pos)
    {
        if (!GridSystem.Instance.IsWalkable(pos)) return false;
        if (pos == Player.GridPosition) return false;
        // if (TurnManager.Instance.GetEntityAt(pos) != null) return false;
        if (GridSystem.Instance.IsCellOccupied(pos)) return false; // 타 엔티티
        return true;
    }

    /// <summary>
    /// SmoothMoveVisual이 Walk 파라미터를 직접 제어하므로 true 반환.
    /// </summary>
    protected override bool HasCustomMovementVisual() => true;
}
