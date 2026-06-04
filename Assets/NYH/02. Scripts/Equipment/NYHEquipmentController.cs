using System;
using System.Collections.Generic;
using PrototypeRT;
using UnityEngine;

/// <summary>
/// 플레이어의 장비 장착 및 해제, 그리고 이로 인한 스탯 변동을 관리하는 코어 시스템입니다.
/// 인벤토리에서 전달받은 아이템 데이터를 분석해 올바른 슬롯(EquipmentSlot)에 장착하고,
/// 기존 장비가 있다면 스왑 처리하며, 장착 조건(요구 레벨/힘)을 검사합니다.
/// </summary>
public class NYHEquipmentController : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("스탯을 적용할 플레이어 스탯입니다. 비워두면 씬의 NYHPlayerStats.Instance를 사용합니다.")]
    [SerializeField] private NYHPlayerStats playerStats;

    [Tooltip("레벨 조건을 확인할 플레이어 레벨 시스템입니다. 비워두면 씬의 PlayerLevelSystem.Instance를 사용합니다.")]
    [SerializeField] private PlayerLevelSystem playerLevelSystem;

    [Tooltip("YJW ItemBase에 NYH 장비 스탯을 연결하는 프로필 목록입니다.")]
    [SerializeField] private List<NYHEquipmentProfile> equipmentProfiles = new();

    // 실제 장착된 장비 데이터(인스턴스)들을 슬롯 키를 기준으로 관리하는 딕셔너리
    private readonly Dictionary<NYHEquipmentSlotKey, NYHEquipmentItemInstance> _equippedItems = new();
    
    // 장착된 장비에 의해 현재 적용 중인 스탯 프로필들
    private readonly Dictionary<NYHEquipmentSlotKey, NYHEquipmentProfile> _appliedProfiles = new();
    
    // 장비와 연결된 인벤토리 내 UI 시각적 객체(InventoryItem)의 참조 보관용
    private readonly Dictionary<NYHEquipmentSlotKey, InventoryItem> _equippedViews = new();

    /// <summary>장비 장착 완료 시 발동하는 이벤트</summary>
    public event Action<EquipmentSlot, NYHEquipmentItemInstance> OnEquipped;
    /// <summary>장비 해제 완료 시 발동하는 이벤트</summary>
    public event Action<EquipmentSlot, NYHEquipmentItemInstance> OnUnequipped;
    /// <summary>인벤토리 아이템 뷰가 연결된 상태로 장착되었을 때 발동하는 UI 연동 이벤트</summary>
    public event Action<EquipmentSlot, int, InventoryItem> OnInventoryItemEquipped;
    /// <summary>인벤토리 아이템 뷰가 장착 해제되었을 때 발동하는 UI 연동 이벤트</summary>
    public event Action<EquipmentSlot, int, InventoryItem> OnInventoryItemUnequipped;
    /// <summary>장착 중인 장비의 강화/감정/정화 같은 내부 상태가 바뀌었을 때 발동하는 이벤트</summary>
    public event Action<EquipmentSlot, int, InventoryItem> OnInventoryItemStateChanged;

    /// <summary>
    /// 플레이어 스탯 시스템에 접근하는 지연 초기화 프로퍼티입니다.
    /// 명시적으로 할당되지 않았다면 싱글톤 인스턴스를 찾습니다.
    /// </summary>
    private NYHPlayerStats Stats
    {
        get
        {
            if (playerStats == null)
                playerStats = NYHPlayerStats.Instance;

            return playerStats;
        }
    }

    /// <summary>
    /// 플레이어 레벨 시스템에 접근하는 지연 초기화 프로퍼티입니다.
    /// 명시적으로 할당되지 않았다면 싱글톤 인스턴스를 찾습니다.
    /// </summary>
    private PlayerLevelSystem LevelSystem
    {
        get
        {
            if (playerLevelSystem == null)
                playerLevelSystem = PlayerLevelSystem.Instance;

            return playerLevelSystem;
        }
    }

    /// <summary>
    /// 원본 아이템 데이터를 받아 새로운 장비 인스턴스로 변환 후 장착을 시도합니다.
    /// </summary>
    public bool TryEquip(ItemBase itemData)
    {
        if (itemData == null)
            return false;

        return TryEquip(new NYHEquipmentItemInstance(itemData));
    }

    /// <summary>
    /// 이미 생성된 장비 인스턴스의 장착을 시도합니다. 
    /// UI 참조(InventoryItem) 없이 순수 데이터 레벨의 장착 시 사용됩니다.
    /// </summary>
    public bool TryEquip(NYHEquipmentItemInstance item)
    {
        if (item == null || item.Data == null)
            return false;

        NYHEquipmentSlotKey key = GetDefaultKey(item.Data);
        return TryEquipInternal(item, null, key, out _);
    }

    /// <summary>
    /// 지정한 아이템을 현재 플레이어가 장착할 수 있는 조건(요구 레벨, 힘)인지 사전에 검증합니다.
    /// </summary>
    public bool CanEquip(ItemBase itemData)
    {
        if (itemData == null) return false;

        NYHEquipmentProfile profile = FindProfile(itemData);
        EquipmentSlot slot = ResolveSlot(itemData, profile);
        if (slot == EquipmentSlot.None) return false;

        return MeetsEquipRequirements(itemData, profile);
    }

    /// <summary>
    /// 지정한 아이템을 특정 슬롯에 장착할 수 있는지 사전에 검증합니다.
    /// </summary>
    public bool CanEquip(ItemBase itemData, EquipmentSlot expectedSlot, int slotIndex)
    {
        if (itemData == null) return false;

        NYHEquipmentProfile profile = FindProfile(itemData);
        EquipmentSlot slot = ResolveSlot(itemData, profile);
        if (slot == EquipmentSlot.None || slot != expectedSlot) return false;

        return MeetsEquipRequirements(itemData, profile);
    }

    /// <summary>
    /// 대상 아이템 데이터가 들어갈 기본 슬롯과 인덱스를 판별해 반환합니다.
    /// </summary>
    public bool TryGetTargetSlotFor(ItemBase itemData, out EquipmentSlot slot, out int slotIndex)
    {
        slot = EquipmentSlot.None;
        slotIndex = 0;

        // 자동 장착 전에 슬롯과 조건을 모두 확인합니다.
        // 실패하면 장착/교체 시도를 하지 않아 인벤토리 상태가 바뀌지 않습니다.
        if (itemData == null || !CanEquip(itemData))
            return false;

        // 반지는 0번 칸이 차 있으면 1번 칸을 우선 선택하는 기존 GetDefaultKey 규칙을 그대로 따릅니다.
        NYHEquipmentSlotKey key = GetDefaultKey(itemData);
        slot = key.Slot;
        slotIndex = key.Index;
        return slot != EquipmentSlot.None;
    }

    /// <summary>
    /// 인벤토리 UI 상의 아이템을 장착 시도합니다.
    /// 장착 성공 시 기존에 착용 중이던 아이템 뷰가 있다면 replacedItem으로 반환하여 인벤토리로 되돌릴 수 있게 합니다.
    /// </summary>
    public bool TryEquipFromInventory(InventoryItem item, out InventoryItem replacedItem)
    {
        replacedItem = null;
        if (item == null || item.ItemData == null)
            return false;

        NYHEquipmentSlotKey key = GetDefaultKey(item.ItemData);
        return TryEquipInternal(new NYHEquipmentItemInstance(item.ItemData, item.ItemInstance), item, key, out replacedItem);
    }

    /// <summary>
    /// 인벤토리 UI 상의 아이템을 명시된 슬롯에 강제 지정하여 장착 시도합니다.
    /// </summary>
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
        return TryEquipInternal(new NYHEquipmentItemInstance(item.ItemData, item.ItemInstance), item, key, out replacedItem);
    }

    /// <summary>
    /// 지정된 슬롯의 인덱스 0번 장비를 해제합니다.
    /// </summary>
    public bool Unequip(EquipmentSlot slot)
    {
        return Unequip(slot, 0, out _);
    }

    /// <summary>
    /// 특정 슬롯과 인덱스에 있는 장비를 해제하고 적용되던 스탯 보너스를 롤백합니다.
    /// 반환되는 itemView는 인벤토리로 다시 들어갈 때 사용됩니다.
    /// </summary>
    public bool Unequip(EquipmentSlot slot, int slotIndex, out InventoryItem itemView)
    {
        NYHEquipmentSlotKey key = new NYHEquipmentSlotKey(slot, slotIndex);
        itemView = null;

        if (!_equippedItems.TryGetValue(key, out NYHEquipmentItemInstance item))
            return false;

        // 장착 해제 시 해당 장비가 올려주던 스탯을 다시 빼줌 (-1)
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

    /// <summary>
    /// 지정된 슬롯(인덱스 0번)에 장착된 장비 인스턴스를 가져옵니다.
    /// </summary>
    public bool TryGetEquipped(EquipmentSlot slot, out NYHEquipmentItemInstance item)
    {
        return TryGetEquipped(slot, 0, out item);
    }

    /// <summary>
    /// 특정 슬롯과 인덱스에 장착된 장비 인스턴스를 가져옵니다.
    /// </summary>
    public bool TryGetEquipped(EquipmentSlot slot, int slotIndex, out NYHEquipmentItemInstance item)
    {
        return _equippedItems.TryGetValue(new NYHEquipmentSlotKey(slot, slotIndex), out item);
    }

    /// <summary>
    /// 특정 슬롯과 인덱스에 장착된 장비의 인벤토리 UI 객체를 가져옵니다.
    /// </summary>
    public bool TryGetEquippedInventoryItem(EquipmentSlot slot, int slotIndex, out InventoryItem item)
    {
        return _equippedViews.TryGetValue(new NYHEquipmentSlotKey(slot, slotIndex), out item);
    }

    /// <summary>
    /// 장착 중인 InventoryItem의 내부 상태가 바뀌었음을 알립니다.
    /// 강화/감정/정화는 아이템 오브젝트를 교체하지 않고 ItemInstance 값만 바꾸므로,
    /// 장착/해제 이벤트 대신 이 이벤트로 스탯 UI와 장비 슬롯 UI를 다시 계산하게 합니다.
    /// </summary>
    public bool NotifyEquippedInventoryItemStateChanged(InventoryItem changedItem)
    {
        if (changedItem == null)
            return false;

        foreach (KeyValuePair<NYHEquipmentSlotKey, InventoryItem> pair in _equippedViews)
        {
            if (pair.Value != changedItem)
                continue;

            OnInventoryItemStateChanged?.Invoke(pair.Key.Slot, pair.Key.Index, changedItem);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 장착의 실제 내부 로직을 처리하는 코어 메서드입니다.
    /// 기존 장비 해제, 새 장비 등록, 스탯 부여, 이벤트를 일괄 호출합니다.
    /// </summary>
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

        // 타겟 슬롯에 이미 착용 중인 장비가 있을 경우 스왑을 위해 기존 장비 해제
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

        // 새 장비 등록
        _equippedItems[key] = item;

        if (inventoryView != null)
            _equippedViews[key] = inventoryView;

        // 새 장비 스탯 보너스 부여 (+1)
        if (profile != null)
        {
            _appliedProfiles[key] = profile;
            ApplyStats(profile, 1);
        }

        OnEquipped?.Invoke(key.Slot, item);
        OnInventoryItemEquipped?.Invoke(key.Slot, key.Index, inventoryView);
        return true;
    }

    /// <summary>
    /// 원본 아이템 데이터와 매칭되는 장비 스탯 프로필(NYHEquipmentProfile)을 목록에서 찾습니다.
    /// </summary>
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

    /// <summary>
    /// 프로필이나 원본 데이터에서 이 장비가 들어갈 슬롯 부위(머리, 몸, 무기 등)를 추출합니다.
    /// </summary>
    private static EquipmentSlot ResolveSlot(ItemBase itemData, NYHEquipmentProfile profile)
    {
        if (profile != null)
            return profile.GetSlot();

        return itemData != null ? itemData.Slot : EquipmentSlot.None;
    }

    /// <summary>
    /// 장착하려는 장비의 기본 슬롯 키를 결정합니다.
    /// 반지(Ring)의 경우 0번이 차 있으면 자동으로 1번(보조) 슬롯을 반환합니다.
    /// </summary>
    private NYHEquipmentSlotKey GetDefaultKey(ItemBase itemData)
    {
        NYHEquipmentProfile profile = FindProfile(itemData);
        EquipmentSlot slot = ResolveSlot(itemData, profile);

        if (slot == EquipmentSlot.Ring && _equippedItems.ContainsKey(new NYHEquipmentSlotKey(slot, 0)))
            return new NYHEquipmentSlotKey(slot, 1);

        return new NYHEquipmentSlotKey(slot, 0);
    }

    /// <summary>
    /// 장착 시 플레이어의 힘(Strength)이나 레벨(Level)이 장비의 요구 조건을 충족하는지 검증합니다.
    /// </summary>
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

    /// <summary>
    /// 프로필 또는 아이템 원본 데이터에서 요구 힘 수치를 가져옵니다.
    /// </summary>
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

    /// <summary>
    /// 프로필에 정의된 추가 스탯(공격력, 방어력, 회피율 등)을 플레이어 스탯 시스템에 반영합니다.
    /// sign이 1이면 증가(장착), -1이면 감소(해제)합니다.
    /// </summary>
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

