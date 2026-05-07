using System.Collections.Generic;

/// <summary>
/// RoomData 그래프 위에서 도는 BFS 유틸. 빌더 검증 / 디버그용.
/// </summary>
public static class MapGraph
{
    /// <summary>모든 방이 Safe Room 에서 도달 가능한가.</summary>
    public static bool IsConnected(List<RoomData> rooms)
    {
        if (rooms == null || rooms.Count == 0) return false;

        RoomData safe = null;
        foreach (var r in rooms)
        {
            if (r.type == RoomType.Safe) { safe = r; break; }
        }
        if (safe == null) return false;

        var distances = BFSDistances(safe);
        foreach (var r in rooms)
        {
            if (!distances.ContainsKey(r)) return false;
        }
        return true;
    }

    /// <summary>두 방 사이 경로 존재 여부.</summary>
    public static bool PathExists(RoomData from, RoomData to)
    {
        if (from == null || to == null) return false;
        return BFSDistances(from).ContainsKey(to);
    }

    /// <summary>start 로부터의 그래프 거리 (간선 = connections).</summary>
    public static Dictionary<RoomData, int> BFSDistances(RoomData start)
    {
        var distances = new Dictionary<RoomData, int>();
        if (start == null) return distances;

        var queue = new Queue<RoomData>();
        distances[start] = 0;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            int d = distances[cur];
            foreach (var next in cur.connections)
            {
                if (next == null || distances.ContainsKey(next)) continue;
                distances[next] = d + 1;
                queue.Enqueue(next);
            }
        }
        return distances;
    }
}
