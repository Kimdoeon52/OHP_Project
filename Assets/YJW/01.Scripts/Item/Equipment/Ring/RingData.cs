using UnityEngine;

[CreateAssetMenu(fileName = "New Ring", menuName = "Item/Ring/New Ring")]
public class RingData : ItemBase
{
    /// <summary>
    /// 유니티 인스펙터에서 RingData 에셋을 수정할 때 자동으로 기본 분류를 맞춥니다.
    /// 반지는 "장비 아이템"이고, 장착 슬롯은 "Ring"이어야 NYHEquipmentController가 반지 슬롯에 넣을 수 있습니다.
    ///
    /// 중요한 점:
    /// 여기서는 이동 속도, 공격 속도, 쿨다운 감소 같은 실제 효과를 적용하지 않습니다.
    /// 반지 효과 수치는 NYHEquipmentProfile의 statModifiers / finalStatModifiers에서 설정하고,
    /// 장착 순간 NYHEquipmentController가 그 프로필을 읽어 플레이어에게 적용합니다.
    /// </summary>
    private void OnValidate()
    {
        // 반지 효과 수치는 RingData가 직접 적용하지 않고 NYHEquipmentProfile에서 튜닝한다.
        itemType = ItemType.Equipment;
        slot = EquipmentSlot.Ring;
    }

    /// <summary>
    /// 인벤토리에 들어갈 실제 아이템 인스턴스를 만듭니다.
    ///
    /// ItemBase는 원본 데이터입니다. 모든 "신속의 반지" 에셋이 공유하는 설명서 같은 것입니다.
    /// EquipmentInstance는 실제 플레이어가 가진 물건 한 개입니다.
    /// 그래서 같은 신속의 반지라도 한 개는 0강, 다른 한 개는 2강처럼 서로 다른 enhanceLevel을 가질 수 있습니다.
    ///
    /// NYH 장비 시스템은 YJW InventoryItem 안의 ItemInstance를 SourceInstance로 받아서 들고 있다가,
    /// 장착 보정을 다시 계산할 때 EquipmentInstance.enhanceLevel을 읽습니다.
    /// </summary>
    public override ItemInstance CreateInstance()
    {
        // 강화 주문서가 enhanceLevel을 올릴 수 있도록 반지도 장비 인스턴스로 생성한다.
        return new EquipmentInstance { data = this };
    }
}
