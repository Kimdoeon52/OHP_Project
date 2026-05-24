using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrototypeRT;

/// <summary>
/// 칼 든 고블린 [Common] — Area 2 (던전, 6~10층)
/// 
/// 이동: 고속 (moveSpeed=0.4) — 한 턴에 2칸 이동 (Crawler 고속 이동 재활용)
/// 공격: 근접 1칸
/// 특수: 명중 시 인벤토리 아이템 1개 랜덤 훔침 → 도주 AI (플레이어 반대 방향 이동)
///       사망 시 훔친 아이템을 플레이어 인벤토리로 복구
/// </summary>
public class EnemyGoblinThief : EnemyBase
{
    private bool _hasStolenItem = false;
    private ItemBase _stolenItemData = null;
    private readonly List<Vector2Int> _movePath = new();

    protected override void ExecutePendingAttack()
    {
        if (_pendingAttackCell.HasValue && Player.GridPosition == _pendingAttackCell.Value)
        {
            int damage = RollAttackDamage();
            Player.TakeDamage(damage);
            TryApplyOnHitEffect(Player);

            // 훔친 아이템이 없으면 명중 시 랜덤 훔침
            if (!_hasStolenItem)
            {
                TryStealItem();
            }
        }
        _pendingAttackCell = null;
        ClearTelegraphs();
    }

    private void TryStealItem()
    {
        var invController = FindFirstObjectByType<InventoryController>();
        if (invController == null || invController.Items.Count == 0) return;

        // 플레이어 인벤토리에서 랜덤 아이템 인덱스 선택
        int index = Random.Range(0, invController.Items.Count);
        var targetItem = invController.Items[index];

        if (targetItem != null && targetItem.ItemData != null)
        {
            _stolenItemData = targetItem.ItemData;
            bool removed = invController.TryRemoveItem(targetItem);
            if (removed)
            {
                Destroy(targetItem.gameObject);
                _hasStolenItem = true;
                Debug.Log($"[GoblinThief] {name}이(가) 플레이어의 {_stolenItemData.Name} 아이템을 훔쳐 도망칩니다!");
            }
        }
    }

    protected override void Die()
    {
        // 사망 시 훔친 아이템 돌려주기
        if (_hasStolenItem && _stolenItemData != null)
        {
            var invController = FindFirstObjectByType<InventoryController>();
            if (invController != null)
            {
                bool added = invController.TryAddItem(_stolenItemData);
                if (added)
                {
                    Debug.Log($"[GoblinThief] {name}을(를) 처치하여 훔쳐갔던 {_stolenItemData.Name}을(를) 되찾았습니다!");
                }
                else
                {
                    Debug.LogWarning($"[GoblinThief] {name}을(를) 처치했으나 인벤토리가 가득 차 {_stolenItemData.Name}을(를) 되찾지 못했습니다.");
                }
            }
        }

        base.Die();
    }

    protected override void PerformMovement()
    {
        _movePath.Clear();
        _movePath.Add(GridPosition);

        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            // 한 턴에 2번 이동 시도 (고속 이동)
            for (int i = 0; i < 2; i++)
            {
                if (TryStepLogical())
                {
                    _movePath.Add(GridPosition);
                }
            }
        }

        StopAllCoroutines();
        StartCoroutine(SmoothMoveVisual(0.08f));
    }

    private bool TryStepLogical()
    {
        Vector2Int dir = GetNextStepDirection();
        if (dir == Vector2Int.zero) return false;

        Vector2Int next = GridPosition + dir;

        if (!GridSystem.Instance.IsWalkable(next)) return false;
        if (next == Player.GridPosition) return false;
        if (TurnManager.Instance.GetEntityAt(next) != null) return false;

        GridPosition = next;
        return true;
    }

    private IEnumerator SmoothMoveVisual(float delay)
    {
        for (int i = 1; i < _movePath.Count; i++)
        {
            Vector3 targetWorldPos = GridSystem.Instance.GridToWorld(_movePath[i]);
            transform.position = targetWorldPos;
            UpdateVisibility();
            yield return new WaitForSeconds(delay);
        }
    }

    private Vector2Int GetNextStepDirection()
    {
        if (_hasStolenItem)
        {
            return GetEscapeStepDirection();
        }
        else
        {
            return GetChaseStepDirection();
        }
    }

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

    private Vector2Int GetEscapeStepDirection()
    {
        Vector2Int bestDir = Vector2Int.zero;
        float maxDist = (Player.GridPosition - GridPosition).sqrMagnitude;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        // 방향 무작위 셔플
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
                if (dist > maxDist)
                {
                    maxDist = dist;
                    bestDir = dir;
                }
            }
        }

        if (bestDir != Vector2Int.zero) return bestDir;

        // 최선책이 없으면 동일 거리 유지 방향 탐색
        foreach (var dir in dirs)
        {
            Vector2Int next = GridPosition + dir;
            if (CanMoveTo(next))
            {
                float dist = (Player.GridPosition - next).sqrMagnitude;
                if (dist == maxDist) return dir;
            }
        }

        return Vector2Int.zero;
    }

    private bool CanMoveTo(Vector2Int pos)
    {
        if (!GridSystem.Instance.IsWalkable(pos)) return false;
        if (pos == Player.GridPosition) return false;
        if (TurnManager.Instance.GetEntityAt(pos) != null) return false;
        return true;
    }
}
