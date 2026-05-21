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

    [Header("스탯 보너스")]
    [Tooltip("장착 중 플레이어에게 더할 스탯 목록입니다. 해제하면 같은 수치가 빠집니다.")]
    [SerializeField] private NYHEquipmentStatModifier[] statModifiers;

    public ItemBase ItemData => itemData;
    public int RequiredStrength => requiredStrength;
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
