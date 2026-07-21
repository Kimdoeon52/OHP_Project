using System.Collections.Generic;
using PrototypeRT;
using UnityEngine;

// 스냅샷을 현재 씬에 적용. 시드/층은 맵 생성 전, 나머지는 장비 초기화 후 호출.
public static class RunRestorer
{
    // 맵 생성보다 먼저 호출 — 저장된 시드/층/챌린지로 같은 맵을 만들게 함.
    public static void ApplySeedDepth(RunSnapshot snap)
    {
        if (snap == null)
            return;

        RunState.Instance?.ResetRun(snap.seed, snap.depth);

        // 챌린지(보스강화/시야 등)는 맵 생성·효과에 영향 → 빌드 전에 복원·고정.
        ChallengeState cs = ChallengeState.Ensure();
        if (cs != null)
        {
            var types = new List<ChallengeType>();
            if (snap.challenges != null)
            {
                foreach (int v in snap.challenges)
                    types.Add((ChallengeType)v);
            }

            cs.Configure(types);
            cs.Lock();
        }
    }

    // 장비 초기화(NYHEquipmentController.Start) 이후 호출 — 시작 장비를 저장본으로 교체.
    public static void ApplyState(RunSnapshot snap)
    {
        if (snap == null)
            return;

        ItemDatabase db = ItemDatabase.Instance;
        if (db == null)
            return;

        PlayerSnapshot p = snap.player;

        // 이어하기는 저장된 그 런의 골드 그대로 복원 (이월 골드 아님).
        Wallet.Instance?.SetBalance(snap.gold);

        // 특성은 부수효과(포인트 차감/즉시 스탯 적용) 없이 값만 복원 — 아래 ApplyStats가 최종 스탯을 절대값으로 덮어씀.
        RestoreTraits(snap);

        // 레벨 먼저 — 장착 요구레벨 검증에 쓰임.
        PlayerLevelSystem.Instance?.SetLevelExp(p.level, p.exp);

        // 1차 스탯 복원 — 장착 요구힘 검증 통과용.
        ApplyStats(p);

        RestoreInventory(snap.inventory, db);
        RestoreEquipped(snap.equipped, db);

        // 2차 스탯 복원 — 장비/반지 재장착으로 더해진 보너스를 절대값으로 덮어 정확한 총합 복원.
        ApplyStats(p);

        // 체력은 MaxHealth 복원 이후라야 클램프가 맞음.
        GridPlayerHealth health = GridPlayerHealth.Instance;
        if (health != null)
        {
            health.SetCurrentHp(p.currentHp);
            health.SetPotionCount(p.potionCount);
        }
    }

    private static void ApplyStats(PlayerSnapshot p)
    {
        NYHPlayerStats.Instance?.SetStats(p.maxHealth, p.strength, p.moveSpeed, p.attackSpeed, p.accuracy, p.defense, p.evasion, p.hunger);
    }

    private static void RestoreTraits(RunSnapshot snap)
    {
        TraitController trait = TraitController.FindAvailableInstance();
        if (trait == null)
            return;

        var levels = new Dictionary<TraitId, int>();
        if (snap.traitLevels != null)
        {
            foreach (TraitDTO dto in snap.traitLevels)
                levels[(TraitId)dto.traitId] = dto.level;
        }

        var points = new Dictionary<TraitTier, int>();
        if (snap.traitPoints != null)
        {
            foreach (TraitPointDTO dto in snap.traitPoints)
                points[(TraitTier)dto.tier] = dto.point;
        }

        trait.RestoreState(levels, points);
    }

    private static void RestoreInventory(List<ItemDTO> dtos, ItemDatabase db)
    {
        InventoryController inv = Object.FindFirstObjectByType<InventoryController>();
        if (inv == null)
            return;

        ClearInventory(inv);

        if (dtos == null)
            return;

        foreach (ItemDTO dto in dtos)
        {
            ItemInstance inst = SaveMapper.FromDTO(dto, db);
            if (inst == null)
                continue;

            inv.TryAddItem(inst, dto.count);
        }
    }

    // 시작 씬에 남아있을 수 있는 아이템을 비움.
    private static void ClearInventory(InventoryController inv)
    {
        var existing = new List<InventoryItem>(inv.Items);
        foreach (InventoryItem item in existing)
        {
            if (item == null)
                continue;

            inv.TryRemoveItem(item);
            Object.Destroy(item.gameObject);
        }
    }

    private static void RestoreEquipped(List<EquipDTO> dtos, ItemDatabase db)
    {
        if (dtos == null)
            return;

        InventoryController inv = Object.FindFirstObjectByType<InventoryController>();
        NYHEquipmentController equip = Object.FindFirstObjectByType<NYHEquipmentController>();
        if (inv == null || equip == null)
            return;

        foreach (EquipDTO dto in dtos)
        {
            ItemInstance inst = SaveMapper.FromDTO(dto.item, db);
            if (inst == null)
                continue;

            // 장비 슬롯 전용 뷰 생성 — 인벤 그리드엔 올리지 않음.
            InventoryItem view = inv.CreateItemView(inst, inv.transform, true);
            if (view == null)
                continue;

            EquipmentSlot slot = (EquipmentSlot)dto.slot;
            if (!equip.TryEquipFromInventory(view, slot, dto.slotIndex, out InventoryItem replaced))
            {
                Object.Destroy(view.gameObject);
                continue;
            }

            view.gameObject.SetActive(false);
            view.SetRaycastTarget(false);

            // 교체된 시작 장비 뷰 정리.
            if (replaced != null)
                Object.Destroy(replaced.gameObject);
        }
    }
}
