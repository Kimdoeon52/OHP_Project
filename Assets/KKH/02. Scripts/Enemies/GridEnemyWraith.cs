using UnityEngine;

/// <summary>
/// 망령: 벽을 통과하여 플레이어를 추적함
/// 턴 흐름
/// 1. 벽 통과하여 플레이어에게 접근
/// 2. 접근 조건은 플레이어가 시야 내에 있을 때
/// 3. 사거리 1이면 공격
/// 
/// 벨런스 조절을 위하여 시야 밖에서도 플레이어의 위치를 감지하는 
/// 대신 공격력 및 체력이 Tracker보다 더 낮게 설정되어야함
/// </summary>
public class GridEnemyWraith : EnemyBase
{
    private Vector2Int? _pendingAttackCell;

    public override void OnTurnUpdate()
    {
        if (IsDead || Player == null) return;

        // 1. 이전 턴 공격 의도 발동
        if (_pendingAttackCell.HasValue && Player.GridPosition == _pendingAttackCell.Value)
        {
            Player.TakeDamage(data.attackDamage);
        }
        _pendingAttackCell = null;
        ClearTelegraphs();

        // 2. 시야 안이면 플레이어 방향으로 1칸 이동
        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            TryStepTowardPlayer();
        }

        // 3. 인접 시 다음 턴 공격 의도 표시
        if (IsAdjacentToPlayer())
        {
            _pendingAttackCell = Player.GridPosition;
            ShowTelegraph(_pendingAttackCell.Value);
        }
    }
    private bool TryPhaseStep(Vector2Int dir)
    {
        var next = GridPosition + dir;

        if (next == Player.GridPosition) return false;  // 플레이어 칸으로는 이동 안 함 (공격은 텔레그래프로)
        if (TurnManager.Instance.GetEntityAt(next) != null) return false;  // 다른 엔티티 점유

        SetGridPosition(next);
        UpdateVisibility();
        return true;
    }
    private void TryStepTowardPlayer()
    {
        var diff = Player.GridPosition - GridPosition; // 방향벡터

        var dirX = diff.x != 0 ? new Vector2Int(System.Math.Sign(diff.x), 0) : Vector2Int.zero; // x축 방향 벡터
        var dirY = diff.y != 0 ? new Vector2Int(0, System.Math.Sign(diff.y)) : Vector2Int.zero; // y축 방향 벡터

        // 더 먼 축 우선, 차단 시 다른 축 시도
        bool xPrimary = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y); // x축이 더 멀면 x축 우선
        var primary = xPrimary ? dirX : dirY; // 우선축 방향 벡터
        var secondary = xPrimary ? dirY : dirX; // 차선축 방향 벡터

        if (primary != Vector2Int.zero && TryPhaseStep(primary)) return; // 우선축으로 이동 시도
        if (secondary != Vector2Int.zero && TryPhaseStep(secondary)) return; // 차선축으로 이동 시도
    }


}


