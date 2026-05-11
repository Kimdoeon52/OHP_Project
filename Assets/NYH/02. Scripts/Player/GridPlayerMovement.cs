using UnityEngine;

/// <summary>
/// 4방향 격자 이동 + facing 추적. 차단 시 facing만 갱신, 시간 소모 없음(자유 회전).
/// </summary>
public class GridPlayerMovement : MonoBehaviour
{
    public Vector2Int Facing { get; private set; } = Vector2Int.right;

    private GridPlayer _player;

    public void Init(GridPlayer player)
    {
        _player = player;
    }

    /// <summary>
    /// 외부에서 facing만 갱신 (근접 공격 등 이동 없이 방향 전환).
    /// </summary>
    public void SetFacing(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return;
        Facing = dir;
    }

    public void TryMove(Vector2Int direction)
    {
        // 회전은 항상 갱신 (차단되어도 방향은 바뀜 — 던지기 방향 잡기 용도)
        Facing = direction;

        var target = _player.GridPosition + direction;

        if (!GridSystem.Instance.IsWalkable(target))
        {
            // 벽에 막혔지만 회전만 한 셈 — 시간 소모 없음
            return;
        }

        // 이동 후 단검 회수 시도 (이동 비용 + 회수 비용 합산)
        float totalCost = _player.Config.moveTimeCost;
        StatusController statusController = _player.GetComponent<StatusController>();
        if (statusController != null)
            totalCost = statusController.ModifyMoveTimeCost(totalCost);

        if (_player.Combat.TryRetrieveAt(target))
        {
            totalCost += _player.Config.retrieveTimeCost;
        }

        _player.SetGridPosition(target);

        // 이동한 칸에 덫이 있으면 플레이어 턴 종료 전에 먼저 발동합니다.
        // 텔레포트 덫처럼 플레이어 위치를 다시 바꾸는 덫이 있으므로,
        // 시야 갱신은 덫 발동 이후의 최종 위치를 기준으로 합니다.
        TrapSystem.Instance?.TryTriggerAt(target, _player);
        VisionSystem.Instance.UpdateVision(_player.GridPosition);

        TurnManager.Instance.OnPlayerActionCompleted(totalCost);
    }
}
