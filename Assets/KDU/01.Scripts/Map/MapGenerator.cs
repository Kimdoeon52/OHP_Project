using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 절차적 맵 생성 진입점. Initialize → Build → Paint → (Phase 4 Spawn) 파이프라인을 호출한다.
/// 빌더/페인터/Feeling 은 외부에서 주입받아 결합도 낮춤.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; }

    [Header("타일맵 (GridSystem 과 동일한 타일맵 참조)")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("설정")]
    [SerializeField] private GameConfig config;

    public DungeonRandom Rng { get; } = new DungeonRandom();
    public List<RoomData> CurrentRooms { get; private set; }
    public int CurrentSeed { get; private set; }
    public int CurrentDepth { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("MapGenerator 가 씬에 두 개 이상 있습니다.");
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 한 층의 맵을 생성한다. (runSeed, depth) 조합이 같으면 항상 같은 결과.
    /// </summary>
    public List<RoomData> Generate(
        int runSeed,
        int depth,
        ILayoutBuilder builder,
        IRegionPainter painter,
        FloorFeeling feeling = null)
    {
        if (builder == null) { Debug.LogError("MapGenerator.Generate: builder 가 null"); return null; }
        if (painter == null) { Debug.LogError("MapGenerator.Generate: painter 가 null"); return null; }

        CurrentSeed = runSeed;
        CurrentDepth = depth;
        Rng.Init(DungeonRandom.DeriveFloorSeed(runSeed, depth));

        // 1. Initialize — 방 종류/개수/크기 결정
        var rooms = InitializeRooms(depth, Rng);
        feeling?.ApplyToInit(rooms);

        // 2. Build — 방 배치 + 연결 + 복도
        rooms = builder.Build(rooms, Rng);
        if (rooms == null)
        {
            Debug.LogError("MapGenerator.Generate: Builder 가 null 을 반환");
            return null;
        }

        // 3. Paint — Tilemap 에 타일 배치
        feeling?.ApplyToPaint(painter);
        painter.Paint(rooms, floorTilemap, wallTilemap, feeling, Rng);

        // 4. Spawn — Phase 4 에서 추가 (EnemySpawner.Spawn(rooms); feeling?.ApplyToSpawn(spawner);)

        CurrentRooms = rooms;
        return rooms;
    }

    /// <summary>
    /// Phase 1 임시 규칙 — Phase 2 LoopBuilder 가 동작할 최소 방 세트.
    /// 본 규칙(층별 가변 개수, depth 기반 Special/Secret 등) 은 후속 Phase 에서 정교화.
    /// </summary>
    private List<RoomData> InitializeRooms(int depth, DungeonRandom rng)
    {
        var rooms = new List<RoomData>
        {
            new RoomData { type = RoomType.Safe, sizeCategory = SizeCategory.Normal }
        };

        int normalCount = rng.Range(4, 7); // 4~6 개
        for (int i = 0; i < normalCount; i++)
        {
            rooms.Add(new RoomData
            {
                type = RoomType.Normal,
                sizeCategory = PickNormalSize(rng)
            });
        }

        rooms.Add(new RoomData { type = RoomType.Exit, sizeCategory = SizeCategory.Normal });
        return rooms;
    }

    private static SizeCategory PickNormalSize(DungeonRandom rng)
    {
        float v = rng.Value();
        if (v < 0.7f) return SizeCategory.Normal;
        if (v < 0.95f) return SizeCategory.Large;
        return SizeCategory.Giant;
    }
}
