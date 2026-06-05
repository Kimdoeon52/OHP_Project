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

    [Tooltip("반지/마법부여/저주가 최종 이동/공격 지연 시간에 개입하는 공용 통로입니다. 비워도 플레이어에서 자동으로 찾아 붙입니다.")]
    [SerializeField] private NYHFinalStatModifierController finalStatModifierController;

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
    /// 최종 행동 지연 보정 컨트롤러를 가져오는 지연 초기화 프로퍼티입니다.
    /// 인스펙터에 직접 연결되어 있으면 그 값을 쓰고,
    /// 연결이 비어 있으면 NYHPlayerStats가 붙은 플레이어 오브젝트에서 찾습니다.
    /// 그래도 없으면 런타임에 자동으로 AddComponent 해서 "반지 효과가 아예 적용되지 않는 상황"을 막습니다.
    /// </summary>
    private NYHFinalStatModifierController FinalStatModifiers
    {
        get
        {
            if (finalStatModifierController == null && Stats != null)
            {
                finalStatModifierController = Stats.GetComponent<NYHFinalStatModifierController>();

                if (finalStatModifierController == null)
                {
                    // 인스펙터 연결이 빠져도 장착 보정이 유실되지 않도록 플레이어에 런타임으로 붙인다.
                    finalStatModifierController = Stats.gameObject.AddComponent<NYHFinalStatModifierController>();
                }
            }

            return finalStatModifierController;
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

        // 장비가 빠지면 그 장비가 주던 최종 초 단위 보정도 즉시 빠져야 한다.
        // 기존 statModifiers는 ApplyStats(profile, -1)로 되돌리고,
        // finalStatModifiers는 장착 중인 전체 장비를 다시 훑어서 새 목록으로 교체한다.
        RebuildFinalStatModifiers();

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

            // 강화 주문서, 감정, 정화, 마법부여처럼 장착은 그대로인데 ItemInstance 내부 값만 바뀐 경우가 있다.
            // 이때 해제/재장착 이벤트가 없으므로 여기서 최종 보정을 다시 계산해 강화 수치를 반영한다.
            RebuildFinalStatModifiers();
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

        // 새 장비가 들어온 뒤 최종 초 단위 보정도 갱신한다.
        // 같은 source(this)로 SetModifiers를 호출하기 때문에 이전 장비 보정과 중복 누적되지 않는다.
        RebuildFinalStatModifiers();

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
    /// 현재 장착 중인 모든 장비를 다시 읽어서 최종 행동 지연 보정 목록을 새로 만듭니다.
    ///
    /// 왜 "변경된 장비 하나만 더하고 빼는 방식"을 쓰지 않느냐면,
    /// 강화 주문서나 정화/마법부여처럼 장착 슬롯은 그대로인데 내부 ItemInstance 값만 바뀌는 경우가 있기 때문입니다.
    /// 매번 전체 장착 장비를 다시 읽으면 계산은 조금 더 하지만,
    /// 예전 강화 수치가 남거나 같은 반지 효과가 두 번 쌓이는 버그를 피할 수 있습니다.
    ///
    /// 흐름:
    /// 1. _equippedItems에서 현재 장착 중인 장비들을 가져옵니다.
    /// 2. 각 장비의 ItemBase와 매칭되는 NYHEquipmentProfile을 찾습니다.
    /// 3. 프로필의 FinalStatModifiers 배열을 읽습니다.
    /// 4. YJW 쪽 ItemInstance가 EquipmentInstance라면 enhanceLevel을 읽어서 강화당 보정까지 계산합니다.
    /// 5. 계산이 끝난 값을 NYHFinalStatModifierController에 source=this로 한 번에 등록합니다.
    /// </summary>
    private void RebuildFinalStatModifiers()
    {
        NYHFinalStatModifierController modifierController = FinalStatModifiers;
        if (modifierController == null)
            return;

        List<NYHFinalStatModifierRuntime> runtimeModifiers = new();

        foreach (KeyValuePair<NYHEquipmentSlotKey, NYHEquipmentItemInstance> pair in _equippedItems)
        {
            NYHEquipmentItemInstance item = pair.Value;
            if (item == null || item.Data == null)
                continue;

            // YJW의 원본 ItemBase와 NYH의 장비 프로필을 매칭한다.
            // 실제 수치 튜닝은 RingData가 아니라 NYHEquipmentProfile 쪽 finalStatModifiers에 들어 있다.
            NYHEquipmentProfile profile = FindProfile(item.Data);
            NYHFinalStatModifier[] finalModifiers = profile != null ? profile.FinalStatModifiers : null;
            if (finalModifiers == null || finalModifiers.Length == 0)
                continue;

            // 같은 반지라도 0강/1강/2강 효과가 달라질 수 있으므로 ItemInstance에서 강화 수치를 읽는다.
            int enhanceLevel = GetEnhanceLevel(item);
            for (int i = 0; i < finalModifiers.Length; i++)
            {
                NYHFinalStatModifier modifier = finalModifiers[i];
                float flatSeconds = modifier.Evaluate(enhanceLevel);
                if (Mathf.Approximately(flatSeconds, 0f))
                    continue;

                runtimeModifiers.Add(new NYHFinalStatModifierRuntime(modifier.target, flatSeconds));
            }
        }

        // 장착 장비 전체를 다시 계산해서 강화/저주/마법부여 변경 후에도 누적 오차가 남지 않게 한다.
        modifierController.SetModifiers(this, runtimeModifiers);
    }

    /// <summary>
    /// 장착된 NYHEquipmentItemInstance 안에서 YJW ItemInstance를 꺼내 강화 수치를 읽습니다.
    ///
    /// YJW 쪽에서 반지를 만들 때 RingData.CreateInstance()가 EquipmentInstance를 반환하면,
    /// 인벤토리 아이템은 그 EquipmentInstance를 들고 있게 됩니다.
    /// NYH 장비 시스템은 InventoryItem.ItemInstance를 SourceInstance로 보관하고,
    /// 여기서 SourceInstance가 EquipmentInstance인지 확인한 뒤 enhanceLevel을 읽습니다.
    ///
    /// SourceInstance가 없거나 EquipmentInstance가 아니면 강화 수치를 알 수 없으므로 0강으로 취급합니다.
    /// </summary>
    private static int GetEnhanceLevel(NYHEquipmentItemInstance item)
    {
        if (item != null && item.SourceInstance is EquipmentInstance equipmentInstance)
            return equipmentInstance.enhanceLevel;

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



            Stats.AddStat(modifier.statType, modifier.amount * sign);
        }
    }

    /// <summary>
    /// 장비 컨트롤러가 꺼질 때 자신이 등록했던 최종 보정을 제거합니다.
    /// 씬 전환이나 플레이어 비활성화 중에 이전 보정값이 컨트롤러 안에 남는 것을 막기 위한 정리 작업입니다.
    /// </summary>
    private void OnDisable()
    {
        if (finalStatModifierController != null)
            finalStatModifierController.ClearModifiers(this);
    }

    /// <summary>
    /// 장비 컨트롤러가 다시 켜졌을 때 현재 장착 상태를 기준으로 최종 보정을 다시 등록합니다.
    /// OnDisable에서 지운 값을 복구하기 위한 짝 함수입니다.
    /// </summary>
    private void OnEnable()
    {
        RebuildFinalStatModifiers();
    }
}

