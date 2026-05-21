using System;
using System.Collections.Generic;
using UnityEngine;

public class NYHEquipmentController : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("스탯을 적용할 플레이어 스탯입니다. 비워두면 씬의 NYHPlayerStats.Instance를 사용합니다.")]
    [SerializeField] private NYHPlayerStats playerStats;

    [Tooltip("YJW ItemBase에 NYH 장비 스탯을 연결하는 프로필 목록입니다.")]
    [SerializeField] private List<NYHEquipmentProfile> equipmentProfiles = new();

    private readonly Dictionary<EquipmentSlot, NYHEquipmentItemInstance> _equippedItems = new();
    private readonly Dictionary<EquipmentSlot, NYHEquipmentProfile> _appliedProfiles = new();

    public event Action<EquipmentSlot, NYHEquipmentItemInstance> OnEquipped;
    public event Action<EquipmentSlot, NYHEquipmentItemInstance> OnUnequipped;

    private NYHPlayerStats Stats
    {
        get
        {
            if (playerStats == null)
                playerStats = NYHPlayerStats.Instance;

            return playerStats;
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

        NYHEquipmentProfile profile = FindProfile(item.Data);
        EquipmentSlot slot = ResolveSlot(item.Data, profile);
        if (slot == EquipmentSlot.None)
        {
            Debug.LogWarning($"NYHEquipmentController: {item.Data.Name} 아이템은 장착 슬롯이 없어 장착할 수 없습니다.");
            return false;
        }

        if (!CanEquip(profile))
            return false;

        if (_equippedItems.ContainsKey(slot))
            Unequip(slot);

        _equippedItems[slot] = item;

        if (profile != null)
        {
            _appliedProfiles[slot] = profile;
            ApplyStats(profile, 1);
        }

        OnEquipped?.Invoke(slot, item);
        return true;
    }

    public bool Unequip(EquipmentSlot slot)
    {
        if (!_equippedItems.TryGetValue(slot, out NYHEquipmentItemInstance item))
            return false;

        if (_appliedProfiles.TryGetValue(slot, out NYHEquipmentProfile profile))
        {
            ApplyStats(profile, -1);
            _appliedProfiles.Remove(slot);
        }

        _equippedItems.Remove(slot);
        OnUnequipped?.Invoke(slot, item);
        return true;
    }

    public bool TryGetEquipped(EquipmentSlot slot, out NYHEquipmentItemInstance item)
    {
        return _equippedItems.TryGetValue(slot, out item);
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

    private bool CanEquip(NYHEquipmentProfile profile)
    {
        if (profile == null)
            return true;

        NYHPlayerStats stats = Stats;
        if (stats == null)
        {
            Debug.LogWarning("NYHEquipmentController: NYHPlayerStats를 찾지 못해 장비 스탯을 적용할 수 없습니다.");
            return false;
        }

        if (stats.Strength < profile.RequiredStrength)
        {
            Debug.LogWarning($"NYHEquipmentController: 힘이 부족합니다. 필요 힘 {profile.RequiredStrength}, 현재 힘 {stats.Strength}");
            return false;
        }

        return true;
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
