using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메인 방을 원형 루프로 배치하고 인접쌍을 L-자 터널로 연결한다.
/// Safe 와 Exit 는 루프의 반대편에 자동 배치 (Exit index ≈ N/2).
/// SPD LoopBuilder 디자인 참고, 코드는 자체 작성.
/// </summary>
[CreateAssetMenu(fileName = "LoopBuilder", menuName = "60s Dungeon/Builders/Loop Builder")]
public class LoopBuilder : ScriptableObject, ILayoutBuilder
{
    [Header("루프 배치")]
    [SerializeField] private int minLoopRadius = 8;     // 방이 작아도 이 이하로는 줄이지 않음
    [SerializeField] private int interRoomGap = 3;      // 인접 방 사이 최소 여유 (벽 + 터널 폭 흡수)

    [Header("방 크기 범위 (셀 단위, min/max 모두 포함)")]
    [SerializeField] private Vector2Int normalSize = new Vector2Int(5, 7);
    [SerializeField] private Vector2Int largeSize = new Vector2Int(8, 11);
    [SerializeField] private Vector2Int giantSize = new Vector2Int(12, 15);

    public List<RoomData> Build(List<RoomData> rooms, DungeonRandom rng)
    {
        if (rooms == null || rooms.Count < 2)
        {
            Debug.LogError("LoopBuilder.Build: 방이 너무 적음 (최소 2개 필요)");
            return rooms;
        }

        foreach (var r in rooms) AssignSize(r, rng);

        var loopOrder = BuildLoopOrder(rooms);
        if (loopOrder == null) return rooms;

        PositionInLoop(loopOrder);

        // 방 자체의 floorCells (인테리어) 채우기
        foreach (var r in rooms) r.floorCells = CellsInRect(r.bounds);

        // 인접쌍 cycle 연결 — 각 연결마다 Tunnel 한 개 생성
        var allRooms = new List<RoomData>(rooms);
        for (int i = 0; i < loopOrder.Count; i++)
        {
            var a = loopOrder[i];
            var b = loopOrder[(i + 1) % loopOrder.Count];
            ConnectWithTunnel(a, b, allRooms, rng);
        }

        // BFS 로 Safe Room 으로부터의 그래프 거리 계산
        var safe = rooms.Find(r => r.type == RoomType.Safe);
        var distances = MapGraph.BFSDistances(safe);
        foreach (var r in allRooms)
            r.distanceFromSafe = distances.TryGetValue(r, out var d) ? d : -1;

        return allRooms;
    }

    private void AssignSize(RoomData room, DungeonRandom rng)
    {
        Vector2Int range;
        switch (room.sizeCategory)
        {
            case SizeCategory.Large: range = largeSize; break;
            case SizeCategory.Giant: range = giantSize; break;
            default: range = normalSize; break;
        }
        int w = rng.Range(range.x, range.y + 1);
        int h = rng.Range(range.x, range.y + 1);
        room.bounds = new RectInt(0, 0, w, h);
    }

    /// <summary>Safe → 절반 Normal → Exit → 나머지 Normal 순서로 cycle 구성.</summary>
    private List<RoomData> BuildLoopOrder(List<RoomData> rooms)
    {
        var safe = rooms.Find(r => r.type == RoomType.Safe);
        var exit = rooms.Find(r => r.type == RoomType.Exit);
        var normals = rooms.FindAll(r => r.type == RoomType.Normal);

        if (safe == null || exit == null)
        {
            Debug.LogError("LoopBuilder: Safe / Exit Room 누락");
            return null;
        }

        int half = normals.Count / 2;
        var order = new List<RoomData>(rooms.Count);
        order.Add(safe);
        for (int i = 0; i < half; i++) order.Add(normals[i]);
        order.Add(exit);
        for (int i = half; i < normals.Count; i++) order.Add(normals[i]);
        return order;
    }

    private void PositionInLoop(List<RoomData> loop)
    {
        int n = loop.Count;
        int radius = ComputeLoopRadius(loop);
        for (int i = 0; i < n; i++)
        {
            float angle = (float)i / n * Mathf.PI * 2f;
            int cx = Mathf.RoundToInt(Mathf.Cos(angle) * radius);
            int cy = Mathf.RoundToInt(Mathf.Sin(angle) * radius);
            var b = loop[i].bounds;
            loop[i].bounds = new RectInt(cx - b.width / 2, cy - b.height / 2, b.width, b.height);
        }
    }

    /// <summary>
    /// 인접 방쌍이 겹치지 않을 만큼만 반지름 확보. chord = 2R·sin(π/N) 식 역산.
    /// 방 크기 합 + interRoomGap 을 인접쌍 거리(chord) 의 하한으로 잡고, 가장 큰 케이스로 R 결정.
    /// </summary>
    private int ComputeLoopRadius(List<RoomData> loop)
    {
        int n = loop.Count;
        if (n < 2) return minLoopRadius;

        float maxChordNeeded = 0f;
        for (int i = 0; i < n; i++)
        {
            var a = loop[i];
            var b = loop[(i + 1) % n];
            float aHalf = Mathf.Max(a.bounds.width, a.bounds.height) * 0.5f;
            float bHalf = Mathf.Max(b.bounds.width, b.bounds.height) * 0.5f;
            float need = aHalf + bHalf + interRoomGap;
            if (need > maxChordNeeded) maxChordNeeded = need;
        }

        float r = maxChordNeeded / (2f * Mathf.Sin(Mathf.PI / n));
        return Mathf.Max(minLoopRadius, Mathf.CeilToInt(r));
    }

    private void ConnectWithTunnel(RoomData a, RoomData b, List<RoomData> allRooms, DungeonRandom rng)
    {
        var doorA = PickDoorTowards(a, b.bounds.center, rng);
        var doorB = PickDoorTowards(b, a.bounds.center, rng);
        var path = LShapePath(doorA, doorB, rng);

        var tunnel = new RoomData
        {
            type = RoomType.Tunnel,
            sizeCategory = SizeCategory.Normal,
            bounds = ComputeBounds(path),
            floorCells = path
        };
        tunnel.doorCells.Add(doorA);
        tunnel.doorCells.Add(doorB);
        a.doorCells.Add(doorA);
        b.doorCells.Add(doorB);

        a.connections.Add(tunnel);
        b.connections.Add(tunnel);
        tunnel.connections.Add(a);
        tunnel.connections.Add(b);

        allRooms.Add(tunnel);
    }

    /// <summary>방 경계 한 칸 바깥 셀 — target 방향과 가까운 면에서 무작위 위치.</summary>
    private Vector2Int PickDoorTowards(RoomData room, Vector2 target, DungeonRandom rng)
    {
        var center = room.bounds.center;
        float dx = target.x - center.x;
        float dy = target.y - center.y;

        int x, y;
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            x = (dx >= 0f) ? room.bounds.xMax : room.bounds.xMin - 1;
            y = rng.Range(room.bounds.yMin, room.bounds.yMax);
        }
        else
        {
            x = rng.Range(room.bounds.xMin, room.bounds.xMax);
            y = (dy >= 0f) ? room.bounds.yMax : room.bounds.yMin - 1;
        }
        return new Vector2Int(x, y);
    }

    /// <summary>Manhattan L-자 경로. 수평-수직 또는 수직-수평 중 무작위.</summary>
    private List<Vector2Int> LShapePath(Vector2Int from, Vector2Int to, DungeonRandom rng)
    {
        var path = new List<Vector2Int>();
        int x = from.x, y = from.y;
        bool horizontalFirst = rng.Chance(0.5f);

        if (horizontalFirst)
        {
            while (x != to.x) { path.Add(new Vector2Int(x, y)); x += (to.x > x) ? 1 : -1; }
            while (y != to.y) { path.Add(new Vector2Int(x, y)); y += (to.y > y) ? 1 : -1; }
        }
        else
        {
            while (y != to.y) { path.Add(new Vector2Int(x, y)); y += (to.y > y) ? 1 : -1; }
            while (x != to.x) { path.Add(new Vector2Int(x, y)); x += (to.x > x) ? 1 : -1; }
        }
        path.Add(new Vector2Int(x, y));
        return path;
    }

    private static List<Vector2Int> CellsInRect(RectInt rect)
    {
        var list = new List<Vector2Int>(rect.width * rect.height);
        for (int x = rect.xMin; x < rect.xMax; x++)
            for (int y = rect.yMin; y < rect.yMax; y++)
                list.Add(new Vector2Int(x, y));
        return list;
    }

    private static RectInt ComputeBounds(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0) return new RectInt(0, 0, 0, 0);
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var c in cells)
        {
            if (c.x < minX) minX = c.x;
            if (c.y < minY) minY = c.y;
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }
        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
}
