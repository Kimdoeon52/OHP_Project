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
        if (_safeCells.Contains(player.GridPosition)) return;

        _armed = false;
        TimeSystem.Instance?.StartTimer();
    }
}
