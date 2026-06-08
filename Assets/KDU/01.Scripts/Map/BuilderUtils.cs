using System.Collections.Generic;
using UnityEngine;


// 빌더 공통 사용
public static class BuilderUtils
{
    // 방 경계 한 칸 바깥 셀 — target 방향과 가까운 면에서 무작위 위치
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

    public static List<Vector2Int> CellsInRect(RectInt rect)
    {
        var list = new List<Vector2Int>(rect.width * rect.height);
        for (int x = rect.xMin; x < rect.xMax; x++)
            for (int y = rect.yMin; y < rect.yMax; y++)
                list.Add(new Vector2Int(x, y));
        return list;
    }

    public static int ComputeLoopRadius(List<RoomData> loop, int interRoomGap, int minRadius)
    {
        int n = loop.Count;
        if (n < 2) 
            return minRadius;

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

    // 룰타일을 위해 벽을 띄움
    private const int BranchAttempts = 16;
    private const int SpecialGap = 2;
    private const int SecretGap = 4;
    private const int BranchClearance = 2;
    private static readonly Vector2Int[] Dirs4 = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

    public static void AttachBranchRooms(List<RoomData> rooms, List<RoomData> allRooms, DungeonRandom rng)
    {
        var failed = new List<RoomData>();
        foreach (var r in rooms)
        {
            if (r.type != RoomType.Safe) continue;
            if (!AttachBranchRoom(r, allRooms, rng)) failed.Add(r);
        }
        foreach (var r in rooms)
        {
            if (r.type != RoomType.Special && r.type != RoomType.Secret) continue;
            if (!AttachBranchRoom(r, allRooms, rng)) failed.Add(r);
        }
        foreach (var f in failed) { rooms.Remove(f); allRooms.Remove(f); }
    }

    public static bool AttachBranchRoom(RoomData branch, List<RoomData> allRooms, DungeonRandom rng)
    {
        var hosts = allRooms.FindAll(r => r.type == RoomType.Normal && r.floorCells != null && r.floorCells.Count > 0);
        if (hosts.Count == 0) 
            return false;
        var obstacles = allRooms.FindAll(r => r.floorCells != null && r.floorCells.Count > 0);

        bool outward = branch.type == RoomType.Secret;
        int gap = outward ? SecretGap : SpecialGap;
        Vector2 centroid = Centroid(obstacles);
        int bw = branch.bounds.width;
        int bh = branch.bounds.height;

        for (int attempt = 0; attempt < BranchAttempts; attempt++)
        {
            var host = rng.Pick(hosts);
            foreach (var dir in OrderDirs(host.bounds, centroid, outward, rng))
            {
                var rect = PlaceBeside(host.bounds, bw, bh, dir, gap);
                if (Overlaps(rect, obstacles, BranchClearance))
                    continue;

                branch.bounds = rect;
                branch.floorCells = CellsInRect(rect);
                ConnectStraightTunnel(host, branch, dir, allRooms);
                return true;
            }
        }
        return false;
    }

    public static void ConnectStraightTunnel(RoomData host, RoomData branch, Vector2Int dir, List<RoomData> allRooms)
    {
        var path = new List<Vector2Int>();
        Vector2Int doorHost, doorBranch;

        if (dir == Vector2Int.right || dir == Vector2Int.left)
        {
            int loY = Mathf.Max(host.bounds.yMin, branch.bounds.yMin);
            int hiY = Mathf.Min(host.bounds.yMax, branch.bounds.yMax); // exclusive
            int cy = Mathf.Clamp((loY + hiY - 1) / 2, loY, hiY - 1);
            int hostX = dir == Vector2Int.right ? host.bounds.xMax : host.bounds.xMin - 1;
            int branchX = dir == Vector2Int.right ? branch.bounds.xMin - 1 : branch.bounds.xMax;
            doorHost = new Vector2Int(hostX, cy);
            doorBranch = new Vector2Int(branchX, cy);
            int lo = Mathf.Min(hostX, branchX), hi = Mathf.Max(hostX, branchX);
            for (int x = lo; x <= hi; x++) path.Add(new Vector2Int(x, cy));
        }
        else // up / down
        {
            int loX = Mathf.Max(host.bounds.xMin, branch.bounds.xMin);
            int hiX = Mathf.Min(host.bounds.xMax, branch.bounds.xMax); // exclusive
            int cx = Mathf.Clamp((loX + hiX - 1) / 2, loX, hiX - 1);
            int hostY = dir == Vector2Int.up ? host.bounds.yMax : host.bounds.yMin - 1;
            int branchY = dir == Vector2Int.up ? branch.bounds.yMin - 1 : branch.bounds.yMax;
            doorHost = new Vector2Int(cx, hostY);
            doorBranch = new Vector2Int(cx, branchY);
            int lo = Mathf.Min(hostY, branchY), hi = Mathf.Max(hostY, branchY);
            for (int y = lo; y <= hi; y++) path.Add(new Vector2Int(cx, y));
        }

        var tunnel = new RoomData
        {
            type = RoomType.Tunnel,
            sizeCategory = SizeCategory.Normal,
            bounds = ComputeBounds(path),
            floorCells = path
        };
        tunnel.doorCells.Add(doorHost);
        tunnel.doorCells.Add(doorBranch);
        host.doorCells.Add(doorHost);
        branch.doorCells.Add(doorBranch);

        host.connections.Add(tunnel);
        branch.connections.Add(tunnel);
        tunnel.connections.Add(host);
        tunnel.connections.Add(branch);

        allRooms.Add(tunnel);

        branch.gatewayCells.Clear();
        if (branch.type == RoomType.Secret)
        {
            // 봉인 2칸을 호스트 벽 라인에서 한 칸 안쪽으로 밀어 넣음
            branch.gatewayCells.Add(doorHost + dir);
            branch.gatewayCells.Add(doorHost + 2 * dir);
        }
        else
        {
            // Special —> 방 입구 1칸
            branch.gatewayCells.Add(doorBranch);
        }
    }

    public static bool Overlaps(RectInt rect, List<RoomData> obstacles, int margin)
    {
        var expanded = new RectInt(
            rect.xMin - margin, rect.yMin - margin,
            rect.width + 2 * margin, rect.height + 2 * margin);
        foreach (var r in obstacles)
        {
            if (r.bounds.width <= 0 || r.bounds.height <= 0) continue;
            if (expanded.Overlaps(r.bounds)) return true;
        }
        return false;
    }

    private static RectInt PlaceBeside(RectInt host, int bw, int bh, Vector2Int dir, int gap)
    {
        int x, y;
        if (dir == Vector2Int.right) 
        { 
            x = host.xMax + gap; 
            y = host.yMin + (host.height - bh) / 2; 
        }
        else if (dir == Vector2Int.left) 
        { 
            x = host.xMin - gap - bw; 
            y = host.yMin + (host.height - bh) / 2; 
        }
        else if (dir == Vector2Int.up) 
        { 
            y = host.yMax + gap; 
            x = host.xMin + (host.width - bw) / 2; 
        }
        else 
        { 
            y = host.yMin - gap - bh; 
            x = host.xMin + (host.width - bw) / 2; 
        } 
        return new RectInt(x, y, bw, bh);
    }

    // 방향 우선순위
    private static List<Vector2Int> OrderDirs(RectInt host, Vector2 centroid, bool outward, DungeonRandom rng)
    {
        var list = new List<Vector2Int>(Dirs4);
        if (outward)
        {
            Vector2 away = host.center - centroid;
            list.Sort((a, b) => Vector2.Dot(away, b).CompareTo(Vector2.Dot(away, a)));
        }
        else
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        return list;
    }

    private static Vector2 Centroid(List<RoomData> rooms)
    {
        Vector2 sum = Vector2.zero;
        int n = 0;
        foreach (var r in rooms)
        {
            if (r.floorCells == null || r.floorCells.Count == 0) 
                continue;
            sum += r.bounds.center;
            n++;
        }
        return n > 0 ? sum / n : Vector2.zero;
    }

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
