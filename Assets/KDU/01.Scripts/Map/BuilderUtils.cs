using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 빌더 공통 사용
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
    /// 인접 방쌍이 겹치지 않을 만큼만 루프 반지름 확보. chord = 2R·sin(π/N) 식 역산
    /// 방 크기 합 + interRoomGap 을 인접쌍 거리 하한으로 잡고 가장 큰 케이스로 R 결정
    /// </summary>
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

    /// <summary>
    /// 루프 위에 방들을 배치
    /// startAngleRad 로 index 0 의 시작 각도 지정
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

    // ───── 막다른 가지(Special/Secret) 배치 ─────
    // 메인 루프에 끼우지 않고 빈 공간에 떨어뜨려 호스트 방 1곳에 터널 1개로만 연결.
    // → "전체 흐름에서 벗어나 따로 빠진" 방. Special=가까이 짧게, Secret=바깥쪽 멀리.

    private const int BranchAttempts = 16;   // 호스트×방향 재시도 횟수
    private const int SpecialGap = 2;        // Special: 호스트와 가까이
    private const int SecretGap = 4;         // Secret: 호스트에서 더 떨어뜨림
    // 막다른 가지 주변 최소 벽 여유. 2 이상이어야 페인터 dissolve(1칸 끼인 벽 뚫기)가
    // 가지 방을 이웃 방/복도와 병합하지 못함 → 진짜 막다른 방 보장. (호스트와의 단일 터널만 연결)
    private const int BranchClearance = 2;
    private static readonly Vector2Int[] Dirs4 = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

    /// <summary>
    /// rooms 안의 Special/Secret 방을 각각 막다른 가지로 부착한다.
    /// 배치 실패한 방은 rooms/allRooms 에서 제거(맵에 떠 있지 않도록).
    /// 호출 시점: 메인 루프가 모두 배치·연결된 직후, BFS 거리 계산 전.
    /// </summary>
    public static void AttachBranchRooms(List<RoomData> rooms, List<RoomData> allRooms, DungeonRandom rng)
    {
        var failed = new List<RoomData>();
        // Safe 가 가장 중요한 가지(시작점) — 호스트 선택 실패를 피하려 가장 먼저 부착.
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

    /// <summary>
    /// 막다른 가지 방 하나를 빈 공간에 배치하고 호스트(Normal) 1곳에 터널 1개로 연결. 성공 시 true.
    /// branch.bounds 에는 AssignSize 가 정한 크기(원점 기준)가 들어 있다고 가정 — 위치만 새로 잡는다.
    /// </summary>
    public static bool AttachBranchRoom(RoomData branch, List<RoomData> allRooms, DungeonRandom rng)
    {
        // 호스트 후보·장애물 = 이미 배치된(floorCells 가 채워진) 방·터널만.
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
                // 방 면에 수직으로 직진하는 1칸 폭 복도 — 방과 닿는 지점이 정확히 1칸(doorB)이라
                // Secret 봉인/문 치환이 그 1칸만으로 완결됨. (L자 터널은 면을 따라 붙어 새는 문제)
                ConnectStraightTunnel(host, branch, dir, allRooms);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// host 와 branch 를 dir 축으로 직진하는 1칸 폭 복도로 연결한다.
    /// PlaceBeside 가 branch 를 host 면 중앙에 정렬해 두므로 수직 직선이 양쪽 면에 1칸씩만 닿는다.
    /// Secret 게이트는 호스트 끝에서 한 칸 안쪽 2칸을 봉인(아래 gatewayCells 기록 참고) — 혹 방지 + RuleTile 백킹 유지.
    /// </summary>
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

        // 관문 셀 기록.
        branch.gatewayCells.Clear();
        if (branch.type == RoomType.Secret)
        {
            // 봉인 2칸을 호스트 벽 라인에서 '한 칸 안쪽(터널 쪽)'으로 밀어 넣는다.
            //  - doorHost(=호스트 벽 라인 칸)는 floor 로 남김 → 벽 밖으로 혹이 튀어나오지 않음(대신 호스트 벽에 1칸 홈).
            //  - 봉인은 2칸 두께라 RuleTile 백킹이 유지돼 1칸 봉인 때처럼 벽이 얇게 깨지지 않음.
            //  - SecretGap=4 → 터널은 doorHost..doorBranch 4칸(offset 0~3). 봉인 offset 1·2, doorBranch(3)는 포켓으로 남겨 방이 막히지 않음.
            //  발견 시 이 2칸을 열어 문으로 치환.
            branch.gatewayCells.Add(doorHost + dir);
            branch.gatewayCells.Add(doorHost + 2 * dir);
        }
        else
        {
            // Special — 방 입구 1칸(잠긴 문 자리, 통행은 가능).
            branch.gatewayCells.Add(doorBranch);
        }
    }

    /// <summary>rect(여유 margin 포함)가 배치된 방/터널 중 하나라도 겹치면 true.</summary>
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

    /// <summary>host 의 dir 면 바깥에 gap 만큼 띄워 branch 를 수직축 중앙 정렬로 놓는 RectInt.</summary>
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
        } // down
        return new RectInt(x, y, bw, bh);
    }

    /// <summary>방향 우선순위. outward=true 면 맵 중심 반대쪽 우선, 아니면 무작위 셔플(결정론).</summary>
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
