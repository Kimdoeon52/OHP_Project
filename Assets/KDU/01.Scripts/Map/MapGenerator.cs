using System.Collections.Generic;
using EnemySystem;
using UnityEngine;

/// <summary>
/// 절차적 맵 생성 진입점. Initialize → Build → Paint → (Phase 4 Spawn) 파이프라인을 호출한다.
/// 빌더/페인터/Feeling 은 외부에서 주입받아 결합도 낮춤.
/// </summary>
public class MapGenerator : Singleton<MapGenerator>
{

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

    [Tooltip("스페셜 방 열쇠 픽업 프리팹 (GDD 9.15). Special 방이 있는 층에 1개 스폰.")]
    [SerializeField] private KeyPickup specialKeyPickupPrefab;
    private readonly List<KeyPickup> _currentKeys = new();

    [Tooltip("Special/Secret 방 상자 배치 스포너 (GDD 6.2.1/6.2.2 · 9.14).")]
    [SerializeField] private ChestSpawner chestSpawner;

    [Tooltip("바닥 아이템 스포너 — 고정 스케줄(힘포션/강화스크롤) + 랜덤 아이템 (GDD 9).")]
    [SerializeField] private MapItemSpawner mapItemSpawner;

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

    /// <summary>Safe Room 출구 문 셀 — 처음엔 일반 문처럼 자동 개폐, 플레이어가 한 번 나가면 영구 잠김 (GDD 11.1).</summary>
    public HashSet<Vector2Int> SafeGatewayCells { get; } = new HashSet<Vector2Int>();

    /// <summary>현재 층의 월드 바운드 (floor 셀 + 패딩). 카메라 팬 제한 등에 사용. Generate 호출마다 갱신.</summary>
    public Bounds MapWorldBounds { get; private set; }
    public bool MapWorldBoundsValid { get; private set; }

    /// <summary>depth -> 영역(1=하수구, 2=던전, 3=지옥)</summary>
    public static int AreaFromDepth(int depth)
    {
        if (depth <= 5) return 1;
        if (depth <= 10) return 2;
        return 3;
    }

    protected override void Awake()
    {
        base.Awake();
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

        CurrentSeed = runSeed;
        CurrentDepth = depth;
        HazardCells.Clear();
        SecretGatewayCells.Clear();
        LockedGatewayCells.Clear();
        SafeGatewayCells.Clear();
        DestroyCurrentExit();
        DestroyCurrentDoors();
        DestroyCurrentKeys();
        if (chestSpawner != null) chestSpawner.Clear();
        if (mapItemSpawner != null) mapItemSpawner.Clear();
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

        // 열쇠 픽업 배치 — Special 방이 있으면 스페셜 키 1개를 Normal/Exit 방 임의 셀에 (GDD 9.15).
        SpawnKeys(rooms);

        // 상자 배치 — Special/Secret 방 상자 구성 + Normal/Exit 랜덤 상자 20% (GDD 6.2.1/6.2.2 · 9.14).
        if (chestSpawner != null) chestSpawner.Spawn(rooms, Rng);

        // 바닥 아이템 — 고정 스케줄(힘포션/강화스크롤) + 랜덤 아이템 (GDD 9).
        if (mapItemSpawner != null) mapItemSpawner.Spawn(rooms, depth, CurrentSeed, Rng);

        ComputeMapBounds(rooms);

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
        SafeGatewayCells.Clear();
        foreach (var r in rooms)
        {
            if (r.type == RoomType.Secret)
                foreach (var c in r.gatewayCells)
                    SecretGatewayCells.Add(c);
            else if (r.type == RoomType.Special)
                foreach (var c in r.gatewayCells)
                    LockedGatewayCells.Add(c);
            else if (r.type == RoomType.Safe)
                foreach (var c in r.gatewayCells)
                    SafeGatewayCells.Add(c);
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

        // SafeExit 의 '바깥 셀' 계산용 — Safe 인테리어 셀 집합.
        var safeRoom = rooms.Find(r => r.type == RoomType.Safe);
        HashSet<Vector2Int> safeFloor = (safeRoom != null && safeRoom.floorCells != null)
            ? new HashSet<Vector2Int>(safeRoom.floorCells)
            : null;

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
                GridDoor.DoorKind kind;
                if (SafeGatewayCells.Contains(cell)) kind = GridDoor.DoorKind.SafeExit;
                else if (LockedGatewayCells.Contains(cell)) kind = GridDoor.DoorKind.Locked;
                else kind = GridDoor.DoorKind.Normal;

                var door = SpawnDoor(cell, horizontalPassage, kind);
                if (kind == GridDoor.DoorKind.SafeExit && safeFloor != null)
                {
                    Vector2Int outside = ComputeSafeOutsideCell(cell, horizontalPassage, safeFloor);
                    door.ConfigureSafeExit(outside);
                }
            }
        }

        // Secret 게이트: 페인터가 wall 로 봉인한 셀에 숨겨진 문 배치.
        // 셀 자체는 wall 이므로 IsDoorwayCell 의 floor 전제 검사를 통과 못함 — 별도 판정 사용.
        int secretSpawned = 0;
        foreach (var cell in SecretGatewayCells)
        {
            if (placed.Contains(cell)) continue;
            if (!IsSecretDoorwayCell(cell, out bool horizontalPassage)) continue;
            placed.Add(cell);
            SpawnDoor(cell, horizontalPassage, GridDoor.DoorKind.Secret);
            secretSpawned++;
        }
        if (SecretGatewayCells.Count > 0)
            Debug.Log($"[MapGenerator] Secret 게이트 {SecretGatewayCells.Count}칸 중 {secretSpawned}개 문 배치");
    }

    private GridDoor SpawnDoor(Vector2Int cell, bool horizontalPassage, GridDoor.DoorKind kind)
    {
        Vector3 pos = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);
        var door = Instantiate(doorPrefab, pos, Quaternion.identity);
        door.Initialize(cell, horizontalPassage, kind);
        _currentDoors.Add(door);
        return door;
    }

    /// <summary>
    /// Safe 게이트 셀의 '바깥' 쪽 인접 셀 — 통로 축 두 이웃 중 Safe.floorCells 에 없는 쪽.
    /// 플레이어가 이 셀에 도달하면 = Safe 를 빠져나간 시점으로 간주.
    /// </summary>
    private static Vector2Int ComputeSafeOutsideCell(Vector2Int cell, bool horizontalPassage, HashSet<Vector2Int> safeFloor)
    {
        if (horizontalPassage)
        {
            var L = cell + Vector2Int.left;
            return safeFloor.Contains(L) ? cell + Vector2Int.right : L;
        }
        else
        {
            var U = cell + Vector2Int.up;
            return safeFloor.Contains(U) ? cell + Vector2Int.down : U;
        }
    }

    /// <summary>
    /// Secret 게이트 셀(wall 봉인) 통로 방향 판정.
    /// Secret 게이트는 짝(2칸)이라 통로 축에서 한쪽은 floor(호스트/터널), 반대편은 다른 게이트(wall) 형태.
    /// → "수직축 둘 다 wall + 수평축 중 한 쪽이 floor" 이면 좌우 통로, 그 반대면 상하 통로.
    /// </summary>
    private bool IsSecretDoorwayCell(Vector2Int cell, out bool horizontalPassage)
    {
        horizontalPassage = false;
        bool floorL = IsOpenFloor(cell + Vector2Int.left);
        bool floorR = IsOpenFloor(cell + Vector2Int.right);
        bool floorU = IsOpenFloor(cell + Vector2Int.up);
        bool floorD = IsOpenFloor(cell + Vector2Int.down);
        bool horiz = !floorU && !floorD && (floorL || floorR);
        bool vert = !floorL && !floorR && (floorU || floorD);
        if (horiz == vert) return false;
        horizontalPassage = horiz;
        return true;
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

    /// <summary>
    /// 열쇠 픽업 배치. Special 방이 한 층에 있으면 SpecialRoom 키 1개를 Normal/Exit 방의
    /// floor 셀(doorCells 인접·HazardCells 제외) 중 무작위 1곳에. (GDD 9.15)
    /// </summary>
    private void SpawnKeys(List<RoomData> rooms)
    {
        if (specialKeyPickupPrefab == null) return;
        if (rooms == null) return;
        if (!rooms.Exists(r => r.type == RoomType.Special)) return;

        // 입구 인접 셀 제외 — 문 옆에 바로 떨어지면 자동 개폐와 겹쳐 어색.
        var avoid = new HashSet<Vector2Int>();
        foreach (var room in rooms)
        {
            if (room.doorCells == null) continue;
            foreach (var d in room.doorCells)
            {
                avoid.Add(d);
                avoid.Add(d + Vector2Int.up);
                avoid.Add(d + Vector2Int.down);
                avoid.Add(d + Vector2Int.left);
                avoid.Add(d + Vector2Int.right);
            }
        }

        var candidates = new List<Vector2Int>();
        foreach (var room in rooms)
        {
            if (room.type != RoomType.Normal && room.type != RoomType.Exit) continue;
            if (room.floorCells == null) continue;
            foreach (var c in room.floorCells)
            {
                if (avoid.Contains(c)) continue;
                if (HazardCells.Contains(c)) continue;
                candidates.Add(c);
            }
        }
        if (candidates.Count == 0) return;

        var cell = Rng.Pick(candidates);
        Vector3 pos = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);
        var pickup = Instantiate(specialKeyPickupPrefab, pos, Quaternion.identity);
        pickup.Initialize(cell, KeyType.SpecialRoom);
        _currentKeys.Add(pickup);
    }

    private void DestroyCurrentKeys()
    {
        foreach (var key in _currentKeys)
        {
            if (key == null) continue;
            if (Application.isPlaying) Destroy(key.gameObject);
            else DestroyImmediate(key.gameObject);
        }
        _currentKeys.Clear();
    }

    /// <summary>현재 층의 floor 셀 + 패딩을 감싸는 월드 바운드 계산. CameraFollow 가 팬 제한에 사용.</summary>
    private void ComputeMapBounds(List<RoomData> rooms)
    {
        MapWorldBoundsValid = false;
        if (rooms == null || GridSystem.Instance == null) return;

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        bool any = false;
        foreach (var r in rooms)
        {
            if (r.floorCells == null) continue;
            foreach (var c in r.floorCells)
            {
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
                any = true;
            }
        }
        if (!any) return;

        // 외벽이 보이도록 약간 여유.
        const int padding = 2;
        minX -= padding; minY -= padding;
        maxX += padding; maxY += padding;

        var wMin = GridSystem.Instance.GridToWorld(new Vector2Int(minX, minY));
        var wMax = GridSystem.Instance.GridToWorld(new Vector2Int(maxX, maxY));
        MapWorldBounds = new Bounds(
            (wMin + wMax) * 0.5f,
            new Vector3(Mathf.Abs(wMax.x - wMin.x), Mathf.Abs(wMax.y - wMin.y), 0f));
        MapWorldBoundsValid = true;
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
