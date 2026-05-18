using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 크롤러: 한 턴에 2칸 고속 이동.
/// 이동은 논리 좌표를 즉시 업데이트하고 시각 보간으로 표현.
/// 템플릿 메서드의 PerformMovement만 override.
/// </summary>
public class GridEnemyCrawler : EnemyBase
{
    private List<Vector2Int> _movePath = new List<Vector2Int>();

    // ExecutePendingAttack / CheckAttackTelegraph는 EnemyBase 기본 사용.
    // PerformMovement만 고속 이동으로 override.

    protected override void PerformMovement()
    {
        _movePath.Clear();
        _movePath.Add(GridPosition);

        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            // 한 턴에 2번 이동 시도
            for (int i = 0; i < 2; i++)
            {
                if (TryStepLogical())
                {
                    _movePath.Add(GridPosition);
                }
            }
        }
        StopAllCoroutines();
        StartCoroutine(SmoothMoveVisual(0.5f));
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

    private bool CanMoveTo(Vector2Int pos)
    {
        if (!GridSystem.Instance.IsWalkable(pos)) return false;
        if (pos == Player.GridPosition) return false;
        if (TurnManager.Instance.GetEntityAt(pos) != null) return false;
        return true;
    }
}
