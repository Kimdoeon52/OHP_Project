using System.Collections;
using System.Collections.Generic;
using PrototypeRT;
using UnityEngine;

// 칼 든 고블린 도적 (EnemyGoblinThief) — 2칸 고속 이동, 아이템 탈취 후 도주, 처치 시 드롭
public class EnemyGoblinThief : EnemyBase
{
    private bool _hasStolenItem = false;
    private ItemInstance _stolenItem = null;
    private readonly List<Vector2Int> _movePath = new List<Vector2Int>();
    private Coroutine _moveCoroutine;

    protected override IEnumerator ExecutePendingAttack(Vector2Int targetCell, float delaySeconds)
    {
        _isAttacking = true;
        ShowTelegraph(targetCell);

        yield return WaitGameSeconds(delaySeconds);

        if (!IsDead)
        {
            UpdateSpriteDirection(targetCell);
            if (_animator != null)
                _animator.SetTrigger("Attack");

            if (Player.GridPosition == targetCell)
            {
                int damage = RollAttackDamage();
                if (TryHitPlayer(damage) && !_hasStolenItem)
                    TryStealItem();
            }
        }

        ClearTelegraphs();
        _attackActionPoints = 0f;
        _isAttacking = false;
        _pendingAttackCell = null;
    }

    private void TryStealItem()
    {
        var invController = FindFirstObjectByType<InventoryController>();
        if (invController == null || invController.Items.Count == 0)
            return;

        int index = GetRng().Range(0, invController.Items.Count);
        var targetItem = invController.Items[index];

        if (targetItem != null && targetItem.ItemInstance != null)
        {
            _stolenItem = targetItem.ItemInstance;

            bool removed = invController.TryRemoveItem(targetItem);
            if (removed)
            {
                Destroy(targetItem.gameObject);
                _hasStolenItem = true;
            }
        }
    }

    protected override void Die()
    {
        if (_hasStolenItem && _stolenItem != null)
        {
            var groundItemController = FindFirstObjectByType<GroundItemController>();
            if (groundItemController != null)
                groundItemController.DropItem(_stolenItem, 1, GridPosition);
            else
                Debug.LogError($"[GoblinThief] {name} 처치 후 GroundItemController가 없어 {_stolenItem.data.Name} 영구 분실!");
        }

        base.Die();
    }

    protected override void PerformMovement()
    {
        if (IsDead)
            return;

        _movePath.Clear();
        _movePath.Add(GridPosition);

        bool inVision = data != null && SquaredDistanceToPlayer() <= data.visionRange * data.visionRange;
        if (inVision)
        {
            isDetected = true;
            _alarmTarget = null;
        }
        else if (GameManager.Instance != null && GameManager.Instance.isAlerted && !SafeRoomTimerGate.PlayerInSafeRoom)
        {
            if (_alarmTarget == null)
                _alarmTarget = Player.GridPosition;

            isDetected = true;
        }

        bool shouldMove = false;
        Vector2Int chaseTarget = Player.GridPosition;

        if (_hasStolenItem)
        {
            if (!SafeRoomTimerGate.PlayerInSafeRoom)
                shouldMove = true;
        }
        else if (inVision)
        {
            shouldMove = true;
            chaseTarget = Player.GridPosition;
        }
        else if (_alarmTarget.HasValue)
        {
            shouldMove = true;
            chaseTarget = _alarmTarget.Value;
        }

        if (shouldMove)
        {
            for (int i = 0; i < 2; i++)
            {
                if (TryStepLogical(chaseTarget))
                    _movePath.Add(GridPosition);
                else
                    break;
            }

            if (!_hasStolenItem && !inVision && _alarmTarget.HasValue && GridPosition == _alarmTarget.Value)
                _alarmTarget = null;
        }

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(SmoothMoveVisual(0.08f));
    }

    private bool TryStepLogical(Vector2Int chaseTarget)
    {
        Vector2Int dir = GetNextStepDirection(chaseTarget);
        if (dir == Vector2Int.zero)
            return false;

        Vector2Int next = GridPosition + dir;

        if (IsSafeRoomCell(next))
            return false;

        if (!GridSystem.Instance.IsWalkable(next))
            return false;

        if (next == Player.GridPosition)
            return false;

        if (GridSystem.Instance.IsCellOccupied(next))
            return false;

        GridSystem.Instance.UnRegisterOccupancy(GridPosition);
        GridSystem.Instance.RegisterOccupancy(next, this);
        GridPosition = next;
        TrapTerrainController.TryApplyTerrainAt(this, next);
        return true;
    }

    private IEnumerator SmoothMoveVisual(float delay)
    {
        if (_animator != null)
            _animator.SetBool("Walk", true);

        for (int i = 1; i < _movePath.Count; i++)
        {
            Vector3 targetWorldPos = GridSystem.Instance.GridToWorld(_movePath[i]);
            transform.position = targetWorldPos;
            UpdateVisibility();
            UpdateSpriteDirection(_movePath[i]);
            yield return WaitGameSeconds(delay);
        }

        if (_animator != null)
            _animator.SetBool("Walk", false);
    }

    private Vector2Int GetNextStepDirection(Vector2Int chaseTarget)
    {
        if (_hasStolenItem)
            return GetEscapeStepDirection();

        return GetChaseStepDirection(chaseTarget);
    }

    private Vector2Int GetChaseStepDirection(Vector2Int targetPos)
    {
        var diff = targetPos - GridPosition;
        if (diff == Vector2Int.zero)
            return Vector2Int.zero;

        var dirX = diff.x != 0 ? new Vector2Int(System.Math.Sign(diff.x), 0) : Vector2Int.zero;
        var dirY = diff.y != 0 ? new Vector2Int(0, System.Math.Sign(diff.y)) : Vector2Int.zero;

        bool xPrimary = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);
        var primary = xPrimary ? dirX : dirY;
        var secondary = xPrimary ? dirY : dirX;

        if (primary != Vector2Int.zero && CanMoveTo(GridPosition + primary))
            return primary;

        if (secondary != Vector2Int.zero && CanMoveTo(GridPosition + secondary))
            return secondary;

        return Vector2Int.zero;
    }

    private Vector2Int GetEscapeStepDirection()
    {
        Vector2Int bestDir = Vector2Int.zero;
        float maxDist = (Player.GridPosition - GridPosition).sqrMagnitude;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        DungeonRandom rng = GetRng();

        for (int i = 0; i < dirs.Length; i++)
        {
            int randIdx = rng.Range(i, dirs.Length);
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

        if (bestDir != Vector2Int.zero)
            return bestDir;

        foreach (var dir in dirs)
        {
            Vector2Int next = GridPosition + dir;
            if (CanMoveTo(next))
            {
                float dist = (Player.GridPosition - next).sqrMagnitude;
                if (Mathf.Approximately(dist, maxDist))
                    return dir;
            }
        }

        return Vector2Int.zero;
    }

    private bool CanMoveTo(Vector2Int pos)
    {
        if (IsSafeRoomCell(pos))
            return false;

        if (!GridSystem.Instance.IsWalkable(pos))
            return false;

        if (pos == Player.GridPosition)
            return false;

        if (GridSystem.Instance.IsCellOccupied(pos))
            return false;

        return true;
    }

    protected override bool HasCustomMovementVisual() => true;
}
