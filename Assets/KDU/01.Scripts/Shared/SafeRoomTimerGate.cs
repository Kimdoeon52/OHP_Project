using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Safe Room 안에 있는 동안 타이머 정지, 첫 이탈 시 타이머 시작 + 게이트 비활성.
/// FloorTransitionController 가 매 층 BuildFloor 마지막에 Arm(safeRoom) 으로 활성화.
///
/// GDD 11.1: Safe Room 은 한 번 나가면 재입장 불가. 본 구현은 *타이머* 한정으로 그 의미를 반영:
/// 첫 이탈로 타이머가 한 번 시작되면 같은 층에서 재진입해도 정지하지 않는다.
/// 재입장 자체 차단(문 잠금) 은 문 시스템과 함께 — BACKLOG.md "문 시스템" 참조.
/// </summary>
public class SafeRoomTimerGate : MonoBehaviour
{
    [SerializeField] private GridPlayer player;

    private HashSet<Vector2Int> _safeCells;
    private bool _armed;

    /// <summary>
    /// 새 층 진입 시 호출 — Safe Room 셀 정보 갱신 + 타이머 정지 + 게이트 활성화.
    /// safeRoom 이 null 이거나 셀이 비면 폴백: 게이트 비활성 + 타이머 즉시 시작.
    /// </summary>
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
