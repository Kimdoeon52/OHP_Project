using System.Collections.Generic;
using PrototypeRT;
using UnityEngine;

// 적 처치 시 골드·아이템·생명 파편 드롭 및 장비 저주 롤링 전담 헬퍼
public static class EnemyLootDropper
{
    public static void DropLoot(EnemyData data, Vector2Int gridPos, GoldPickup goldPrefab, ItemPickup itemPrefab, ItemBase fragmentsOfLifeItem, DungeonRandom rng, string ownerName)
    {
        if (data == null || rng == null)
            return;

        if (data.goldMax > 0 && rng.Value() <= data.goldDropChance)
        {
            int gold = rng.Range(data.goldMin, data.goldMax + 1);
            if (gold > 0)
                SpawnGoldDrop(gridPos, gold, goldPrefab, ownerName);
        }

        if (data.itemDrops != null)
        {
            foreach (var drop in data.itemDrops)
            {
                if (drop == null || drop.item == null)
                    continue;

                if (rng.Value() > drop.chance)
                    continue;

                for (int i = 0; i < drop.count; i++)
                    SpawnItemDrop(drop.item, gridPos, itemPrefab, rng, ownerName);
            }
        }

        if (fragmentsOfLifeItem != null && !ChallengeState.Has(ChallengeType.FragmentSeal))
        {
            int fragmentCount = data.grade == EnemyGrade.Elite ? 2 : 1;
            for (int i = 0; i < fragmentCount; i++)
                SpawnItemDrop(fragmentsOfLifeItem, gridPos, itemPrefab, rng, ownerName);
        }
    }

    private static void SpawnGoldDrop(Vector2Int gridPos, int amount, GoldPickup goldPrefab, string ownerName)
    {
        if (goldPrefab == null)
        {
            Debug.LogWarning($"{ownerName}: goldPickupPrefab 미할당 — 골드 {amount} 드롭 실패.");
            return;
        }

        GoldPickup pickup = Object.Instantiate(goldPrefab, WorldOf(gridPos), Quaternion.identity);
        pickup.Initialize(gridPos, amount);
    }

    private static void SpawnItemDrop(ItemBase item, Vector2Int gridPos, ItemPickup itemPrefab, DungeonRandom rng, string ownerName)
    {
        var groundItemController = Object.FindFirstObjectByType<GroundItemController>();
        ItemInstance itemInst = item.CreateInstance();

        if (itemInst is EquipmentInstance equipInst)
        {
            equipInst.enhanceLevel = ItemRoll.Enhance(rng);
            equipInst.curse = RollCurseForEquipment(equipInst, rng);
            equipInst.isAnalyzed = false;
        }

        if (groundItemController != null)
        {
            groundItemController.DropItem(itemInst, 1, gridPos);
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogWarning($"{ownerName}: {item.name} 드롭 실패. Item_Pickup을 연결해주세요.");
            return;
        }

        // itemSO를 null로 두어 ItemPickup.Start가 롤링된 인스턴스를 덮어쓰지 않게 유지
        ItemPickup pickup = Object.Instantiate(itemPrefab, WorldOf(gridPos), Quaternion.identity);
        pickup.item = itemInst;
        pickup.itemSO = null;
        pickup.Initialize(gridPos);
    }

    public static CurseData RollCurseForEquipment(EquipmentInstance equipInst, DungeonRandom rng)
    {
        if (equipInst == null || rng == null)
            return null;

        if (!ItemRoll.IsCurseEligible(equipInst.data))
            return null;

        if (rng.Value() > 0.5f)
            return null;

        var database = ItemDatabase.Instance;
        if (database == null)
            return null;

        EquipmentSlot slot = equipInst.data.Slot;
        List<CurseData> matchedCurses = new List<CurseData>();
        foreach (var curse in database.Curses)
        {
            if (curse != null && curse.TargetSlot == slot)
                matchedCurses.Add(curse);
        }

        if (matchedCurses.Count == 0)
            return null;

        return rng.Pick(matchedCurses);
    }

    private static Vector3 WorldOf(Vector2Int cell)
    {
        if (GridSystem.Instance != null)
            return GridSystem.Instance.GridToWorld(cell);

        return new Vector3(cell.x, cell.y, 0f);
    }
}
