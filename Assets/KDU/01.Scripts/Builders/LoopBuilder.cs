using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메인 방을 원형 루프로 배치하고 인접쌍을 L-자 터널로 연결한다.
/// Safe 와 Exit 는 루프의 반대편에 자동 배치
/// SPD LoopBuilder 디자인 참고, 코드는 자체 작성.
/// </summary>
[CreateAssetMenu(fileName = "LoopBuilder", menuName = "60s Dungeon/Builders/Loop Builder")]
public class LoopBuilder : ScriptableObject, ILayoutBuilder
{
    [Header("루프 배치")]
    [SerializeField] private int minLoopRadius = 8;     // 방이 작아도 이 이하로는 줄이지 않음
    [SerializeField] private int interRoomGap = 3;      // 인접 방 사이 최소 여유

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
        if (loopOrder == null) 
            return rooms;

        int radius = BuilderUtils.ComputeLoopRadius(loopOrder, interRoomGap, minLoopRadius);
        BuilderUtils.PositionInLoop(loopOrder, Vector2Int.zero, radius);

        // 메인 루프 방의 floorCells 채우기 (Safe/Special/Secret 막다른 가지는 아래에서 따로 배치)
        foreach (var r in rooms)
        {
            if (r.type == RoomType.Safe || r.type == RoomType.Special || r.type == RoomType.Secret) continue;
            r.floorCells = BuilderUtils.CellsInRect(r.bounds);
        }

        // 인접쌍 cycle 연결 — 각 연결마다 Tunnel 한 개 생성
        var allRooms = new List<RoomData>(rooms);
        for (int i = 0; i < loopOrder.Count; i++)
        {
            var a = loopOrder[i];
            var b = loopOrder[(i + 1) % loopOrder.Count];
            BuilderUtils.ConnectWithTunnel(a, b, allRooms, rng);
        }

        // Special/Secret 을 막다른 가지로 빈 공간에 부착
        BuilderUtils.AttachBranchRooms(rooms, allRooms, rng);

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
            case SizeCategory.Large: 
                range = largeSize; 
                break;
            case SizeCategory.Giant: 
                range = giantSize; 
                break;
            default: 
                range = normalSize; 
                break;
        }
        int w = rng.Range(range.x, range.y + 1);
        int h = rng.Range(range.x, range.y + 1);
        room.bounds = new RectInt(0, 0, w, h);
    }

    private List<RoomData> BuildLoopOrder(List<RoomData> rooms)
    {
        var safe = rooms.Find(r => r.type == RoomType.Safe);
        var exit = rooms.Find(r => r.type == RoomType.Exit);
        var boss = rooms.Find(r => r.type == RoomType.Boss);
        var normals = rooms.FindAll(r => r.type == RoomType.Normal);

        if (safe == null || exit == null)
        {
            Debug.LogError("LoopBuilder: Safe / Exit Room 누락");
            return null;
        }

        int half = normals.Count / 2;
        var order = new List<RoomData>(rooms.Count);
        // Safe 는 메인 루프에 포함하지 않음 — Special 처럼 막다른 가지로 부착(GDD 11.1, 시작점 격리).
        for (int i = 0; i < half; i++) order.Add(normals[i]);
        if (boss != null) order.Add(boss);
        order.Add(exit);
        for (int i = half; i < normals.Count; i++) order.Add(normals[i]);
        return order;
    }

}
