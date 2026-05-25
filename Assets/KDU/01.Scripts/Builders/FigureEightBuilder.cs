using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 8자 빌더 — 두 루프가 하나의 공유 방(pinch)을 통해 연결
/// Safe 는 루프 A 에, Exit (+ Boss) 는 루프 B 에 배치 -> 두 갈래 동선 선택지 제공
/// </summary>
[CreateAssetMenu(fileName = "FigureEightBuilder", menuName = "60s Dungeon/Builders/Figure Eight Builder")]
public class FigureEightBuilder : ScriptableObject, ILayoutBuilder
{
    [Header("루프 배치")]
    [SerializeField] private int minLoopRadius = 8;
    [SerializeField] private int interRoomGap = 3;
    [Tooltip("두 루프 중심 사이 추가 여유 (공유 방이 두 루프에 동시에 끼이므로 0 이상 권장)")]
    [SerializeField] private int loopSeparationGap = 0;

    [Header("방 크기 범위")]
    [SerializeField] private Vector2Int normalSize = new Vector2Int(5, 7);
    [SerializeField] private Vector2Int largeSize = new Vector2Int(8, 11);
    [SerializeField] private Vector2Int giantSize = new Vector2Int(12, 15);
    [Tooltip("공유 방(pinch) 의 크기 카테고리 — 4방향 허브이므로 Large 권장")]
    [SerializeField] private SizeCategory sharedRoomSize = SizeCategory.Large;

    public List<RoomData> Build(List<RoomData> rooms, DungeonRandom rng)
    {
        if (rooms == null || rooms.Count < 4)
        {
            Debug.LogError("FigureEightBuilder.Build: 방이 너무 적음 (최소 4개 — Safe/Exit/공유/Normal 최소)");
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

        // 공유 방은 마지막 Normal 을 빼서 사용. Large 로 강제
        var shared = normals[normals.Count - 1];
        normals.RemoveAt(normals.Count - 1);
        shared.sizeCategory = sharedRoomSize;

        // 1. 방 사이즈 결정
        foreach (var r in rooms) AssignSize(r, rng);

        // 2. 두 루프 구성 — 공유 방을 양쪽 index 0 에 배치.
        // 루프 A 회전 순서: [shared, Safe, normals(전반)]
        // 루프 B 회전 순서: [shared, Exit, (Boss), normals(후반)]
        int half = normals.Count / 2;
        var loopA = new List<RoomData> { shared, safe };
        for (int i = 0; i < half; i++) loopA.Add(normals[i]);

        var loopB = new List<RoomData> { shared, exit };
        if (boss != null) loopB.Add(boss);
        for (int i = half; i < normals.Count; i++) loopB.Add(normals[i]);

        // 3. 반지름 산출
        int radiusA = BuilderUtils.ComputeLoopRadius(loopA, interRoomGap, minLoopRadius);
        int radiusB = BuilderUtils.ComputeLoopRadius(loopB, interRoomGap, minLoopRadius);

        // 4. 위치: 공유 방을 원점에 두기 위해 두 루프 중심을 X 축 좌우로 배치.
        // 루프 A 중심 = (-radiusA - loopSeparationGap/2, 0), index 0 (shared) 가 angle 0 (동쪽) -> world (0,0)
        // 루프 B 중심 = (+radiusB + loopSeparationGap/2, 0), index 0 (shared) 가 angle π (서쪽) -> world (0,0)
        int gapHalf = Mathf.Max(0, loopSeparationGap) / 2;
        var centerA = new Vector2Int(-(radiusA + gapHalf), 0);
        var centerB = new Vector2Int(+(radiusB + gapHalf), 0);
        BuilderUtils.PositionInLoop(loopA, centerA, radiusA, startAngleRad: 0f);
        BuilderUtils.PositionInLoop(loopB, centerB, radiusB, startAngleRad: Mathf.PI);

        // 5. 인테리어
        foreach (var r in rooms) r.floorCells = BuilderUtils.CellsInRect(r.bounds);

        // 6. 두 루프 cycle 연결.
        // 공유 방은 두 루프의 index 0 이므로 양쪽 사이클에서 각각 2개씩, 총 4개 터널이 모임
        var allRooms = new List<RoomData>(rooms);
        ConnectLoop(loopA, allRooms, rng);
        ConnectLoop(loopB, allRooms, rng);

        // 7. BFS Safe Room 거리
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
            case SizeCategory.Large: range = largeSize; break;
            case SizeCategory.Giant: range = giantSize; break;
            default: range = normalSize; break;
        }
        int w = rng.Range(range.x, range.y + 1);
        int h = rng.Range(range.x, range.y + 1);
        room.bounds = new RectInt(0, 0, w, h);
    }
}
