using System.Collections.Generic;
using UnityEngine;

public static class SpawnCells
{
    // 이미 무언가(상자/아이템/골드)가 차지한 셀 — 스포너 간 중복 배치 방지
    private static readonly HashSet<Vector2Int> Occupied = new HashSet<Vector2Int>();

    // 층 생성 시작마다 호출 — 점유 초기화
    public static void ResetOccupied()
    {
        Occupied.Clear();
    }

    // 셀 점유 등록
    public static void MarkOccupied(Vector2Int cell)
    {
        Occupied.Add(cell);
    }

    public static bool IsOccupied(Vector2Int cell)
    {
        return Occupied.Contains(cell);
    }

    // 방 floor 셀 중 입구 인접, Hazard, 점유된 셀을 제외한 배치 후보.
    public static List<Vector2Int> Candidates(RoomData room)
    {
        var result = new List<Vector2Int>();
        if (room == null || room.floorCells == null) 
            return result;

        // 문(door) 주변 스폰 제외 범위: 맨해튼 거리 3칸 이내 모두 제외
        const int DoorAvoidRadius = 3;
        var avoid = new HashSet<Vector2Int>();
        if (room.doorCells != null)
        {
            foreach (var d in room.doorCells)
            {
                for (int dx = -DoorAvoidRadius; dx <= DoorAvoidRadius; dx++)
                {
                    int remaining = DoorAvoidRadius - Mathf.Abs(dx);
                    for (int dy = -remaining; dy <= remaining; dy++)
                    {
                        avoid.Add(d + new Vector2Int(dx, dy));
                    }
                }
            }
        }

        var hazard = MapGenerator.Instance != null ? MapGenerator.Instance.HazardCells : null;
        var grid = GridSystem.Instance;
        foreach (var c in room.floorCells)
        {
            if (avoid.Contains(c))
                continue;

            if (hazard != null && hazard.Contains(c))
                continue;

            // 실제 통행 가능한 셀만 (맵 밖/벽 위 배치 방지)
            if (grid != null && !grid.IsWalkable(c))
                continue;

            if (Occupied.Contains(c))
                continue;

            result.Add(c);
        }
        return result;
    }

    // 지정한 방 타입들의 후보 셀을 모두 모음
    public static List<Vector2Int> CandidatesIn(List<RoomData> rooms, params RoomType[] types)
    {
        var result = new List<Vector2Int>();
        if (rooms == null) 
            return result;

        foreach (var room in rooms)
        {
            if (System.Array.IndexOf(types, room.type) < 0)
                continue;

            result.AddRange(Candidates(room));
        }
        return result;
    }

    // 리스트에서 무작위 1개를 꺼내고 점유 등록
    public static Vector2Int PopRandom(List<Vector2Int> cells, DungeonRandom rng)
    {
        int idx = rng.Range(0, cells.Count);
        Vector2Int cell = cells[idx];
        cells.RemoveAt(idx);
        Occupied.Add(cell);
        return cell;
    }
}
