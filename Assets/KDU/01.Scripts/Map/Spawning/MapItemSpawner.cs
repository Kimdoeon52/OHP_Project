using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵에 바닥 아이템(ItemPickup)을 배치하는 스포너. MapGenerator 가 상자 배치 뒤에 호출한다.
///
/// 두 흐름:
///   1) 고정 스케줄 — 힘 포션/강화 스크롤처럼 "여러 층에 걸쳐 정확한 개수"로 등장하는 아이템.
///      각 항목은 등장 가능 층(floors) 중 count개 층을 runSeed 로 결정론 선택 → 그 층에만 1개씩.
///      (예: 힘 포션 floors=[1,2] count=1 → 1·2층 중 시드로 고른 한 층에 1개. Area마다 동일 패턴.)
///   2) 랜덤 아이템 — Normal/Exit 방마다 randomItemChance 로 "아이템 등장" 굴림(방당 attemptsPerRoom회).
///      등장하면 개수 미정 풀(randomItemPool, 스케줄 항목 제외)에서 랜덤 1개.
///
/// 랜덤 상자(20%)는 이 흐름이 아니라 ChestSpawner 의 별도 흐름이 담당한다(GDD 9.14).
/// 보스 층은 MapGenerator 가 이 스포너를 호출하지 않으므로 자동 제외.
///
/// 특수 방 하위 종류 중 '바닥 아이템/골드 뭉치' 종류도 담당 — 스크롤 방(Special),
/// 포션 방·골드 방(Secret). 종류는 MapGenerator 가 결정(room.specialKind/secretKind)하고,
/// 상자류는 ChestSpawner, 몬스터 방은 EnemySpawner 가 같은 값을 읽어 분담한다.
/// </summary>
public class MapItemSpawner : MonoBehaviour
{
    [Serializable]
    public class ScheduledItemSpawn
    {
        [Tooltip("식별/디버그용 (힘 포션 1Area 전반부 등).")]
        public string label;
        public ItemBase item;
        [Tooltip("등장 가능 층(브래킷). 이 중 count개 층이 시드로 뽑혀 1개씩 등장.")]
        public int[] floors;
        [Min(1)] public int count = 1;
    }

    [Header("프리팹 / 풀")]
    [SerializeField] private ItemPickup itemPickupPrefab;
    [Tooltip("개수 미정(랜덤 등장) 아이템 풀. 스케줄에 등록된 아이템은 자동 제외.")]
    [SerializeField] private ItemPool randomItemPool;
    [Tooltip("장비에 부여할 저주 풀 (비우면 저주 없음).")]
    [SerializeField] private CurseData[] cursePool;

    [Header("고정 스케줄 (힘 포션 / 강화 스크롤 등)")]
    [SerializeField] private List<ScheduledItemSpawn> scheduled = new();

    [Header("매 층 1개 확정 (지도 스크롤 등 — GDD 9.1)")]
    [Tooltip("이 스포너가 도는 모든 (비보스) 층에 항목당 1개씩 Normal/Exit 방에 확정 배치.")]
    [SerializeField] private List<ItemBase> guaranteedPerFloor = new();

    [Header("랜덤 아이템 (방당 확률)")]
    [SerializeField, Range(0f, 1f)] private float randomItemChance = 0.2f;
    [SerializeField, Min(1)] private int attemptsPerRoom = 1;

    [Header("골드 (Normal/Exit 방당 확률)")]
    [SerializeField] private GoldPickup goldPickupPrefab;
    [SerializeField, Range(0f, 1f)] private float goldChance = 0.2f;
    [SerializeField, Min(0)] private int goldMin = 10;
    [SerializeField, Min(0)] private int goldMax = 30;
    [SerializeField, Min(1)] private int goldAttemptsPerRoom = 1;

    [Header("특수 방 — 스크롤 방 (randomItemPool 에서 카테고리=Scroll 필터, GDD 6.2.1)")]
    [Tooltip("스크롤 방에서 제외할 ID. GDD: 강화 스크롤(1002) 제외.")]
    [SerializeField] private int[] scrollRoomExcludeIds = { 1002 };
    [SerializeField, Min(1)] private int scrollRoomMin = 2;
    [SerializeField, Min(1)] private int scrollRoomMax = 3;

    [Header("특수 방 — 포션 방 (randomItemPool 에서 카테고리=Potion 필터, GDD 6.2.2)")]
    [Tooltip("포션 방에서 제외할 ID. GDD: 힘 포션(2003) 제외.")]
    [SerializeField] private int[] potionRoomExcludeIds = { 2003 };
    [SerializeField, Min(1)] private int potionRoomMin = 2;
    [SerializeField, Min(1)] private int potionRoomMax = 2;

    [Header("특수 방 — 골드 방")]
    [SerializeField, Min(1)] private int goldRoomPileMin = 3;
    [SerializeField, Min(1)] private int goldRoomPileMax = 5;
    [SerializeField, Min(0)] private int goldRoomAmountMin = 50;
    [SerializeField, Min(0)] private int goldRoomAmountMax = 75;

    private readonly List<GameObject> _spawned = new();

    public void Spawn(List<RoomData> rooms, int depth, int runSeed, DungeonRandom rng)
    {
        Clear();
        if (itemPickupPrefab == null || rooms == null || rng == null) return;

        SpawnScheduled(rooms, depth, runSeed, rng);
        SpawnGuaranteed(rooms, rng);
        SpawnRandom(rooms, rng);
        SpawnGold(rooms, rng);
        SpawnSubtypeRooms(rooms, rng);
    }

    // ───── 특수 방 하위 종류 (스크롤/포션/골드 방 — GDD 6.2.1/6.2.2) ─────
    // 종류는 MapGenerator 가 결정(room.specialKind/secretKind). 상자류는 ChestSpawner,
    // 몬스터 방은 EnemySpawner, 인챈트 방은 별도 시스템이 같은 값을 읽어 담당.

    private void SpawnSubtypeRooms(List<RoomData> rooms, DungeonRandom rng)
    {
        foreach (var room in rooms)
        {
            if (room.type == RoomType.Special && room.specialKind == SpecialRoomKind.Scroll)
                SpawnItemsInRoom(room, ItemCategory.Scroll, scrollRoomExcludeIds, scrollRoomMin, scrollRoomMax, rng);
            else if (room.type == RoomType.Secret && room.secretKind == SecretRoomKind.Potion)
                SpawnItemsInRoom(room, ItemCategory.Potion, potionRoomExcludeIds, potionRoomMin, potionRoomMax, rng);
            else if (room.type == RoomType.Secret && room.secretKind == SecretRoomKind.Gold)
                SpawnGoldPilesInRoom(room, rng);
        }
    }

    /// <summary>방 floor 셀에 (전체 풀에서 category 필터·제외ID 적용해 뽑은) 아이템을 min~max개 배치.</summary>
    private void SpawnItemsInRoom(RoomData room, ItemCategory category, int[] excludeIds, int min, int max, DungeonRandom rng)
    {
        var items = ItemsOfCategory(category, excludeIds);
        if (items.Count == 0) return;

        var cells = SpawnCells.Candidates(room);
        int count = Mathf.Min(rng.Range(min, max + 1), cells.Count);
        for (int i = 0; i < count; i++)
            SpawnPickup(items[rng.Range(0, items.Count)], SpawnCells.PopRandom(cells, rng), rng);
    }

    /// <summary>randomItemPool(전체)에서 ID 구간 카테고리(ItemCategories)로 필터, excludeIds 제외.</summary>
    private List<ItemBase> ItemsOfCategory(ItemCategory category, int[] excludeIds)
    {
        var result = new List<ItemBase>();
        if (randomItemPool == null || randomItemPool.items == null) return result;
        foreach (var so in randomItemPool.items)
        {
            if (so == null) continue;
            if (ItemCategories.Of(so) != category) continue;
            if (excludeIds != null && Array.IndexOf(excludeIds, so.Id) >= 0) continue;
            result.Add(so);
        }
        return result;
    }

    /// <summary>골드 방 — 바닥 골드 뭉치 여러 개(개당 goldRoomAmount 범위).</summary>
    private void SpawnGoldPilesInRoom(RoomData room, DungeonRandom rng)
    {
        if (goldPickupPrefab == null) return;
        var cells = SpawnCells.Candidates(room);
        int piles = Mathf.Min(rng.Range(goldRoomPileMin, goldRoomPileMax + 1), cells.Count);
        for (int i = 0; i < piles; i++)
        {
            int amount = rng.Range(goldRoomAmountMin, goldRoomAmountMax + 1);
            Vector2Int cell = SpawnCells.PopRandom(cells, rng);
            Vector3 pos = GridSystem.Instance != null
                ? GridSystem.Instance.GridToWorld(cell)
                : new Vector3(cell.x, cell.y, 0);
            GoldPickup gold = Instantiate(goldPickupPrefab, pos, Quaternion.identity);
            gold.Initialize(cell, amount);
            _spawned.Add(gold.gameObject);
        }
    }

    // ───── 골드 ─────

    private void SpawnGold(List<RoomData> rooms, DungeonRandom rng)
    {
        if (goldPickupPrefab == null || goldMax <= 0) return;

        foreach (var room in rooms)
        {
            if (room.type != RoomType.Normal && room.type != RoomType.Exit) continue;

            var cells = SpawnCells.Candidates(room);
            for (int a = 0; a < goldAttemptsPerRoom && cells.Count > 0; a++)
            {
                if (!rng.Chance(goldChance)) continue;
                int amount = rng.Range(goldMin, goldMax + 1);
                Vector2Int cell = SpawnCells.PopRandom(cells, rng);
                Vector3 pos = GridSystem.Instance != null
                    ? GridSystem.Instance.GridToWorld(cell)
                    : new Vector3(cell.x, cell.y, 0);
                GoldPickup gold = Instantiate(goldPickupPrefab, pos, Quaternion.identity);
                gold.Initialize(cell, amount);
                _spawned.Add(gold.gameObject);
            }
        }
    }

    // ───── 매 층 확정 ─────

    private void SpawnGuaranteed(List<RoomData> rooms, DungeonRandom rng)
    {
        foreach (var item in guaranteedPerFloor)
            if (item != null) PlaceInNormalOrExit(rooms, item, rng);
    }

    public void Clear()
    {
        foreach (var go in _spawned)
        {
            if (go == null) continue;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        _spawned.Clear();
    }

    // ───── 고정 스케줄 ─────

    private void SpawnScheduled(List<RoomData> rooms, int depth, int runSeed, DungeonRandom rng)
    {
        for (int i = 0; i < scheduled.Count; i++)
        {
            var entry = scheduled[i];
            if (entry == null || entry.item == null || entry.floors == null || entry.floors.Length == 0)
                continue;
            if (Array.IndexOf(entry.floors, depth) < 0) continue; // 이 층은 대상 아님

            // 등장 가능 층 중 count개를 runSeed 로 고정 선택 — 현재 층이 뽑혔으면 1개 배치.
            if (DepthIsChosen(entry, depth, runSeed, i))
                PlaceInNormalOrExit(rooms, entry.item, rng);
        }
    }

    /// <summary>floors 중 count개 층을 runSeed 기반으로 결정론 선택하고, depth 가 포함되면 true.</summary>
    private static bool DepthIsChosen(ScheduledItemSpawn entry, int depth, int runSeed, int index)
    {
        var floors = new List<int>(entry.floors);
        int take = Mathf.Min(entry.count, floors.Count);

        var r = new DungeonRandom();
        // runSeed + 항목 식별로 파생 — 같은 런이면 항상 같은 층이 뽑힘.
        r.Init(runSeed ^ (entry.item.Id * 397) ^ (index * 131) ^ (floors.Count * 17));

        // 부분 Fisher-Yates: 앞 take개가 선택된 층.
        for (int k = 0; k < take; k++)
        {
            int j = r.Range(k, floors.Count);
            (floors[k], floors[j]) = (floors[j], floors[k]);
            if (floors[k] == depth) return true;
        }
        return false;
    }

    private void PlaceInNormalOrExit(List<RoomData> rooms, ItemBase item, DungeonRandom rng)
    {
        var cells = SpawnCells.CandidatesIn(rooms, RoomType.Normal, RoomType.Exit);
        if (cells.Count == 0) return;
        SpawnPickup(item, SpawnCells.PopRandom(cells, rng), rng);
    }

    // ───── 랜덤 아이템 ─────

    private void SpawnRandom(List<RoomData> rooms, DungeonRandom rng)
    {
        var candidates = BuildRandomCandidates();
        if (candidates.Count == 0) return;

        foreach (var room in rooms)
        {
            if (room.type != RoomType.Normal && room.type != RoomType.Exit) continue;

            var cells = SpawnCells.Candidates(room);
            for (int a = 0; a < attemptsPerRoom && cells.Count > 0; a++)
            {
                if (!rng.Chance(randomItemChance)) continue;
                ItemBase item = candidates[rng.Range(0, candidates.Count)];
                SpawnPickup(item, SpawnCells.PopRandom(cells, rng), rng);
            }
        }
    }

    /// <summary>개수 미정 풀 = randomItemPool 에서 스케줄 등록 아이템을 뺀 것.</summary>
    private List<ItemBase> BuildRandomCandidates()
    {
        var result = new List<ItemBase>();
        if (randomItemPool == null || randomItemPool.items == null) return result;

        var excluded = new HashSet<ItemBase>();
        foreach (var s in scheduled)
            if (s != null && s.item != null) excluded.Add(s.item);
        foreach (var g in guaranteedPerFloor)
            if (g != null) excluded.Add(g);

        foreach (var so in randomItemPool.items)
        {
            if (so == null || excluded.Contains(so)) continue;
            result.Add(so);
        }
        return result;
    }

    // ───── 공통 ─────

    private void SpawnPickup(ItemBase item, Vector2Int cell, DungeonRandom rng)
    {
        if (item == null) return;

        Vector3 pos = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);

        ItemPickup pickup = Instantiate(itemPickupPrefab, pos, Quaternion.identity);
        pickup.itemSO = item;
        pickup.enhanceLevel = item.CanEnhance ? ItemRoll.Enhance(rng) : 0;
        pickup.curse = item.CanCurse ? ItemRoll.Curse(rng, cursePool) : null;
        pickup.Initialize(cell);
        _spawned.Add(pickup.gameObject);
    }
}
