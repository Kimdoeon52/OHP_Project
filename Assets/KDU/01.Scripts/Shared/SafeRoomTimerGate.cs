using System.Collections.Generic;
using UnityEngine;

// 세이프 룸 안에서 사용할 스크립트
public class SafeRoomTimerGate : MonoBehaviour
{
    [SerializeField] private GridPlayer player;

    private HashSet<Vector2Int> _safeCells;
    private HashSet<Vector2Int> _thresholdCells;
    private bool _armed;
    private bool _hasStarted;

    // 적 감지 차단용 — 플레이어가 세이프룸 안에 있는지 (적 AI가 조회)
    public static bool PlayerInSafeRoom { get; private set; }

    // 게이트 비활성화 — 보스층 등 게이트가 타이머를 건드리면 안 되는 층에서 호출
    public void Disarm()
    {
        _armed = false;
        PlayerInSafeRoom = false;
    }

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
        _thresholdCells = CollectSafeExitThresholdCells();
        _hasStarted = false;
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

        if (player.IsDead)
            return;

        var ts = TimeSystem.Instance;
        if (ts == null)
            return;

        // 안/밖 연속 판정 — 걸쳤다 다시 들어오면 재정지 (영구 해제 안 함)
        if (!_hasStarted && (_thresholdCells == null || _thresholdCells.Count == 0))
            _thresholdCells = CollectSafeExitThresholdCells();

        // 철문 위 칸은 아직 출발 확정이 아니므로 세이프룸과 같은 준비 구역으로 취급합니다.
        bool inSafeArea = _safeCells.Contains(player.GridPosition);
        bool onThreshold = _thresholdCells != null && _thresholdCells.Contains(player.GridPosition);
        bool waitingToStart = !_hasStarted && (inSafeArea || onThreshold);

        PlayerInSafeRoom = waitingToStart;
        if (waitingToStart)
        {
            if (ts.IsRunning)
                ts.StopTimer();
        }
        else
        {
            _hasStarted = true;
            if (!ts.IsRunning)
                ts.StartTimer();
        }
    }

    private static HashSet<Vector2Int> CollectSafeExitThresholdCells()
    {
        HashSet<Vector2Int> thresholdCells = new();
        GridDoor[] doors = FindObjectsByType<GridDoor>(FindObjectsSortMode.None);
        foreach (GridDoor door in doors)
        {
            if (door != null && door.Kind == GridDoor.DoorKind.SafeExit)
                thresholdCells.Add(door.GridPosition);
        }

        return thresholdCells;
    }
}
