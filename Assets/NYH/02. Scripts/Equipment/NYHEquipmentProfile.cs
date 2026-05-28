using UnityEngine;

[CreateAssetMenu(fileName = "NYHEquipmentProfile", menuName = "NYH/Equipment/Profile")]
public class NYHEquipmentProfile : ScriptableObject
{
    [Header("연결 아이템")]
    [Tooltip("이 장비 프로필이 적용될 YJW ItemBase 에셋입니다.")]
    [SerializeField] private ItemBase itemData;

    [Tooltip("ItemBase의 슬롯 설정 대신 사용할 장착 슬롯입니다. None이면 ItemBase의 Slot 값을 사용합니다.")]
    [SerializeField] private EquipmentSlot slotOverride = EquipmentSlot.None;

    [Header("장착 조건")]
    [Tooltip("장착에 필요한 최소 힘 수치입니다.")]
    [SerializeField, Min(0)] private int requiredStrength;

    [Tooltip("장착에 필요한 최소 플레이어 레벨입니다. 0이면 레벨 제한이 없습니다.")]
    [SerializeField, Min(0)] private int requiredLevel;

    [Header("스탯 보너스")]
    [Tooltip("장착 중 플레이어에게 더할 스탯 목록입니다. 해제하면 같은 수치가 빠집니다.")]
    [SerializeField] private NYHEquipmentStatModifier[] statModifiers;

    [Header("저주 / 인챈트 예약")]
    [Tooltip("후속 저주 시스템에서 사용할 저주 식별자입니다. 비워두면 저주 효과가 없습니다.")]
    [SerializeField] private string curseId;

    [Tooltip("후속 인챈트 시스템에서 사용할 인챈트 식별자입니다. 비워두면 인챈트 효과가 없습니다.")]
    [SerializeField] private string enchantId;

    public ItemBase ItemData => itemData;
    public int RequiredStrength => requiredStrength;
    public int RequiredLevel => requiredLevel;
    public string CurseId => curseId;
    public string EnchantId => enchantId;
    public NYHEquipmentStatModifier[] StatModifiers => statModifiers;

    public EquipmentSlot GetSlot()
    {
        if (slotOverride != EquipmentSlot.None)
            return slotOverride;

        return itemData != null ? itemData.Slot : EquipmentSlot.None;
    }

    public bool Matches(ItemBase item)
    {
        return itemData != null && itemData == item;
    }
}
