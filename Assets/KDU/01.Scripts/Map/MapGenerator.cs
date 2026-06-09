using System.Collections.Generic;
using EnemySystem;
using UnityEngine;

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

    [Tooltip("보스 층")]
    [SerializeField] private BossRoomLoader bossRoomLoader;

    [Tooltip("Exit")]
    [SerializeField] private GridExit exitPrefab;
    private GridExit _currentExit;

    private Vector2Int? _pendingBossExitCell;

    [Tooltip("문 프리팹")]
    [SerializeField] private GridDoor doorPrefab;
    private readonly List<GridDoor> _currentDoors = new();

    [Tooltip("스페셜 방 열쇠 프리팹")]
    [SerializeField] private KeyPickup specialKeyPickupPrefab;
    private readonly List<KeyPickup> _currentKeys = new();

    [Tooltip("Special/Secret 방 상자 배치 스포너")]
    [SerializeField] private ChestSpawner chestSpawner;

    [Tooltip("바닥 아이템 스포너")]
    [SerializeField] private MapItemSpawner mapItemSpawner;

    [Tooltip("상점 스포너 (1/6/11층)")]
    [SerializeField] private ShopSpawner shopSpawner;

    public DungeonRandom Rng { get; } = new DungeonRandom();
    public List<RoomData> CurrentRooms { get; private set; }
    public int CurrentSeed { get; private set; }
    public int CurrentDepth { get; private set; }
    public FloorFeeling CurrentFeeling { get; private set; }

    // 현재 층의 Hazard 셀 좌표
    public HashSet<Vector2Int> HazardCells { get; } = new HashSet<Vector2Int>();

    // Secret 방으로 들어가는 숨은 문 셀
    public HashSet<Vector2Int> SecretGatewayCells { get; } = new HashSet<Vector2Int>();

    // Special 방으로 들어가는 잠긴 문 셀
    public HashSet<Vector2Int> LockedGatewayCells { get; } = new HashSet<Vector2Int>();

    // Safe Room 출구 문 셀
    public HashSet<Vector2Int> SafeGatewayCells { get; } = new HashSet<Vector2Int>();

    // 현재 층의 월드 바운드
    public Bounds MapWorldBounds { get; private set; }
    public bool MapWorldBoundsValid { get; private set; }

    // depth로 영역 구분 (1=하수구, 2=던전, 3=지옥)
    public static int AreaFromDepth(int depth)
    {
        if (depth <= 5) 
            return 1;

        if (depth <= 10) 
            return 2;

        return 3;
    }

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        GameEvents.OnBossDefeated += OnBossDefeated;
    }

    private void OnDisable()
    {
        GameEvents.OnBossDefeated -= OnBossDefeated;
    }

    // 보스 처치 시 출구 생성
    private void OnBossDefeated()
    {
        if (!_pendingBossExitCell.HasValue)
            return;

        SpawnExitMarker(_pendingBossExitCell);
        _pendingBossExitCell = null;
    }


    // 시드 기반 한 층의 맵을 생성한다.
    public List<RoomData> Generate( int runSeed, int depth, ILayoutBuilder builder = null, IRegionPainter painter = null, FloorFeeling feeling = null)
    {

        CurrentSeed = runSeed;
        CurrentDepth = depth;
        HazardCells.Clear();
        SecretGatewayCells.Clear();
        LockedGatewayCells.Clear();
        SafeGatewayCells.Clear();
        _pendingBossExitCell = null;
        DestroyCurrentExit();
        DestroyCurrentDoors();
        DestroyCurrentKeys();

        if (chestSpawner != null) 
            chestSpawner.Clear();

        if (mapItemSpawner != null) 
            mapItemSpawner.Clear();

        // 이전 층의 Feeling 효과 리셋
        if (VisionSystem.Instance != null)
            VisionSystem.Instance.VisionRadiusDelta = 0;

        // 열쇠는 해당 층에서만 사용 가능
        if (KeyInventory.Instance != null)
            KeyInventory.Instance.ClearAll();

        Rng.Init(DungeonRandom.DeriveFloorSeed(runSeed, depth));

        // 보스 층 분기
        if (IsBossDepth(depth) && bossRoomLoader != null)
        {
            var bossRooms = bossRoomLoader.Load(depth, tilemaps, out var bossExitCell);

            if (bossRooms == null) 
                return null;

            CurrentRooms = bossRooms;
            CurrentFeeling = null;

            _pendingBossExitCell = bossExitCell;

            if (Spawner != null)
                Spawner.Spawn(bossRooms, depth, Rng);

            return bossRooms;
        }

        if (feeling == null && feelingPool != null) 
            feeling = feelingPool.Pick(Rng, depth);
        if (builder == null && builderPool != null) 
            builder = builderPool.Pick(Rng);
        if (painter == null && painterPool != null) 
            painter = painterPool.Pick(Rng, depth);

        if (builder == null) 
        { 
            Debug.LogError("[MapGenerator] builder 가 null"); 
            return null; 
        }
        if (painter == null) 
        { 
            Debug.LogError("[MapGenerator] painter 가 null"); 
            return null; 
        }

        CurrentFeeling = feeling;
        if (feelingPool == null)
            Debug.LogWarning("[MapGenerator] feelingpool 비었음");

        var rooms = InitializeRooms(depth, Rng);
        feeling?.ApplyToInit(rooms);

        rooms = builder.Build(rooms, Rng);
        if (rooms == null)
            return null;

        DeriveGatewayCells(rooms);

        painter.Paint(rooms, tilemaps, feeling, Rng);

        Vector2Int? exitCell = FindExitCell(rooms);

        if (exitCell.HasValue) 
            ClearHazardAround(exitCell.Value);

        SpawnExitMarker(exitCell);

        if (Spawner != null)
        {
            feeling?.ApplyToSpawn(Spawner);
            Spawner.Spawn(rooms, depth, Rng);
        }

        // 문 배치
        PlaceDoors(rooms);

        // 열쇠 배치
        SpawnKeys(rooms);

        // 상자 배치
        if (chestSpawner != null) 
            chestSpawner.Spawn(rooms, Rng);

        // 바닥 아이템 배치
        if (mapItemSpawner != null)
            mapItemSpawner.Spawn(rooms, depth, CurrentSeed, Rng);

        // 상점 배치 (1/6/11층 Safe Room)
        if (shopSpawner != null && IsShopDepth(depth))
            shopSpawner.Spawn(rooms, depth, Rng);

        ComputeMapBounds(rooms);

        CurrentRooms = rooms;
        return rooms;
    }

    public bool IsBossDepth(int depth)
    {
        return bossDepthInterval > 0 && depth > 0 && depth % bossDepthInterval == 0;
    }

    public bool IsShopDepth(int depth)
    {
        return depth == 1 || depth == 6 || depth == 11;
    }

    private List<RoomData> InitializeRooms(int depth, DungeonRandom rng)
    {
        var rooms = new List<RoomData>
        {
            new RoomData { type = RoomType.Safe, sizeCategory = SizeCategory.Normal }
        };

        if (IsBossDepth(depth))
        {
            // 보스 층
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

            float specialChance = config != null ? config.specialRoomChance : 0f;
            float secretChance = config != null ? config.secretRoomChance : 0f;

            if (rng.Chance(specialChance))
            {
                var kind = (SpecialRoomKind)rng.Range(0, System.Enum.GetValues(typeof(SpecialRoomKind)).Length);
                rooms.Add(new RoomData { type = RoomType.Special, sizeCategory = SizeCategory.Normal, specialKind = kind });
            }
            if (rng.Chance(secretChance))
            {
                var kind = (SecretRoomKind)rng.Range(0, System.Enum.GetValues(typeof(SecretRoomKind)).Length);
                rooms.Add(new RoomData { type = RoomType.Secret, sizeCategory = SizeCategory.Normal, secretKind = kind });
            }
        }

        rooms.Add(new RoomData { type = RoomType.Exit, sizeCategory = SizeCategory.Normal });
        return rooms;
    }

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
        if (exitRoom == null || exitRoom.floorCells == null || exitRoom.floorCells.Count == 0)
            return null;

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

    // 출구 주변 hazard 없게 정리
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

    /// 방 입구마다 문 배치
    private void PlaceDoors(List<RoomData> rooms)
    {
        if (doorPrefab == null) 
            return;

        var safeRoom = rooms.Find(r => r.type == RoomType.Safe);
        HashSet<Vector2Int> safeFloor = (safeRoom != null && safeRoom.floorCells != null)
            ? new HashSet<Vector2Int>(safeRoom.floorCells)
            : null;

        var placed = new HashSet<Vector2Int>();
        foreach (var room in rooms)
        {
            if (room.type == RoomType.Secret || room.type == RoomType.Tunnel)
                continue;

            if (room.doorCells == null) 
                continue;

            foreach (var cell in room.doorCells)
            {
                if (placed.Contains(cell))
                    continue;

                if (!IsDoorwayCell(cell, out bool horizontalPassage))
                    continue;

                placed.Add(cell);
                GridDoor.DoorKind kind;
                if (SafeGatewayCells.Contains(cell))
                    kind = GridDoor.DoorKind.SafeExit;
                else if (LockedGatewayCells.Contains(cell))
                    kind = GridDoor.DoorKind.Locked;
                else
                    kind = GridDoor.DoorKind.Normal;

                var door = SpawnDoor(cell, horizontalPassage, kind);
                if (kind == GridDoor.DoorKind.SafeExit && safeFloor != null)
                {
                    Vector2Int outside = ComputeSafeOutsideCell(cell, horizontalPassage, safeFloor);
                    door.ConfigureSafeExit(outside);
                }
            }
        }

        // wall 로 봉인되어있는 시크릿 입구에 문 배치
        int secretSpawned = 0;
        foreach (var cell in SecretGatewayCells)
        {
            if (placed.Contains(cell))
                continue;

            if (!IsSecretDoorwayCell(cell, out bool horizontalPassage)) 
                continue;

            placed.Add(cell);
            SpawnDoor(cell, horizontalPassage, GridDoor.DoorKind.Secret);
            secretSpawned++;
        }
    }

    private GridDoor SpawnDoor(Vector2Int cell, bool horizontalPassage, GridDoor.DoorKind kind)
    {
        Vector3 pos = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(cell) : new Vector3(cell.x, cell.y, 0);
        var door = Instantiate(doorPrefab, pos, Quaternion.identity);
        door.Initialize(cell, horizontalPassage, kind);
        _currentDoors.Add(door);
        return door;
    }

    // Safe방 나왔는지 체크
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

    // Secret방 문 상하좌우 판단
    private bool IsSecretDoorwayCell(Vector2Int cell, out bool horizontalPassage)
    {
        horizontalPassage = false;
        bool floorL = IsOpenFloor(cell + Vector2Int.left);
        bool floorR = IsOpenFloor(cell + Vector2Int.right);
        bool floorU = IsOpenFloor(cell + Vector2Int.up);
        bool floorD = IsOpenFloor(cell + Vector2Int.down);
        bool horiz = !floorU && !floorD && (floorL || floorR);
        bool vert = !floorL && !floorR && (floorU || floorD);

        if (horiz == vert) 
            return false;

        horizontalPassage = horiz;
        return true;
    }

    // 상하 좌우 문
    private bool IsDoorwayCell(Vector2Int cell, out bool horizontalPassage)
    {
        horizontalPassage = false;
        if (!IsOpenFloor(cell)) 
            return false;

        bool wallL = IsWallCell(cell + Vector2Int.left);
        bool wallR = IsWallCell(cell + Vector2Int.right);
        bool wallU = IsWallCell(cell + Vector2Int.up);
        bool wallD = IsWallCell(cell + Vector2Int.down);

        // 좌우로 통하는 입구
        bool horiz = wallU && wallD && IsOpenFloor(cell + Vector2Int.left) && IsOpenFloor(cell + Vector2Int.right);
        // 상하로 통하는 입구
        bool vert = wallL && wallR && IsOpenFloor(cell + Vector2Int.up) && IsOpenFloor(cell + Vector2Int.down);

        if (horiz == vert)
            return false;

        horizontalPassage = horiz;
        return true;
    }

    private bool IsWallCell(Vector2Int c)
    {
        return tilemaps != null && tilemaps.wall != null && tilemaps.wall.HasTile(new Vector3Int(c.x, c.y, 0));
    }

    // 통행 가능한 바닥
    private bool IsOpenFloor(Vector2Int c)
    {
        var cell = new Vector3Int(c.x, c.y, 0);
        if (tilemaps == null || tilemaps.floor == null || !tilemaps.floor.HasTile(cell))
            return false;

        if (tilemaps.wall != null && tilemaps.wall.HasTile(cell)) 
            return false;

        return true;
    }

    private void DestroyCurrentDoors()
    {
        foreach (var door in _currentDoors)
        {
            if (door == null) 
                continue;

            if (Application.isPlaying)
                Destroy(door.gameObject);
            else 
                DestroyImmediate(door.gameObject);
        }
        _currentDoors.Clear();
    }

    // 열쇠 배치
    private void SpawnKeys(List<RoomData> rooms)
    {
        if (specialKeyPickupPrefab == null)
            return;

        if (rooms == null) 
            return;

        if (!rooms.Exists(r => r.type == RoomType.Special))
            return;

        // 입구 인접 셀 제외
        var avoid = new HashSet<Vector2Int>();
        foreach (var room in rooms)
        {
            if (room.doorCells == null) 
                continue;

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
            if (room.type != RoomType.Normal && room.type != RoomType.Exit) 
                continue;
            if (room.floorCells == null) 
                continue;
            foreach (var c in room.floorCells)
            {
                if (avoid.Contains(c)) 
                    continue;
                if (HazardCells.Contains(c)) 
                    continue;
                candidates.Add(c);
            }
        }
        if (candidates.Count == 0) 
            return;

        var cell = Rng.Pick(candidates);
        Vector3 pos = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(cell) : new Vector3(cell.x, cell.y, 0);
        var pickup = Instantiate(specialKeyPickupPrefab, pos, Quaternion.identity);
        pickup.Initialize(cell, KeyType.SpecialRoom);
        _currentKeys.Add(pickup);
    }

    private void DestroyCurrentKeys()
    {
        foreach (var key in _currentKeys)
        {
            if (key == null) 
                continue;

            if (Application.isPlaying) 
                Destroy(key.gameObject);

            else 
                DestroyImmediate(key.gameObject);
        }
        _currentKeys.Clear();
    }

    // 현재 층의 월드 크기 계산
    private void ComputeMapBounds(List<RoomData> rooms)
    {
        MapWorldBoundsValid = false;
        if (rooms == null || GridSystem.Instance == null) 
            return;

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        bool any = false;
        foreach (var r in rooms)
        {
            if (r.floorCells == null) 
                continue;

            foreach (var c in r.floorCells)
            {
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
                any = true;
            }
        }
        if (!any)
            return;

        const int padding = -2;
        minX -= padding; minY -= padding;
        maxX += padding; maxY += padding;

        var wMin = GridSystem.Instance.GridToWorld(new Vector2Int(minX, minY));
        var wMax = GridSystem.Instance.GridToWorld(new Vector2Int(maxX, maxY));
        MapWorldBounds = new Bounds( (wMin + wMax) * 0.5f, new Vector3(Mathf.Abs(wMax.x - wMin.x), Mathf.Abs(wMax.y - wMin.y), 0f));
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
