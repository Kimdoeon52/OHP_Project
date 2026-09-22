using System;
using System.Collections.Generic;
using UnityEngine;

// 맵에 아이템 배치용 스크립트
public class MapItemSpawner : MonoBehaviour
{
    [Serializable]
    public class ScheduledItemSpawn
    {
        public string label;
        public ItemBase item;
        public int[] floors;
        [Min(1)] public int count = 1;
    }

    [Header("프리팹")]
    [SerializeField] private ItemPickup itemPickupPrefab;
    [Header("풀")]
    [SerializeField] private ItemPool randomItemPool;
    [SerializeField] private CurseData[] cursePool;

    [Header("고정 등장 아이템")]
    [SerializeField] private List<ScheduledItemSpawn> scheduled = new();

    [Header("매 층 등장 아이템")]
    [SerializeField] private List<ItemBase> guaranteedPerFloor = new();

    [Header("매 층 택1 등장 아이템 (빵/비상 식량 균등)")]
    [SerializeField] private List<ItemBase> perFloorOneOf = new();
    [Tooltip("2개째 추가 등장 확률")]
    [SerializeField, Range(0f, 1f)] private float perFloorOneOfSecondChance = 0.5f;

    [Header("소형 비상 식량 — 생존주의자 특성 전용")]
    [Tooltip("소형 비상 식량 SO")]
    [SerializeField] private ItemBase smallEmergencyFood;

    [Header("랜덤 아이템 등장 확률")]
    [SerializeField, Range(0f, 1f)] private float randomItemChance = 0.2f;
    [SerializeField, Min(1)] private int attemptsPerRoom = 1;

    [Header("골드 등장 확률")]
    [SerializeField] private GoldPickup goldPickupPrefab;
    [SerializeField, Range(0f, 1f)] private float goldChance = 0.2f;
    [SerializeField, Min(0)] private int goldMin = 10;
    [SerializeField, Min(0)] private int goldMax = 30;
    [SerializeField, Min(1)] private int goldAttemptsPerRoom = 1;

    [Header("스페셜 방 — 스크롤 방")]
    [SerializeField] private int[] scrollRoomExcludeIds = { 1002 }; // 강화 스크롤 제외
    [SerializeField, Min(1)] private int scrollRoomMin = 2;
    [SerializeField, Min(1)] private int scrollRoomMax = 3;

    [Header("스페셜 방 — 포션 방")]
    [SerializeField] private int[] potionRoomExcludeIds = { 2003 }; // 힘 포션 제외
    [SerializeField, Min(1)] private int potionRoomMin = 2;
    [SerializeField, Min(1)] private int potionRoomMax = 2;

    [Header("Secret 방 — 마법 부여 방")]
    [SerializeField] private EnchantAltar enchantAltarPrefab;

    [Header("챌린지 — 시야 제한")]
    [Tooltip("횃불 SO")]
    [SerializeField] private ItemBase visionLimitTorch;
    [Tooltip("매 층 등장 수")]
    [SerializeField, Min(1)] private int visionLimitTorchCount = 1;

    [Header("챌린지 — 빈약한 강화")]
    [Tooltip("강화주문서 등장 수 감소량")]
    [SerializeField, Min(0)] private int scarceUpgradeCountReduction = 1;

    [Header("스페셜 방 — 골드 방")]
    [SerializeField, Min(1)] private int goldRoomPileMin = 3;
    [SerializeField, Min(1)] private int goldRoomPileMax = 5;
    [SerializeField, Min(0)] private int goldRoomAmountMin = 50;
    [SerializeField, Min(0)] private int goldRoomAmountMax = 75;

    private readonly List<GameObject> _spawned = new();

    // 생존주의자 특성으로 소형 비상 식량이 예약된 남은 층 수
    private int _emergencyFoodFloorsRemaining;

    private void OnEnable()
    {
        var trait = TraitController.FindAvailableInstance();
        if (trait != null)
            trait.OnTraitLevelChanged += HandleTraitLevelChanged;
    }

    private void OnDisable()
    {
        if (TraitController.Instance != null)
            TraitController.Instance.OnTraitLevelChanged -= HandleTraitLevelChanged;
    }

    // 생존주의자 특성 레벨업 시 다음 N개 층에 소형 비상 식량 예약
    private void HandleTraitLevelChanged(TraitId id, int level)
    {
        if (id != TraitId.Survivalist || TraitController.Instance == null)
            return;
        _emergencyFoodFloorsRemaining = TraitController.Instance.GetEmergencyFoodFloorCount();
    }

    public void Spawn(List<RoomData> rooms, int depth, int runSeed, DungeonRandom rng)
    {
        Clear();
        if (itemPickupPrefab == null || rooms == null || rng == null)
            return;

        SpawnScheduled(rooms, depth, runSeed, rng);
        SpawnGuaranteed(rooms, rng);
        SpawnVisionLimitTorch(rooms, rng);
        SpawnPerFloorOneOf(rooms, rng);
        SpawnTraitEmergencyFood(rooms, rng);
        SpawnRandom(rooms, rng);
        SpawnGold(rooms, rng);
        SpawnSubtypeRooms(rooms, depth, rng);
    }

    // 빵/비상 식량 — 매 층 후보 중 균등하게 1개 등장, 확률로 2개째 추가 (보스 층은 Spawn 미호출로 자동 제외)
    private void SpawnPerFloorOneOf(List<RoomData> rooms, DungeonRandom rng)
    {
        var candidates = new List<ItemBase>();
        foreach (var item in perFloorOneOf)
            if (item != null)
                candidates.Add(item);

        if (candidates.Count == 0)
            return;

        int count = rng.Chance(perFloorOneOfSecondChance) ? 2 : 1;
        for (int i = 0; i < count; i++)
            PlaceInNormalOrExit(rooms, candidates[rng.Range(0, candidates.Count)], rng);
    }

    // 소형 비상 식량 — 특성 예약 층 동안 1개씩 배치 (보스 층은 Spawn 미호출로 소모 안 됨)
    private void SpawnTraitEmergencyFood(List<RoomData> rooms, DungeonRandom rng)
    {
        if (smallEmergencyFood == null || _emergencyFoodFloorsRemaining <= 0)
            return;
        _emergencyFoodFloorsRemaining--;
        PlaceInNormalOrExit(rooms, smallEmergencyFood, rng);
    }


    private void SpawnSubtypeRooms(List<RoomData> rooms, int depth, DungeonRandom rng)
    {
        foreach (var room in rooms)
        {
            if (room.type == RoomType.Special && room.specialKind == SpecialRoomKind.Scroll)
                SpawnItemsInRoom(room, ItemCategory.Scroll, scrollRoomExcludeIds, scrollRoomMin, scrollRoomMax, rng);
            else if (room.type == RoomType.Secret && room.secretKind == SecretRoomKind.Potion)
                SpawnItemsInRoom(room, ItemCategory.Potion, potionRoomExcludeIds, potionRoomMin, potionRoomMax, rng);
            else if (room.type == RoomType.Secret && room.secretKind == SecretRoomKind.Gold)
                SpawnGoldPilesInRoom(room, rng);
            else if (room.type == RoomType.Secret && room.secretKind == SecretRoomKind.Enchant)
                SpawnEnchantAltar(room, depth, rng);
        }
    }

    // 마법 부여 제단 1개 배치
    private void SpawnEnchantAltar(RoomData room, int depth, DungeonRandom rng)
    {
        if (enchantAltarPrefab == null)
        {
            Debug.LogWarning("[MapItemSpawner] Enchant 방인데 enchantAltarPrefab 미연결 — 방 빔");
            return;
        }

        var cells = SpawnCells.Candidates(room);
        if (cells.Count == 0)
        {
            Debug.LogWarning("[MapItemSpawner] Enchant 방 배치 가능 셀 없음 — 방 빔");
            return;
        }

        // 방 중앙에 가장 가까운 후보 셀 선택 (쏠림 방지)
        Vector2Int cell = NearestToCenter(room, cells);
        EnchantAltar altar = Instantiate(enchantAltarPrefab);
        altar.Initialize(cell, depth);
        _spawned.Add(altar.gameObject);
    }

    // 방 floorCells 중심에 가장 가까운 후보 셀
    private static Vector2Int NearestToCenter(RoomData room, List<Vector2Int> cells)
    {
        Vector2 center = Vector2.zero;
        var src = (room.floorCells != null && room.floorCells.Count > 0) ? room.floorCells : cells;
        foreach (var c in src)
            center += c;
        center /= src.Count;

        Vector2Int best = cells[0];
        float bestDist = float.MaxValue;
        foreach (var c in cells)
        {
            float d = ((Vector2)c - center).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }
        return best;
    }

    // 아이템을 min ~ max개 배치
    private void SpawnItemsInRoom(RoomData room, ItemCategory category, int[] excludeIds, int min, int max, DungeonRandom rng)
    {
        var items = ItemsOfCategory(category, excludeIds);
        if (items.Count == 0) 
            return;

        var cells = SpawnCells.Candidates(room);
        int count = Mathf.Min(rng.Range(min, max + 1), cells.Count);
        for (int i = 0; i < count; i++)
            SpawnPickup(items[rng.Range(0, items.Count)], SpawnCells.PopRandom(cells, rng), rng);
    }

    // ID 매핑
    private List<ItemBase> ItemsOfCategory(ItemCategory category, int[] excludeIds)
    {
        var result = new List<ItemBase>();
        if (randomItemPool == null || randomItemPool.items == null) 
            return result;
        foreach (var so in randomItemPool.items)
        {
            if (so == null) 
                continue;
            if (ItemCategories.Of(so) != category) 
                continue;
            if (excludeIds != null && Array.IndexOf(excludeIds, so.Id) >= 0) 
                continue;
            result.Add(so);
        }
        return result;
    }

    // 골드 방 — 바닥 골드 뭉치 여러 개
    private void SpawnGoldPilesInRoom(RoomData room, DungeonRandom rng)
    {
        if (goldPickupPrefab == null) 
            return;
        var cells = SpawnCells.Candidates(room);
        int piles = Mathf.Min(rng.Range(goldRoomPileMin, goldRoomPileMax + 1), cells.Count);
        for (int i = 0; i < piles; i++)
        {
            int amount = rng.Range(goldRoomAmountMin, goldRoomAmountMax + 1);
            Vector2Int cell = SpawnCells.PopRandom(cells, rng);
            Vector3 pos = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(cell): new Vector3(cell.x, cell.y, 0);
            GoldPickup gold = Instantiate(goldPickupPrefab, pos, Quaternion.identity);
            gold.Initialize(cell, amount);
            _spawned.Add(gold.gameObject);
        }
    }

    // 골드 스폰
    private void SpawnGold(List<RoomData> rooms, DungeonRandom rng)
    {
        if (goldPickupPrefab == null || goldMax <= 0) 
            return;

        foreach (var room in rooms)
        {
            if (room.type != RoomType.Normal && room.type != RoomType.Exit) 
                continue;

            var cells = SpawnCells.Candidates(room);
            for (int a = 0; a < goldAttemptsPerRoom && cells.Count > 0; a++)
            {
                if (!rng.Chance(goldChance))
                    continue;
                int amount = rng.Range(goldMin, goldMax + 1);

                Vector2Int cell = SpawnCells.PopRandom(cells, rng);
                Vector3 pos = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(cell) : new Vector3(cell.x, cell.y, 0);
                GoldPickup gold = Instantiate(goldPickupPrefab, pos, Quaternion.identity);
                gold.Initialize(cell, amount);
                _spawned.Add(gold.gameObject);
            }
        }
    }

    // 시야 제한 챌린지 활성 시 매 층 횃불 고정 등장
    private void SpawnVisionLimitTorch(List<RoomData> rooms, DungeonRandom rng)
    {
        if (visionLimitTorch == null || !ChallengeState.Has(ChallengeType.VisionLimit))
            return;

        for (int i = 0; i < visionLimitTorchCount; i++)
            PlaceInNormalOrExit(rooms, visionLimitTorch, rng);
    }

    // 매 층 확정
    private void SpawnGuaranteed(List<RoomData> rooms, DungeonRandom rng)
    {
        foreach (var item in guaranteedPerFloor)
            if (item != null) 
                PlaceInNormalOrExit(rooms, item, rng);
    }

    public void Clear()
    {
        foreach (var go in _spawned)
        {
            if (go == null) 
                continue;
            if (Application.isPlaying) 
                Destroy(go);
            else 
                DestroyImmediate(go);
        }
        _spawned.Clear();
    }

    // 고정 배치
    private void SpawnScheduled(List<RoomData> rooms, int depth, int runSeed, DungeonRandom rng)
    {
        for (int i = 0; i < scheduled.Count; i++)
        {
            var entry = scheduled[i];
            if (entry == null || entry.item == null || entry.floors == null || entry.floors.Length == 0)
                continue;
            if (Array.IndexOf(entry.floors, depth) < 0)
                continue;

            int count = entry.count;
            // 빈약한 강화 챌린지 — 강화주문서 등장 수 감소
            if (entry.item is ReinforceScroll && ChallengeState.Has(ChallengeType.ScarceUpgrade))
                count = Mathf.Max(1, entry.count - scarceUpgradeCountReduction);

            if (DepthIsChosen(entry, count, depth, runSeed, i))
                PlaceInNormalOrExit(rooms, entry.item, rng);
        }
    }

    private static bool DepthIsChosen(ScheduledItemSpawn entry, int count, int depth, int runSeed, int index)
    {
        var floors = new List<int>(entry.floors);
        int take = Mathf.Min(count, floors.Count);

        var r = new DungeonRandom();
        r.Init(runSeed ^ (entry.item.Id * 397) ^ (index * 131) ^ (floors.Count * 17)); // 대충 설정한 숫자

        for (int k = 0; k < take; k++)
        {
            int j = r.Range(k, floors.Count);
            (floors[k], floors[j]) = (floors[j], floors[k]);
            if (floors[k] == depth) 
                return true;
        }
        return false;
    }

    private void PlaceInNormalOrExit(List<RoomData> rooms, ItemBase item, DungeonRandom rng)
    {
        var cells = SpawnCells.CandidatesIn(rooms, RoomType.Normal, RoomType.Exit);
        if (cells.Count == 0)
            return;

        SpawnPickup(item, SpawnCells.PopRandom(cells, rng), rng);
    }

    // 랜덤 아이템 배치
    private void SpawnRandom(List<RoomData> rooms, DungeonRandom rng)
    {
        var candidates = BuildRandomCandidates();
        if (candidates.Count == 0) 
            return;

        foreach (var room in rooms)
        {
            if (room.type != RoomType.Normal && room.type != RoomType.Exit)
                continue;

            var cells = SpawnCells.Candidates(room);
            for (int a = 0; a < attemptsPerRoom && cells.Count > 0; a++)
            {
                if (!rng.Chance(randomItemChance))
                    continue;

                ItemBase item = candidates[rng.Range(0, candidates.Count)];
                SpawnPickup(item, SpawnCells.PopRandom(cells, rng), rng);
            }
        }
    }

    // 고정 배치용 아이템 제외한 나머지 아이템 종류
    private List<ItemBase> BuildRandomCandidates()
    {
        var result = new List<ItemBase>();
        if (randomItemPool == null || randomItemPool.items == null)
            return result;

        var excluded = new HashSet<ItemBase>();
        foreach (var s in scheduled)
            if (s != null && s.item != null) 
                excluded.Add(s.item);

        foreach (var g in guaranteedPerFloor)
            if (g != null)
                excluded.Add(g);

        foreach (var p in perFloorOneOf)
            if (p != null)
                excluded.Add(p);

        if (smallEmergencyFood != null)
            excluded.Add(smallEmergencyFood);

        if (visionLimitTorch != null && ChallengeState.Has(ChallengeType.VisionLimit))
            excluded.Add(visionLimitTorch);

        foreach (var so in randomItemPool.items)
        {
            if (so == null || excluded.Contains(so))
                continue;

            result.Add(so);
        }
        return result;
    }

    private void SpawnPickup(ItemBase item, Vector2Int cell, DungeonRandom rng)
    {
        if (item == null)
            return;

        Vector3 pos = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(cell) : new Vector3(cell.x, cell.y, 0);

        ItemPickup pickup = Instantiate(itemPickupPrefab, pos, Quaternion.identity);
        pickup.itemSO = item;
        pickup.enhanceLevel = item.CanEnhance ? ItemRoll.Enhance(rng) : 0;
        pickup.curse = item.CanCurse && ItemRoll.IsCurseEligible(item) ? ItemRoll.Curse(rng, cursePool, item.Slot) : null;
        pickup.Initialize(cell);
        _spawned.Add(pickup.gameObject);

        // TEMP DEBUG — 저주 굴림 확인용. 확인 끝나면 지워주세요.
        if (item.CanCurse)
            Debug.Log($"[MapItemSpawner] {item.Name} @ {cell} canCurse=true curse={(pickup.curse != null ? pickup.curse.CurseName : "없음")} (seed={rng.Seed}, call={rng.CallCount})");
    }
}
