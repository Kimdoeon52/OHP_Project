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

    [Header("타일맵 레이어")]
    [SerializeField] private TerrainTilemaps tilemaps = new TerrainTilemaps();
    public TerrainTilemaps Tilemaps => tilemaps;

    [Header("설정")]
    [SerializeField] private GameConfig config;

    [SerializeField] private EnemySpawner spawner;
    private IEnemySpawner Spawner => spawner;

    [SerializeField] private FloorFeelingPool feelingPool;

    [SerializeField] private BuilderPool builderPool;
    [SerializeField] private PainterPool painterPool;

    [Header("보스 층 배수")]
    [SerializeField] private int bossDepthInterval = 5;

    [Tooltip("보스 층 로더")]
    [SerializeField] private BossRoomLoader bossRoomLoader;

    [Tooltip("Exit")]
    [SerializeField] private GridExit exitPrefab;
    private GridExit _currentExit;

    public DungeonRandom Rng { get; } = new DungeonRandom();
    public List<RoomData> CurrentRooms { get; private set; }
    public int CurrentSeed { get; private set; }
    public int CurrentDepth { get; private set; }
    public FloorFeeling CurrentFeeling { get; private set; }

    /// <summary>현재 층의 Hazard 셀 좌표 — 이동 처리부가 페널티 판정용으로 조회. Generate 호출마다 갱신.</summary>
    public HashSet<Vector2Int> HazardCells { get; } = new HashSet<Vector2Int>();

    /// <summary>depth -> 영역(1=하수구, 2=던전, 3=지옥)</summary>
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
    /// 시드 기반 한 층의 맵을 생성한다.
    /// </summary>
    public List<RoomData> Generate(
        int runSeed,
        int depth,
        ILayoutBuilder builder = null,
        IRegionPainter painter = null,
        FloorFeeling feeling = null)
    {

        if (Instance == null) Instance = this;

        CurrentSeed = runSeed;
        CurrentDepth = depth;
        HazardCells.Clear();
        DestroyCurrentExit();
        // 이전 층의 Feeling 효과 리셋
        if (VisionSystem.Instance != null) VisionSystem.Instance.VisionRadiusDelta = 0;
        Rng.Init(DungeonRandom.DeriveFloorSeed(runSeed, depth));

        // 보스 층 분기
        if (IsBossDepth(depth) && bossRoomLoader != null)
        {
            var bossRooms = bossRoomLoader.Load(depth, tilemaps, out var bossExitCell);
            if (bossRooms == null) return null;
            CurrentRooms = bossRooms;
            CurrentFeeling = null;
            SpawnExitMarker(bossExitCell);
            if (Spawner != null) Spawner.Spawn(bossRooms, depth, Rng);
            return bossRooms;
        }

        if (feeling == null && feelingPool != null) feeling = feelingPool.Pick(Rng, depth);
        if (builder == null && builderPool != null) builder = builderPool.Pick(Rng);
        if (painter == null && painterPool != null) painter = painterPool.Pick(Rng, depth);

        if (builder == null) { Debug.LogError("[MapGenerator] builder 가 null"); return null; }
        if (painter == null) { Debug.LogError("[MapGenerator] painter 가 null"); return null; }

        CurrentFeeling = feeling;
        if (feelingPool == null)
            Debug.LogWarning("[MapGenerator] feelingPool 슬롯 비어있음 — Feeling 적용 안 됨");
        else if (feeling == null)
            Debug.Log($"[MapGenerator] depth={depth} 에서 Feeling 미발동");
        else
            Debug.Log($"[MapGenerator] depth={depth} Feeling 발동: {feeling.Id}");

        var rooms = InitializeRooms(depth, Rng);
        feeling?.ApplyToInit(rooms);

        rooms = builder.Build(rooms, Rng);
        if (rooms == null)
        {
            Debug.LogError("[MapGenerator] Builder가 null 반환함");
            return null;
        }

        painter.Paint(rooms, tilemaps, feeling, Rng);

        Vector2Int? exitCell = FindExitCell(rooms);
        if (exitCell.HasValue) ClearHazardAround(exitCell.Value);

        SpawnExitMarker(exitCell);

        if (Spawner != null)
        {
            feeling?.ApplyToSpawn(Spawner);
            Spawner.Spawn(rooms, depth, Rng);
        }

        CurrentRooms = rooms;
        return rooms;
    }

    public bool IsBossDepth(int depth)
    {
        return bossDepthInterval > 0 && depth > 0 && depth % bossDepthInterval == 0;
    }

    private List<RoomData> InitializeRooms(int depth, DungeonRandom rng)
    {
        var rooms = new List<RoomData>
        {
            new RoomData { type = RoomType.Safe, sizeCategory = SizeCategory.Normal }
        };

        if (IsBossDepth(depth))
        {
            // 보스 층: Safe + Boss(Giant) + Exit. Normal 없음. Exit 는 보스 처치 시 열림
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

    private static Vector2Int? FindExitCell(List<RoomData> rooms)
    {
        var exitRoom = rooms.Find(r => r.type == RoomType.Exit);
        if (exitRoom == null || exitRoom.floorCells == null || exitRoom.floorCells.Count == 0) return null;

        Vector2 center = Vector2.zero;
        foreach (var c in exitRoom.floorCells) center += c;
        center /= exitRoom.floorCells.Count;

        Vector2Int best = exitRoom.floorCells[0];
        float bestDist = float.MaxValue;
        foreach (var c in exitRoom.floorCells)
        {
            float d = ((Vector2)c - center).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = c; }
        }
        return best;
    }

    // 출구 주변 hazard 청소. HazardFeeling 이 2-격자 정렬로만 칠하므로,
    // clear 영역도 짝수 시작·홀수 끝(2-aligned) 으로 맞춰 1칸짜리 remnant 가 안 남게 함.
    // 결과: 출구 3×3 을 덮는 4×4 짝수 정렬 블록이 비워짐.
    private void ClearHazardAround(Vector2Int center)
    {
        if (tilemaps == null || tilemaps.hazard == null) return;
        int minX = (center.x - 1) & ~1;
        int maxX = (center.x + 1) | 1;
        int minY = (center.y - 1) & ~1;
        int maxY = (center.y + 1) | 1;
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                tilemaps.hazard.SetTile(new Vector3Int(x, y, 0), null);
                HazardCells.Remove(new Vector2Int(x, y));
            }
        }
    }

    private void SpawnExitMarker(Vector2Int? exitCell)
    {
        if (exitPrefab == null || !exitCell.HasValue) return;
        Vector3 worldPos = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(exitCell.Value)
            : new Vector3(exitCell.Value.x, exitCell.Value.y, 0);
        _currentExit = Instantiate(exitPrefab, worldPos, Quaternion.identity);
    }

    private void DestroyCurrentExit()
    {
        if (_currentExit == null) return;
        if (Application.isPlaying) Destroy(_currentExit.gameObject);
        else DestroyImmediate(_currentExit.gameObject);
        _currentExit = null;
    }

    private static SizeCategory PickNormalSize(DungeonRandom rng)
    {
        float v = rng.Value();
        if (v < 0.7f) return SizeCategory.Normal;
        if (v < 0.95f) return SizeCategory.Large;
        return SizeCategory.Giant;
    }
}
