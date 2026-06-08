using System.Collections.Generic;
using UnityEngine;

public static class SpawnCells
{
    private static readonly Vector2Int[] Neighbors =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    // 방 floor 셀 중 입구 인접, Hazard 를 제외한 배치 후보.
    public static List<Vector2Int> Candidates(RoomData room)
    {
        var result = new List<Vector2Int>();
        if (room == null || room.floorCells == null) 
            return result;

        var avoid = new HashSet<Vector2Int>();
        if (room.doorCells != null)
        {
            foreach (var d in room.doorCells)
            {
                avoid.Add(d);
                foreach (var n in Neighbors) avoid.Add(d + n);
            }
        }

        var hazard = MapGenerator.Instance != null ? MapGenerator.Instance.HazardCells : null;
        foreach (var c in room.floorCells)
        {
            if (avoid.Contains(c)) 
                continue;

            if (hazard != null && hazard.Contains(c))
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

    // 리스트에서 무작위 1개를 꺼냄
    public static Vector2Int PopRandom(List<Vector2Int> cells, DungeonRandom rng)
    {
        int idx = rng.Range(0, cells.Count);
        Vector2Int cell = cells[idx];
        cells.RemoveAt(idx);
        return cell;
    }
}
