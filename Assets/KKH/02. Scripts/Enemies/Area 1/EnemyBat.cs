using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 박쥐 (EnemyBat) — 한 턴에 최대 2칸 고속 이동 및 시각 보간
public class EnemyBat : EnemyBase
{
    private readonly List<Vector2Int> _movePath = new List<Vector2Int>();
    private Coroutine _moveCoroutine;

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

        Vector2Int? targetPos = null;
        if (inVision)
            targetPos = Player.GridPosition;
        else if (_alarmTarget.HasValue)
            targetPos = _alarmTarget.Value;

        if (targetPos.HasValue)
        {
            for (int i = 0; i < 2; i++)
            {
                if (TryStepLogical(targetPos.Value))
                    _movePath.Add(GridPosition);
                else
                    break;
            }

            if (!inVision && _alarmTarget.HasValue && GridPosition == _alarmTarget.Value)
                _alarmTarget = null;
        }

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(SmoothMoveVisual(0.05f));
    }

    private bool TryStepLogical(Vector2Int targetPos)
    {
        Vector2Int dir = GetNextStepDirection(targetPos);
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

    private Vector2Int GetNextStepDirection(Vector2Int targetPos)
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

    protected override bool HasCustomMovementVisual()
    {
        return true;
    }
}
