using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Phase 2 통합 테스트 — Inspector 우클릭 ContextMenu 로 실행.
/// 1) 단일 생성: runSeed/depth 로 한 층 생성 + 통계 로그
/// 2) 결정론 테스트: 같은 시드 2회 → 결과 동일?
/// 3) 100 시드 스트레스: 무작위 시드들 모두 entrance↔exit 연결성 통과?
/// </summary>
public class MapGenerationTester : MonoBehaviour
{
    [Header("의존성")]
    [SerializeField] private MapGenerator generator;
    [SerializeField] private LoopBuilder builder;
    [SerializeField] private DefaultPainter painter;

    [Header("입력")]
    [SerializeField] private int runSeed = 12345;
    [SerializeField] private int depth = 1;
    [SerializeField] private int stressTestCount = 100;

    [ContextMenu("1) 단일 생성")]
    public void GenerateOnce()
    {
        if (!ValidateRefs()) return;
        var rooms = generator.Generate(runSeed, depth, builder, painter);
        Report(rooms);
    }

    [ContextMenu("2) 결정론 테스트")]
    public void DeterminismTest()
    {
        if (!ValidateRefs()) return;
        var hash1 = HashRooms(generator.Generate(runSeed, depth, builder, painter));
        var hash2 = HashRooms(generator.Generate(runSeed, depth, builder, painter));
        bool ok = hash1 == hash2;
        Debug.Log($"[Determinism seed={runSeed} depth={depth}] {(ok ? "PASS" : "FAIL")}  hash1={hash1}  hash2={hash2}");
    }

    [ContextMenu("3) 100 시드 연결성 테스트")]
    public void StressTest()
    {
        if (!ValidateRefs()) return;
        int passed = 0;
        var failedSeeds = new List<int>();
        for (int i = 0; i < stressTestCount; i++)
        {
            int seed = unchecked(runSeed + i * 7919);
            var rooms = generator.Generate(seed, depth, builder, painter);
            if (rooms != null && MapGraph.IsConnected(rooms)) passed++;
            else failedSeeds.Add(seed);
        }
        if (failedSeeds.Count == 0)
            Debug.Log($"[Stress] {passed}/{stressTestCount} PASS — 모두 entrance↔exit 연결됨");
        else
            Debug.LogError($"[Stress] {passed}/{stressTestCount} pass, 실패 시드: {string.Join(", ", failedSeeds)}");
    }

    private bool ValidateRefs()
    {
        if (generator == null) { Debug.LogError("Tester: MapGenerator 참조 없음"); return false; }
        if (builder == null) { Debug.LogError("Tester: LoopBuilder 에셋 없음"); return false; }
        if (painter == null) { Debug.LogError("Tester: DefaultPainter 에셋 없음"); return false; }
        return true;
    }

    private void Report(List<RoomData> rooms)
    {
        if (rooms == null) { Debug.LogError("Report: rooms null"); return; }
        int roomCount = 0, tunnelCount = 0;
        foreach (var r in rooms)
        {
            if (r.type == RoomType.Tunnel) tunnelCount++;
            else roomCount++;
        }
        bool connected = MapGraph.IsConnected(rooms);
        bool hasBoss = rooms.Exists(r => r.type == RoomType.Boss);
        var sb = new StringBuilder();
        sb.AppendLine($"[Map seed={runSeed} depth={depth}] 방 {roomCount} / 터널 {tunnelCount} / 연결 {(connected ? "OK" : "FAIL")}{(hasBoss ? " / BOSS 층" : "")}");
        foreach (var r in rooms)
        {
            if (r.type == RoomType.Tunnel) continue;
            sb.AppendLine(
                $"  - {r.type,-7} {r.sizeCategory,-6} bounds={r.bounds} cells={r.floorCells.Count} dist={r.distanceFromSafe} conn={r.connections.Count}");
        }
        Debug.Log(sb.ToString());
    }

    /// <summary>방 리스트 결정론 검증용 해시 — type/bounds/floorCells.Count/distance.</summary>
    private static string HashRooms(List<RoomData> rooms)
    {
        if (rooms == null) return "<null>";
        unchecked
        {
            int h = 17;
            foreach (var r in rooms)
            {
                h = h * 31 + (int)r.type;
                h = h * 31 + r.bounds.GetHashCode();
                h = h * 31 + (r.floorCells?.Count ?? 0);
                h = h * 31 + r.distanceFromSafe;
            }
            return h.ToString("X8");
        }
    }
}
