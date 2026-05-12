using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GridEnemyCrawler : EnemyBase
{
    private Vector2Int? _pendingAttackCell;
    private List<Vector2Int> _movePath = new List<Vector2Int>(); // 턴 내 이동 경로 기록용

    public override void OnTurnUpdate()
    {
        if (IsDead || Player == null) return;

        // 1. 이전 턴 공격 의도 발동
        ExecutePendingAttack();

        // 2. 고속 이동 (논리 처리)    
        _movePath.Clear();
        _movePath.Add(GridPosition); // 시작점 기록

        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            // 한 턴에 2번 이동 시도
            for (int i = 0; i < 2; i++)
            {
                if (TryStepLogical()) // 격자 좌표만 즉시 업데이트하는 전용 함수
                {
                    _movePath.Add(GridPosition); // 이동 성공 시 경로 기록
                }
            }
        }
        StopAllCoroutines();
        StartCoroutine(SmoothMoveVisual(0.5f));

        CheckAttackTelegraph();
    }
    private bool TryStepLogical()
    {
        Vector2Int dir = GetNextStepDirection();
        if (dir == Vector2Int.zero)
        {
            return false;
        }

        Vector2Int next = GridPosition + dir;

        if (GridSystem.Instance.IsWalkable(next))
        {
            return false;
        }
        if (next == Player.GridPosition)
        {
            return false;
        }
        if (TurnManager.Instance.GetEntityAt(next) != null)
        {
            return false;
        }

        GridPosition = next;
        return true;
    }
    private IEnumerator SmoothMoveVisual(float delay)
    {
        for (int i = 1; i < _movePath.Count; i++)
        {
            Vector3 targetWorldPos = GridSystem.Instance.GridToWorld(_movePath[i]);

            transform.position = targetWorldPos;
            UpdateVisibility(); // 시야 업데이트
            yield return new WaitForSeconds(delay);
        }
    }

    private void ExecutePendingAttack()
    {
        if (_pendingAttackCell.HasValue && Player.GridPosition == _pendingAttackCell.Value)
        {
            Player.TakeDamage(data.attackDamage);
            Debug.Log("공격받음 ");
        }
        _pendingAttackCell = null;
        ClearTelegraphs();
    }
    private void CheckAttackTelegraph()
    {
        if (!IsAdjacentToPlayer())
        {
            _pendingAttackCell = Player.GridPosition;
            ShowTelegraph(_pendingAttackCell.Value);
        }
    }

    private Vector2Int GetNextStepDirection()
    {
        var diff = Player.GridPosition - GridPosition;
        if (diff == Vector2Int.zero)
        {
            return Vector2Int.zero;
        }
        var dirX = diff.x != 0 ? new Vector2Int(System.Math.Sign(diff.x), 0) : Vector2Int.zero;
        var dirY = diff.y != 0 ? new Vector2Int(0, System.Math.Sign(diff.y)) : Vector2Int.zero;
        // 더 먼 축 우선순위 결정
        bool xPrimary = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);
        var primary = xPrimary ? dirX : dirY;
        var secondary = xPrimary ? dirY : dirX;
        // 가려는 칸이 비어있는지 확인 후 방향 반환
        if (primary != Vector2Int.zero && CanMoveTo(GridPosition + primary)) return primary;
        if (secondary != Vector2Int.zero && CanMoveTo(GridPosition + secondary)) return secondary;
        return Vector2Int.zero;
    }
    // 4. 이동 가능 여부 검증 (중복 코드 방지용)
    private bool CanMoveTo(Vector2Int pos)
    {
        if (!GridSystem.Instance.IsWalkable(pos)) return false; // ! 추가 확인
        if (pos == Player.GridPosition) return false;
        if (TurnManager.Instance.GetEntityAt(pos) != null) return false;
        return true;
    }

}
