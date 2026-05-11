using System.Collections.Generic;
using EnemySystem;
using UnityEngine;

/// <summary>
/// 절차적 맵 생성 진입점. Initialize → Build → Paint → (Phase 4 Spawn) 파이프라인을 호출한다.
/// 빌더/페인터/Feeling 은 외부에서 주입받아 결합도 낮춤.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; }

    [Header("타일맵 레이어 (GridSystem 과 동일한 참조 — 같은 GameObject 드래그)")]
    [SerializeField] private TerrainTilemaps tilemaps = new TerrainTilemaps();
    public TerrainTilemaps Tilemaps => tilemaps;

    [Header("설정")]
    [SerializeField] private GameConfig config;

    [Header("스폰 (옵션 — 미할당 시 적 없음)")]
    [Tooltip("EnemySpawner 컴포넌트 (KKH/EnemySystem). 매 Generate 호출마다 Spawn() 호출.")]
    [SerializeField] private EnemySpawner spawner;
    private IEnemySpawner Spawner => spawner;

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

        // 3. Paint — TerrainTilemaps 각 레이어에 타일 배치.
        feeling?.ApplyToPaint(painter);
        painter.Paint(rooms, tilemaps, feeling, Rng);

        // 4. Spawn — Spawner 가 할당됐으면 호출. Feeling 의 ApplyToSpawn 으로 사전 파라미터 변경 가능.
        if (Spawner != null)
        {
            feeling?.ApplyToSpawn(Spawner);
            Spawner.Spawn(rooms, depth, Rng);
        }

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
