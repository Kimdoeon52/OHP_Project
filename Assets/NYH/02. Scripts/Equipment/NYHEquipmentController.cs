using System;
using System.Collections.Generic;
using PrototypeRT;
using UnityEngine;

public class NYHEquipmentController : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("스탯을 적용할 플레이어 스탯입니다. 비워두면 씬의 NYHPlayerStats.Instance를 사용합니다.")]
    [SerializeField] private NYHPlayerStats playerStats;

    [Tooltip("레벨 조건을 확인할 플레이어 레벨 시스템입니다. 비워두면 씬의 PlayerLevelSystem.Instance를 사용합니다.")]
    [SerializeField] private PlayerLevelSystem playerLevelSystem;

    [Tooltip("YJW ItemBase에 NYH 장비 스탯을 연결하는 프로필 목록입니다.")]
    [SerializeField] private List<NYHEquipmentProfile> equipmentProfiles = new();

    private readonly Dictionary<NYHEquipmentSlotKey, NYHEquipmentItemInstance> _equippedItems = new();
    private readonly Dictionary<NYHEquipmentSlotKey, NYHEquipmentProfile> _appliedProfiles = new();
    private readonly Dictionary<NYHEquipmentSlotKey, InventoryItem> _equippedViews = new();

    public event Action<EquipmentSlot, NYHEquipmentItemInstance> OnEquipped;
    public event Action<EquipmentSlot, NYHEquipmentItemInstance> OnUnequipped;
    public event Action<EquipmentSlot, int, InventoryItem> OnInventoryItemEquipped;
    public event Action<EquipmentSlot, int, InventoryItem> OnInventoryItemUnequipped;

    private NYHPlayerStats Stats
    {
        get
        {
            if (playerStats == null)
                playerStats = NYHPlayerStats.Instance;

            return playerStats;
        }
    }

    private PlayerLevelSystem LevelSystem
    {
        get
        {
            if (playerLevelSystem == null)
                playerLevelSystem = PlayerLevelSystem.Instance;

            return playerLevelSystem;
        }
    }

    public bool TryEquip(ItemBase itemData)
    {
        if (itemData == null)
            return false;

        return TryEquip(new NYHEquipmentItemInstance(itemData));
    }

    public bool TryEquip(NYHEquipmentItemInstance item)
    {
        if (item == null || item.Data == null)
            return false;

        NYHEquipmentSlotKey key = GetDefaultKey(item.Data);
        return TryEquipInternal(item, null, key, out _);
    }

    public bool CanEquip(ItemBase itemData)
    {
        if (itemData == null) return false;

        NYHEquipmentProfile profile = FindProfile(itemData);
        EquipmentSlot slot = ResolveSlot(itemData, profile);
        if (slot == EquipmentSlot.None) return false;

        return MeetsEquipRequirements(itemData, profile);
    }

    public bool CanEquip(ItemBase itemData, EquipmentSlot expectedSlot, int slotIndex)
    {
        if (itemData == null) return false;

        NYHEquipmentProfile profile = FindProfile(itemData);
        EquipmentSlot slot = ResolveSlot(itemData, profile);
        if (slot == EquipmentSlot.None || slot != expectedSlot) return false;

        return MeetsEquipRequirements(itemData, profile);
    }

    public bool TryEquipFromInventory(InventoryItem item, out InventoryItem replacedItem)
    {
        replacedItem = null;
        if (item == null || item.ItemData == null)
            return false;

        NYHEquipmentSlotKey key = GetDefaultKey(item.ItemData);
        return TryEquipInternal(new NYHEquipmentItemInstance(item.ItemData), item, key, out replacedItem);
    }

    public bool TryEquipFromInventory(InventoryItem item, EquipmentSlot slot, int slotIndex, out InventoryItem replacedItem)
    {
        replacedItem = null;
        if (item == null || item.ItemData == null)
            return false;

        NYHEquipmentProfile profile = FindProfile(item.ItemData);
        EquipmentSlot resolvedSlot = ResolveSlot(item.ItemData, profile);
        if (resolvedSlot != slot)
        {
            Debug.LogWarning($"NYHEquipmentController: {item.ItemData.Name} 아이템은 {slot} 슬롯에 장착할 수 없습니다.");
            return false;
        }

        NYHEquipmentSlotKey key = new NYHEquipmentSlotKey(slot, slotIndex);
        return TryEquipInternal(new NYHEquipmentItemInstance(item.ItemData), item, key, out replacedItem);
    }

    public bool Unequip(EquipmentSlot slot)
    {
        return Unequip(slot, 0, out _);
    }

    public bool Unequip(EquipmentSlot slot, int slotIndex, out InventoryItem itemView)
    {
        NYHEquipmentSlotKey key = new NYHEquipmentSlotKey(slot, slotIndex);
        itemView = null;

        if (!_equippedItems.TryGetValue(key, out NYHEquipmentItemInstance item))
            return false;

        if (_appliedProfiles.TryGetValue(key, out NYHEquipmentProfile profile))
        {
            ApplyStats(profile, -1);
            _appliedProfiles.Remove(key);
        }

        _equippedItems.Remove(key);
        if (_equippedViews.TryGetValue(key, out itemView))
            _equippedViews.Remove(key);

        OnUnequipped?.Invoke(slot, item);
        OnInventoryItemUnequipped?.Invoke(slot, slotIndex, itemView);
        return true;
    }

    public bool TryGetEquipped(EquipmentSlot slot, out NYHEquipmentItemInstance item)
    {
        return TryGetEquipped(slot, 0, out item);
    }

    public bool TryGetEquipped(EquipmentSlot slot, int slotIndex, out NYHEquipmentItemInstance item)
    {
        return _equippedItems.TryGetValue(new NYHEquipmentSlotKey(slot, slotIndex), out item);
    }

    public bool TryGetEquippedInventoryItem(EquipmentSlot slot, int slotIndex, out InventoryItem item)
    {
        return _equippedViews.TryGetValue(new NYHEquipmentSlotKey(slot, slotIndex), out item);
    }

    private bool TryEquipInternal(NYHEquipmentItemInstance item, InventoryItem inventoryView, NYHEquipmentSlotKey key, out InventoryItem replacedItem)
    {
        replacedItem = null;
        if (item == null || item.Data == null)
            return false;

        NYHEquipmentProfile profile = FindProfile(item.Data);
        EquipmentSlot slot = ResolveSlot(item.Data, profile);
        if (slot == EquipmentSlot.None)
        {
            Debug.LogWarning($"NYHEquipmentController: {item.Data.Name} 아이템은 장착 슬롯이 없어 장착할 수 없습니다.");
            return false;
        }

        if (slot != key.Slot)
        {
            Debug.LogWarning($"NYHEquipmentController: {item.Data.Name} 아이템은 {key.Slot} 슬롯에 장착할 수 없습니다.");
            return false;
        }

        if (!MeetsEquipRequirements(item.Data, profile))
            return false;

        if (_equippedItems.TryGetValue(key, out NYHEquipmentItemInstance oldItem))
        {
            if (_appliedProfiles.TryGetValue(key, out NYHEquipmentProfile oldProfile))
            {
                ApplyStats(oldProfile, -1);
                _appliedProfiles.Remove(key);
            }

            if (_equippedViews.TryGetValue(key, out replacedItem))
                _equippedViews.Remove(key);

            OnUnequipped?.Invoke(key.Slot, oldItem);
            OnInventoryItemUnequipped?.Invoke(key.Slot, key.Index, replacedItem);
        }

        _equippedItems[key] = item;

        if (inventoryView != null)
            _equippedViews[key] = inventoryView;

        if (profile != null)
        {
            _appliedProfiles[key] = profile;
            ApplyStats(profile, 1);
        }

        OnEquipped?.Invoke(key.Slot, item);
        OnInventoryItemEquipped?.Invoke(key.Slot, key.Index, inventoryView);
        return true;
    }

    private NYHEquipmentProfile FindProfile(ItemBase itemData)
    {
        for (int i = 0; i < equipmentProfiles.Count; i++)
        {
            NYHEquipmentProfile profile = equipmentProfiles[i];
            if (profile != null && profile.Matches(itemData))
                return profile;
        }

        return null;
    }

    private static EquipmentSlot ResolveSlot(ItemBase itemData, NYHEquipmentProfile profile)
    {
        if (profile != null)
            return profile.GetSlot();

        return itemData != null ? itemData.Slot : EquipmentSlot.None;
    }

    private NYHEquipmentSlotKey GetDefaultKey(ItemBase itemData)
    {
        NYHEquipmentProfile profile = FindProfile(itemData);
        EquipmentSlot slot = ResolveSlot(itemData, profile);

        if (slot == EquipmentSlot.Ring && _equippedItems.ContainsKey(new NYHEquipmentSlotKey(slot, 0)))
            return new NYHEquipmentSlotKey(slot, 1);

        return new NYHEquipmentSlotKey(slot, 0);
    }

    private bool MeetsEquipRequirements(ItemBase itemData, NYHEquipmentProfile profile)
    {
        NYHPlayerStats stats = Stats;
        if (stats == null)
        {
            Debug.LogWarning("NYHEquipmentController: NYHPlayerStats를 찾지 못해 장비 스탯을 적용할 수 없습니다.");
            return false;
        }

        int requiredStrength = GetRequiredStrength(itemData, profile);
        if (stats.Strength < requiredStrength)
        {
            Debug.LogWarning($"NYHEquipmentController: 힘이 부족합니다. 필요 힘 {requiredStrength}, 현재 힘 {stats.Strength}");
            return false;
        }

        int requiredLevel = profile != null ? profile.RequiredLevel : 0;
        if (requiredLevel > 0)
        {
            PlayerLevelSystem levelSystem = LevelSystem;
            if (levelSystem == null)
            {
                Debug.LogWarning("NYHEquipmentController: PlayerLevelSystem을 찾지 못해 레벨 조건을 확인할 수 없습니다.");
                return false;
            }

            if (levelSystem.Level < requiredLevel)
            {
                Debug.LogWarning($"NYHEquipmentController: 레벨이 부족합니다. 필요 레벨 {requiredLevel}, 현재 레벨 {levelSystem.Level}");
                return false;
            }
        }

        return true;
    }

    private static int GetRequiredStrength(ItemBase itemData, NYHEquipmentProfile profile)
    {
        if (profile != null && profile.RequiredStrength > 0)
            return profile.RequiredStrength;

        if (itemData is ArmorData armor)
            return armor.CanUseStrength;

        if (itemData is CloseWeapon closeWeapon)
            return closeWeapon.CanUseStrength;

        if (itemData is ThrowableWeapon throwableWeapon)
            return throwableWeapon.CanUseStrength;

        return 0;
    }

    private void ApplyStats(NYHEquipmentProfile profile, int sign)
    {
        if (profile == null || Stats == null)
            return;

        NYHEquipmentStatModifier[] modifiers = profile.StatModifiers;
        if (modifiers == null)
            return;

        for (int i = 0; i < modifiers.Length; i++)
        {
            NYHEquipmentStatModifier modifier = modifiers[i];
            if (modifier.amount == 0)
                continue;

            // 현재 NYHPlayerStats의 Health는 최대 체력 증가가 아니라 회복 이벤트로 처리된다.
            // 체력 장비 보너스가 필요하면 NYHPlayerStats에 MaxHealth 변경 API를 따로 추가해야 한다.
            if (modifier.statType == NYHStatType.Health)
            {
                Debug.LogWarning("NYHEquipmentController: Health 장비 보너스는 현재 최대 체력 증가로 적용되지 않습니다.");
                continue;
            }

            Stats.AddStat(modifier.statType, modifier.amount * sign);
        }
    }
}
