using UnityEngine;

/// <summary>
/// 망령: 벽을 통과하여 플레이어를 추적함.
/// TryStep을 override하여 벽 통과 이동(IsWalkable 체크 제거).
/// 템플릿 메서드 기본 흐름 그대로 사용.
/// </summary>
public class GridEnemyWraith : EnemyBase
{
    // OnTurnUpdate는 EnemyBase 기본 템플릿 메서드 사용

    /// <summary>
    /// 벽 통과 이동: IsWalkable 체크 없이 이동 가능.
    /// </summary>
    protected override bool TryStep(Vector2Int dir)
    {
        var next = GridPosition + dir;
        if (next == Player.GridPosition) return false;
        if (TurnManager.Instance.GetEntityAt(next) != null) return false;

        SetGridPosition(next);
        UpdateVisibility();
        return true;
    }
}
