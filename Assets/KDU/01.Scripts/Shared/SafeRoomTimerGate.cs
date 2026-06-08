using System.Collections.Generic;
using UnityEngine;

// 세이프 룸 안에서 사용할 스크립트
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
        if (!_armed) 
            return;

        if (player == null || _safeCells == null)
            return;

        // SafeRoom 안에 있는 동안엔 시간 정지 유지
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
