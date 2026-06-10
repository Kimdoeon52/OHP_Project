using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 박쥐 (EnemyBat) [Common] — Area 1 (하수구, 1~5층)
///
/// 핵심 메커니즘:
///   1. 한 턴에 최대 2칸 고속 이동 (moveSpeed 0.5 = 비용 0.5초)
///   2. 논리 좌표(GridPosition)를 먼저 즉시 갱신 후, 시각 이동은 SmoothMoveVisual 코루틴으로 보간
///   3. EnemyBase 템플릿 중 PerformMovement만 override
///
/// override 목록:
///   PerformMovement()          → 2칸 고속 이동 + 시각 보간 코루틴 시작
///   HasCustomMovementVisual()  → true 반환 (Walk 파라미터 수동 제어)
/// </summary>
public class EnemyBat : EnemyBase
{
    /// <summary> 이번 턴에 이동할 격자 좌표 경로 버퍼. 0번은 출발지. </summary>
    private List<Vector2Int> _movePath = new List<Vector2Int>();
    private Coroutine _moveCoroutine;

    // ExecutePendingAttack / CheckAttackTelegraph는 부모(EnemyBase) 기본 로직 그대로 사용.
    // (인접 1칸 근접 공격 예고)

    /// <summary>
    /// 플레이어가 visionRange 내에 있으면 최대 2칸의 논리 스텝을 밟고
    /// SmoothMoveVisual 코루틴으로 시각 이동 시작.
    /// </summary>
    protected override void PerformMovement()
    {
        _movePath.Clear();
        _movePath.Add(GridPosition); // 0번 인덱스 = 출발 위치

        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            // 실시간으로 최대 1칸씩 논리 이동 시도
            if (TryStepLogical())
            {
                _movePath.Add(GridPosition);
            }
        }

        // 진행 중 코루틴 취소 후 새 경로로 재시작
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
        }
        _moveCoroutine = StartCoroutine(SmoothMoveVisual(0.05f)); // 칸당 0.05초 간격으로 고속 이동
    }

    /// <summary>
    /// 애니메이션 없이 논리 좌표(GridPosition)만 즉시 갱신.
    /// 벽, 플레이어 점유, 타 엔티티 점유 검증 포함.
    /// </summary>
    /// <returns>논리 이동 성공 여부</returns>
    private bool TryStepLogical()
    {
        Vector2Int dir = GetNextStepDirection();
        if (dir == Vector2Int.zero) return false;

        Vector2Int next = GridPosition + dir;

        if (!GridSystem.Instance.IsWalkable(next)) return false;   // 벽
        if (next == Player.GridPosition) return false;             // 플레이어 위치
                                                                   // if (TurnManager.Instance.GetEntityAt(next) != null) return false; // 타 엔티티
        if (GridSystem.Instance.IsCellOccupied(next)) return false; // 타 엔티티
        if (GridSystem.Instance != null)
        {
            GridSystem.Instance.UnRegisterOccupancy(GridPosition);
            GridSystem.Instance.RegisterOccupancy(next, this);
        }
        // 논리 좌표만 즉시 반영 (시각은 SmoothMoveVisual에서 처리)
        GridPosition = next;
        TrapTerrainController.TryApplyTerrainAt(this, next);
        return true;
    }

    /// <summary>
    /// _movePath를 따라 월드 좌표를 delay(초) 간격으로 순차 스냅 이동.
    /// Walk 파라미터를 이 코루틴이 직접 켜고 끔.
    /// </summary>
    /// <param name="delay">칸 간 대기 시간 (초)</param>
    private IEnumerator SmoothMoveVisual(float delay)
    {
        if (_animator != null) _animator.SetBool("Walk", true);

        // 0번은 출발지이므로 1번부터 순서대로 이동
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
    /// 플레이어 방향으로 이동할 최선 방향 벡터 계산.
    /// X/Y 차이가 더 큰 축을 주축으로 먼저 시도, 막히면 부축으로 우회.
    /// </summary>
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

    /// <summary>
    /// 목표 격자가 이동 가능한지 검증.
    /// 벽, 플레이어, 타 엔티티 점유 시 false.
    /// </summary>
    private bool CanMoveTo(Vector2Int pos)
    {
        if (!GridSystem.Instance.IsWalkable(pos)) return false;
        if (pos == Player.GridPosition) return false;
        //      if (TurnManager.Instance.GetEntityAt(pos) != null) return false;
        if (GridSystem.Instance.IsCellOccupied(pos)) return false; // 타 엔티티
        return true;
    }

    /// <summary>
    /// SmoothMoveVisual이 Walk 파라미터를 직접 제어하므로 true 반환.
    /// 부모의 자동 Walk 리셋이 발동되지 않음.
    /// </summary>
    protected override bool HasCustomMovementVisual()
    {
        return true;
    }
}
