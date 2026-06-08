using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FigureEightBuilder", menuName = "60s Dungeon/Builders/Figure Eight Builder")]
public class FigureEightBuilder : ScriptableObject, ILayoutBuilder
{
    [Header("루프 배치")]
    [SerializeField] private int minLoopRadius = 8;
    [SerializeField] private int interRoomGap = 3;
    [SerializeField] private int loopSeparationGap = 0;

    [Header("방 크기 범위")]
    [SerializeField] private Vector2Int normalSize = new Vector2Int(5, 7);
    [SerializeField] private Vector2Int largeSize = new Vector2Int(8, 11);
    [SerializeField] private Vector2Int giantSize = new Vector2Int(12, 15);
    [Tooltip("공유 방의 크기 카테고리")]
    [SerializeField] private SizeCategory sharedRoomSize = SizeCategory.Large;

    public List<RoomData> Build(List<RoomData> rooms, DungeonRandom rng)
    {
        if (rooms == null || rooms.Count < 4)
        {
            Debug.LogError("FigureEightBuilder.Build: 방이 너무 적음");
            return rooms;
        }

        var safe = rooms.Find(r => r.type == RoomType.Safe);
        var exit = rooms.Find(r => r.type == RoomType.Exit);
        var boss = rooms.Find(r => r.type == RoomType.Boss);
        var normals = rooms.FindAll(r => r.type == RoomType.Normal);

        if (safe == null || exit == null)
        {
            Debug.LogError("FigureEightBuilder: Safe / Exit 누락");
            return null;
        }
        if (normals.Count < 1)
        {
            Debug.LogError("FigureEightBuilder: 공유 노드용 Normal 방 1개 이상 필요");
            return null;
        }

        var shared = normals[normals.Count - 1];
        normals.RemoveAt(normals.Count - 1);
        shared.sizeCategory = sharedRoomSize;

        // 방 사이즈 결정
        foreach (var r in rooms) 
            AssignSize(r, rng);
        int half = normals.Count / 2;
        var loopA = new List<RoomData> { shared };
        for (int i = 0; i < half; i++)
            loopA.Add(normals[i]);

        var loopB = new List<RoomData> { shared, exit };
        if (boss != null)
            loopB.Add(boss);
        for (int i = half; i < normals.Count; i++)
            loopB.Add(normals[i]);

        int radiusA = BuilderUtils.ComputeLoopRadius(loopA, interRoomGap, minLoopRadius);
        int radiusB = BuilderUtils.ComputeLoopRadius(loopB, interRoomGap, minLoopRadius);

        int gapHalf = Mathf.Max(0, loopSeparationGap) / 2;
        var centerA = new Vector2Int(-(radiusA + gapHalf), 0);
        var centerB = new Vector2Int(+(radiusB + gapHalf), 0);
        BuilderUtils.PositionInLoop(loopA, centerA, radiusA, startAngleRad: 0f);
        BuilderUtils.PositionInLoop(loopB, centerB, radiusB, startAngleRad: Mathf.PI);

        foreach (var r in rooms)
        {
            if (r.type == RoomType.Safe || r.type == RoomType.Special || r.type == RoomType.Secret) 
                continue;

            r.floorCells = BuilderUtils.CellsInRect(r.bounds);
        }

        var allRooms = new List<RoomData>(rooms);
        ConnectLoop(loopA, allRooms, rng);
        ConnectLoop(loopB, allRooms, rng);

        BuilderUtils.AttachBranchRooms(rooms, allRooms, rng);

        var distances = MapGraph.BFSDistances(safe);
        foreach (var r in allRooms)
            r.distanceFromSafe = distances.TryGetValue(r, out var d) ? d : -1;

        return allRooms;
    }

    private static void ConnectLoop(List<RoomData> loop, List<RoomData> allRooms, DungeonRandom rng)
    {
        for (int i = 0; i < loop.Count; i++)
        {
            var a = loop[i];
            var b = loop[(i + 1) % loop.Count];
            BuilderUtils.ConnectWithTunnel(a, b, allRooms, rng);
        }
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
}
