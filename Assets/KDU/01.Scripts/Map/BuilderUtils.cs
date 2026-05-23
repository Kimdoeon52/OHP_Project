using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 빌더(LoopBuilder / FigureEightBuilder 등) 공통 헬퍼.
/// 방 사이즈 결정처럼 빌더 별 파라미터에 의존하는 로직은 제외 — 진짜 stateless 공용만 모음.
/// </summary>
public static class BuilderUtils
{
    /// <summary>방 경계 한 칸 바깥 셀 — target 방향과 가까운 면에서 무작위 위치.</summary>
    public static Vector2Int PickDoorTowards(RoomData room, Vector2 target, DungeonRandom rng)
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
    public static List<Vector2Int> LShapePath(Vector2Int from, Vector2Int to, DungeonRandom rng)
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

    /// <summary>두 방 사이를 L자 터널로 연결. 양쪽 doorCell 추가 + allRooms 에 Tunnel RoomData 추가.</summary>
    public static void ConnectWithTunnel(RoomData a, RoomData b, List<RoomData> allRooms, DungeonRandom rng)
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

    /// <summary>RectInt 안의 모든 셀.</summary>
    public static List<Vector2Int> CellsInRect(RectInt rect)
    {
        var list = new List<Vector2Int>(rect.width * rect.height);
        for (int x = rect.xMin; x < rect.xMax; x++)
            for (int y = rect.yMin; y < rect.yMax; y++)
                list.Add(new Vector2Int(x, y));
        return list;
    }

    /// <summary>
    /// 인접 방쌍이 겹치지 않을 만큼만 루프 반지름 확보. chord = 2R·sin(π/N) 식 역산.
    /// 방 크기 합 + interRoomGap 을 인접쌍 거리 하한으로 잡고 가장 큰 케이스로 R 결정.
    /// </summary>
    public static int ComputeLoopRadius(List<RoomData> loop, int interRoomGap, int minRadius)
    {
        int n = loop.Count;
        if (n < 2) return minRadius;

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
        return Mathf.Max(minRadius, Mathf.CeilToInt(r));
    }

    /// <summary>
    /// 루프 위에 방들을 배치 (center 중심 원).
    /// startAngleRad 로 index 0 의 시작 각도 지정 (figure-8 처럼 두 루프가 공유 방을 같은 좌표에 두고 싶을 때).
    /// </summary>
    public static void PositionInLoop(List<RoomData> loop, Vector2Int center, int radius, float startAngleRad = 0f)
    {
        int n = loop.Count;
        for (int i = 0; i < n; i++)
        {
            float angle = startAngleRad + (float)i / n * Mathf.PI * 2f;
            int cx = center.x + Mathf.RoundToInt(Mathf.Cos(angle) * radius);
            int cy = center.y + Mathf.RoundToInt(Mathf.Sin(angle) * radius);
            var b = loop[i].bounds;
            loop[i].bounds = new RectInt(cx - b.width / 2, cy - b.height / 2, b.width, b.height);
        }
    }

    /// <summary>셀 리스트를 감싸는 최소 RectInt.</summary>
    public static RectInt ComputeBounds(List<Vector2Int> cells)
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
