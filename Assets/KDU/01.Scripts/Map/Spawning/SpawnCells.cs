using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방 안에서 스폰 후보 셀을 고르는 공용 헬퍼. 상자/열쇠/아이템 스포너 공용.
/// 입구(doorCells) 인접 셀과 Hazard 셀은 제외한다.
/// </summary>
public static class SpawnCells
{
    private static readonly Vector2Int[] Neighbors =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>방 floor 셀 중 입구 인접·Hazard 를 제외한 배치 후보.</summary>
    public static List<Vector2Int> Candidates(RoomData room)
    {
        var result = new List<Vector2Int>();
        if (room == null || room.floorCells == null) return result;

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
            if (avoid.Contains(c)) continue;
            if (hazard != null && hazard.Contains(c)) continue;
            result.Add(c);
        }
        return result;
    }

    /// <summary>지정한 방 타입들의 후보 셀을 모두 모은다.</summary>
    public static List<Vector2Int> CandidatesIn(List<RoomData> rooms, params RoomType[] types)
    {
        var result = new List<Vector2Int>();
        if (rooms == null) return result;
        foreach (var room in rooms)
        {
            if (System.Array.IndexOf(types, room.type) < 0) continue;
            result.AddRange(Candidates(room));
        }
        return result;
    }

    /// <summary>리스트에서 무작위 1개를 꺼낸다(중복 방지 — 꺼낸 셀 제거).</summary>
    public static Vector2Int PopRandom(List<Vector2Int> cells, DungeonRandom rng)
    {
        int idx = rng.Range(0, cells.Count);
        Vector2Int cell = cells[idx];
        cells.RemoveAt(idx);
        return cell;
    }
}
