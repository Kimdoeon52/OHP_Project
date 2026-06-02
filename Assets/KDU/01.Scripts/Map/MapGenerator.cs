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

    [Tooltip("문 프리팹 — 1칸 통로(벽이 양옆으로 끊긴 입구)마다 배치")]
    [SerializeField] private GridDoor doorPrefab;
    private readonly List<GridDoor> _currentDoors = new();

    public DungeonRandom Rng { get; } = new DungeonRandom();
    public List<RoomData> CurrentRooms { get; private set; }
    public int CurrentSeed { get; private set; }
    public int CurrentDepth { get; private set; }
    public FloorFeeling CurrentFeeling { get; private set; }

    /// <summary>현재 층의 Hazard 셀 좌표 — 이동 처리부가 페널티 판정용으로 조회. Generate 호출마다 갱신.</summary>
    public HashSet<Vector2Int> HazardCells { get; } = new HashSet<Vector2Int>();

    /// <summary>Secret 방으로 들어가는 숨은 문 셀 — 페인터가 wall 로 봉인, 추후 문/탐색 시스템이 개통. Generate 호출마다 갱신.</summary>
    public HashSet<Vector2Int> SecretGatewayCells { get; } = new HashSet<Vector2Int>();

    /// <summary>Special 방으로 들어가는 잠긴 문 셀 — 통행 가능하되 추후 문/열쇠 시스템이 잠금 처리. Generate 호출마다 갱신.</summary>
    public HashSet<Vector2Int> LockedGatewayCells { get; } = new HashSet<Vector2Int>();

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
        SecretGatewayCells.Clear();
        LockedGatewayCells.Clear();
        DestroyCurrentExit();
        DestroyCurrentDoors();
        // 이전 층의 Feeling 효과 리셋
        if (VisionSystem.Instance != null) VisionSystem.Instance.VisionRadiusDelta = 0;
        // 열쇠는 해당 층에서만 사용 가능 (GDD 9.15) → 층 진입 시 폐기.
        if (KeyInventory.Instance != null) KeyInventory.Instance.ClearAll();
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

        // Special/Secret 막다른 가지의 입구 셀을 게이트 집합으로 노출.
        // 페인터가 Secret 게이트를 wall 로 봉인하므로 Paint 전에 채워야 함.
        DeriveGatewayCells(rooms);

        painter.Paint(rooms, tilemaps, feeling, Rng);

        Vector2Int? exitCell = FindExitCell(rooms);
        if (exitCell.HasValue) ClearHazardAround(exitCell.Value);

        SpawnExitMarker(exitCell);

        if (Spawner != null)
        {
            feeling?.ApplyToSpawn(Spawner);
            Spawner.Spawn(rooms, depth, Rng);
        }

        // 문은 스폰 뒤에 배치 — 닫힌 문이 스폰 단계의 도달성 판정을 막지 않도록.
        PlaceDoors(rooms);

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

            // 특수 방: 층당 Special 0~1, Secret 0~1 (GDD 6.2). 빌더가 막다른 가지로 따로 부착.
            float specialChance = config != null ? config.specialRoomChance : 0f;
            float secretChance = config != null ? config.secretRoomChance : 0f;
            if (rng.Chance(specialChance))
                rooms.Add(new RoomData { type = RoomType.Special, sizeCategory = SizeCategory.Normal });
            if (rng.Chance(secretChance))
                rooms.Add(new RoomData { type = RoomType.Secret, sizeCategory = SizeCategory.Normal });
        }

        rooms.Add(new RoomData { type = RoomType.Exit, sizeCategory = SizeCategory.Normal });
        return rooms;
    }

    /// <summary>
    /// Secret = 숨은 문(페인터가 wall 봉인), Special = 잠긴 문 - 구현해야됨.
    /// </summary>
    private void DeriveGatewayCells(List<RoomData> rooms)
    {
        SecretGatewayCells.Clear();
        LockedGatewayCells.Clear();
        foreach (var r in rooms)
        {
            if (r.type == RoomType.Secret)
                foreach (var c in r.gatewayCells)
                    SecretGatewayCells.Add(c);
            else if (r.type == RoomType.Special)
                foreach (var c in r.gatewayCells)
                    LockedGatewayCells.Add(c);
        }
    }

    private static Vector2Int? FindExitCell(List<RoomData> rooms)
    {
        var exitRoom = rooms.Find(r => r.type == RoomType.Exit);
        if (exitRoom == null || exitRoom.floorCells == null || exitRoom.floorCells.Count == 0) return null;

        Vector2 center = Vector2.zero;
        foreach (var c in exitRoom.floorCells) 
            center += c;
        
        center /= exitRoom.floorCells.Count;

        Vector2Int best = exitRoom.floorCells[0];
        float bestDist = float.MaxValue;
        foreach (var c in exitRoom.floorCells)
        {
            float d = ((Vector2)c - center).sqrMagnitude;
            if (d < bestDist) 
            { 
                bestDist = d; best = c; 
            }
        }
        return best;
    }

    // 출구 주변 hazard 청소. HazardFeeling 이 2-격자 정렬로만 칠하므로,
    // clear 영역도 짝수 시작·홀수 끝(2-aligned) 으로 맞춰 1칸짜리 remnant 가 안 남게 함.
    // 결과: 출구 3×3 을 덮는 4×4 짝수 정렬 블록이 비워짐.
    private void ClearHazardAround(Vector2Int center)
    {
        if (tilemaps == null || tilemaps.hazard == null) 
            return;
        
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
        Vector3 worldPos = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(exitCell.Value) : new Vector3(exitCell.Value.x, exitCell.Value.y, 0);
        _currentExit = Instantiate(exitPrefab, worldPos, Quaternion.identity);
    }

    private void DestroyCurrentExit()
    {
        if (_currentExit == null)
            return;
        if (Application.isPlaying)
            Destroy(_currentExit.gameObject);
        else
            DestroyImmediate(_currentExit.gameObject);
        _currentExit = null;
    }

    /// <summary>
    /// 방 입구(1칸 통로 = 벽이 양옆으로 끊긴 floor 셀)마다 문을 배치한다.
    /// 후보는 빌드 때 기록된 doorCells(방-복도 접점). 복도 중간엔 doorCell 이 없어 문이 안 생긴다.
    /// Secret(봉인 상태)·Tunnel 은 제외, 1칸 통로 형태가 아닌 셀(dissolve 로 넓어진 입구 등)도 제외.
    /// </summary>
    private void PlaceDoors(List<RoomData> rooms)
    {
        if (doorPrefab == null) return;

        var placed = new HashSet<Vector2Int>();
        foreach (var room in rooms)
        {
            if (room.type == RoomType.Secret || room.type == RoomType.Tunnel) continue;
            if (room.doorCells == null) continue;

            foreach (var cell in room.doorCells)
            {
                if (placed.Contains(cell)) continue;
                if (!IsDoorwayCell(cell, out bool horizontalPassage)) continue;

                placed.Add(cell);
                Vector3 pos = GridSystem.Instance != null
                    ? GridSystem.Instance.GridToWorld(cell)
                    : new Vector3(cell.x, cell.y, 0);
                var door = Instantiate(doorPrefab, pos, Quaternion.identity);
                var kind = LockedGatewayCells.Contains(cell)
                    ? GridDoor.DoorKind.Locked
                    : GridDoor.DoorKind.Normal;
                door.Initialize(cell, horizontalPassage, kind);
                _currentDoors.Add(door);
            }
        }
    }

    /// <summary>cell 이 1칸 폭 통로(양옆이 벽, 통로 방향만 트인 floor)인지. horizontalPassage = 좌우로 통하는지.</summary>
    private bool IsDoorwayCell(Vector2Int cell, out bool horizontalPassage)
    {
        horizontalPassage = false;
        if (!IsOpenFloor(cell)) return false;

        bool wallL = IsWallCell(cell + Vector2Int.left);
        bool wallR = IsWallCell(cell + Vector2Int.right);
        bool wallU = IsWallCell(cell + Vector2Int.up);
        bool wallD = IsWallCell(cell + Vector2Int.down);

        // 좌우로 통하는 입구: 위아래가 벽, 좌우가 트임.
        bool horiz = wallU && wallD && IsOpenFloor(cell + Vector2Int.left) && IsOpenFloor(cell + Vector2Int.right);
        // 상하로 통하는 입구: 좌우가 벽, 위아래가 트임.
        bool vert = wallL && wallR && IsOpenFloor(cell + Vector2Int.up) && IsOpenFloor(cell + Vector2Int.down);

        if (horiz == vert) return false; // 둘 다거나 둘 다 아니면 1칸 통로가 아님
        horizontalPassage = horiz;
        return true;
    }

    private bool IsWallCell(Vector2Int c)
    {
        return tilemaps != null && tilemaps.wall != null && tilemaps.wall.HasTile(new Vector3Int(c.x, c.y, 0));
    }

    // 통행 가능한 바닥(벽/구덩이 아래 깔린 floor 타일은 제외).
    private bool IsOpenFloor(Vector2Int c)
    {
        var cell = new Vector3Int(c.x, c.y, 0);
        if (tilemaps == null || tilemaps.floor == null || !tilemaps.floor.HasTile(cell)) return false;
        if (tilemaps.wall != null && tilemaps.wall.HasTile(cell)) return false;
        if (tilemaps.chasm != null && tilemaps.chasm.HasTile(cell)) return false;
        return true;
    }

    private void DestroyCurrentDoors()
    {
        foreach (var door in _currentDoors)
        {
            if (door == null) continue;
            if (Application.isPlaying) Destroy(door.gameObject);
            else DestroyImmediate(door.gameObject);
        }
        _currentDoors.Clear();
    }

    private static SizeCategory PickNormalSize(DungeonRandom rng)
    {
        float v = rng.Value();
        if (v < 0.7f) 
            return SizeCategory.Normal;
        if (v < 0.95f) 
            return SizeCategory.Large;
        return SizeCategory.Giant;
    }
}
