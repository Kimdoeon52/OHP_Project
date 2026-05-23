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

    [Header("Floor Feeling 풀 (옵션 — Generate 호출 시 feeling 인자가 null 이면 풀에서 자동 선택)")]
    [SerializeField] private FloorFeelingPool feelingPool;

    [Header("빌더/페인터 풀 (옵션 — 인자가 null 이면 풀에서 무작위 선택)")]
    [SerializeField] private BuilderPool builderPool;
    [SerializeField] private PainterPool painterPool;

    [Header("보스 층 규칙")]
    [Tooltip("보스 등장 주기 — N의 배수 depth. 0 = 보스 없음. 기본 5 → depth 5/10/15...")]
    [SerializeField] private int bossDepthInterval = 5;

    [Tooltip("보스 층은 절차생성을 스킵하고 이 로더가 prefab 을 로드. null 이면 보스 층도 절차생성됨.")]
    [SerializeField] private BossRoomLoader bossRoomLoader;

    public DungeonRandom Rng { get; } = new DungeonRandom();
    public List<RoomData> CurrentRooms { get; private set; }
    public int CurrentSeed { get; private set; }
    public int CurrentDepth { get; private set; }
    public FloorFeeling CurrentFeeling { get; private set; }

    /// <summary>현재 층의 Hazard 셀 좌표 — 이동 처리부가 페널티 판정용으로 조회. Generate 호출마다 갱신.</summary>
    public HashSet<Vector2Int> HazardCells { get; } = new HashSet<Vector2Int>();

    /// <summary>depth → 영역(1=하수구, 2=던전, 3=지옥). GDD 5.1 — 1~5 / 6~10 / 11~15.</summary>
    public static int AreaFromDepth(int depth)
    {
        if (depth <= 5) return 1;
        if (depth <= 10) return 2;
        return 3;
    }

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
        ILayoutBuilder builder = null,
        IRegionPainter painter = null,
        FloorFeeling feeling = null)
    {

        // Edit 모드 (ContextMenu 호출) 에서는 Awake 가 안 돌아 Instance 가 null.
        // 페인터/스폰러가 Instance 로 HazardCells/CurrentDepth 등을 조회하므로 방어적으로 세팅.
        if (Instance == null) Instance = this;

        CurrentSeed = runSeed;
        CurrentDepth = depth;
        HazardCells.Clear();
        // 이전 층의 Feeling 효과 리셋 — DarkFeeling 의 VisionRadiusDelta 가 다음 층까지 새지 않게.
        if (VisionSystem.Instance != null) VisionSystem.Instance.VisionRadiusDelta = 0;
        Rng.Init(DungeonRandom.DeriveFloorSeed(runSeed, depth));

        // 보스 층 분기 — 절차생성을 스킵하고 사전 제작 prefab 로드 (MAP_GENERATION.md 1.1).
        if (IsBossDepth(depth) && bossRoomLoader != null)
        {
            var bossRooms = bossRoomLoader.Load(depth, tilemaps);
            if (bossRooms == null) return null;
            CurrentRooms = bossRooms;
            CurrentFeeling = null;
            if (Spawner != null) Spawner.Spawn(bossRooms, depth, Rng);
            Debug.Log($"[MapGenerator] depth={depth} 보스 prefab 로드 — 절차생성 스킵");
            return bossRooms;
        }

        // 0. 풀 fallback — 호출자가 명시하지 않으면 풀에서 뽑음. Rng.Init 직후 결정론 유지를 위해 항상 같은 순서.
        if (feeling == null && feelingPool != null) feeling = feelingPool.Pick(Rng, depth);
        if (builder == null && builderPool != null) builder = builderPool.Pick(Rng);
        if (painter == null && painterPool != null) painter = painterPool.Pick(Rng, depth);

        if (builder == null) { Debug.LogError("MapGenerator.Generate: builder 가 null (인자/풀 모두 미할당)"); return null; }
        if (painter == null) { Debug.LogError("MapGenerator.Generate: painter 가 null (인자/풀 모두 미할당)"); return null; }

        CurrentFeeling = feeling;
        if (feelingPool == null)
            Debug.LogWarning("[MapGenerator] feelingPool 슬롯 비어있음 — Feeling 적용 안 됨");
        else if (feeling == null)
            Debug.Log($"[MapGenerator] depth={depth} 에서 Feeling 미발동 (triggerChance 또는 area 가중치 확인)");
        else
            Debug.Log($"[MapGenerator] depth={depth} Feeling 발동: {feeling.Id}");

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
        // 페인터가 feeling 인자를 직접 읽으므로 ApplyToPaint 별도 훅 불필요 (PHASE_3_5_PLAN.md Q1 결정 A).
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

    /// <summary>depth 가 보스 주기(기본 5) 의 배수인지.</summary>
    public bool IsBossDepth(int depth)
    {
        return bossDepthInterval > 0 && depth > 0 && depth % bossDepthInterval == 0;
    }

    /// <summary>
    /// 한 층의 방 리스트 초기화. 보스 층(depth 가 bossDepthInterval 배수, 로더 미할당 시) 은 Safe + Boss(Giant) + Exit 3방.
    /// 일반 층은 Safe + Normal × 가변 + Exit.
    /// 본 규칙(Special/Secret 등) 은 후속 Phase 에서 정교화.
    /// </summary>
    private List<RoomData> InitializeRooms(int depth, DungeonRandom rng)
    {
        var rooms = new List<RoomData>
        {
            new RoomData { type = RoomType.Safe, sizeCategory = SizeCategory.Normal }
        };

        if (IsBossDepth(depth))
        {
            // 보스 층: Safe + Boss(Giant) + Exit. Normal 없음. Exit 는 보스 처치 시 열림 (게임 플로우 별도).
            rooms.Add(new RoomData { type = RoomType.Boss, sizeCategory = SizeCategory.Giant });
        }
        else
        {
            int normalCount = rng.Range(4, 7); // 4~6 개
            for (int i = 0; i < normalCount; i++)
            {
                rooms.Add(new RoomData
                {
                    type = RoomType.Normal,
                    sizeCategory = PickNormalSize(rng)
                });
            }
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
