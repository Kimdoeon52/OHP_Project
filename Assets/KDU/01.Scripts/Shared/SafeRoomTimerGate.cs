using System.Collections.Generic;
using UnityEngine;

// 세이프 룸 안에서 사용할 스크립트
public class SafeRoomTimerGate : MonoBehaviour
{
    [SerializeField] private GridPlayer player;

    private HashSet<Vector2Int> _safeCells;
    private bool _armed;

    // 적 감지 차단용 — 플레이어가 세이프룸 안에 있는지 (적 AI가 조회)
    public static bool PlayerInSafeRoom { get; private set; }

    public void Arm(RoomData safeRoom)
    {
        if (safeRoom == null || safeRoom.floorCells == null || safeRoom.floorCells.Count == 0)
        {
            _armed = false;
            PlayerInSafeRoom = false;
            TimeSystem.Instance?.StartTimer();
            return;
        }
        _safeCells = new HashSet<Vector2Int>(safeRoom.floorCells);
        PlayerInSafeRoom = true;
        TimeSystem.Instance?.StopTimer();
        _armed = true;
    }

    private void Update()
    {
        if (!_armed)
            return;

        if (player == null || _safeCells == null)
            return;

        var ts = TimeSystem.Instance;
        if (ts == null)
            return;

        // 안/밖 연속 판정 — 걸쳤다 다시 들어오면 재정지 (영구 해제 안 함)
        bool inside = _safeCells.Contains(player.GridPosition);
        PlayerInSafeRoom = inside;
        if (inside)
        {
            if (ts.IsRunning)
                ts.StopTimer();
        }
        else
        {
            if (!ts.IsRunning)
                ts.StartTimer();
        }
    }
}
