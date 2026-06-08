using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Safe Room 안에 있는 동안 타이머 정지, 첫 이탈 시 타이머 시작 + 게이트 비활성
/// </summary>
public class SafeRoomTimerGate : MonoBehaviour
{
    [SerializeField] private GridPlayer player;

    private HashSet<Vector2Int> _safeCells;
    private bool _armed;

    public void Arm(RoomData safeRoom)
    {
        if (safeRoom == null || safeRoom.floorCells == null || safeRoom.floorCells.Count == 0)
        {
            _armed = false;
            TimeSystem.Instance?.StartTimer();
            return;
        }
        _safeCells = new HashSet<Vector2Int>(safeRoom.floorCells);
        TimeSystem.Instance?.StopTimer();
        _armed = true;
    }

    private void Update()
    {
        if (!_armed) return;
        if (player == null || _safeCells == null) return;

        // SafeRoom 안에 있는 동안엔 계속 정지 유지 — GridPlayer.Start 등 다른 곳에서
        // 타이머를 켜도 순서에 상관없이 다시 멈춘다. (첫 이탈 전까지 무조건 0초 흐름)
        if (_safeCells.Contains(player.GridPosition))
        {
            if (TimeSystem.Instance != null && TimeSystem.Instance.IsRunning)
                TimeSystem.Instance.StopTimer();
            return;
        }

        _armed = false;
        TimeSystem.Instance?.StartTimer();
    }
}
