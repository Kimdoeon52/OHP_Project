using UnityEngine;

[CreateAssetMenu(fileName = "ReinforceScroll", menuName = "Item/Scroll/ReinforceScroll")]
public class ReinforceScroll : ItemBase
{
    public override void Use(ItemInstance targetItem)
    {

		if (targetItem is not EquipmentInstance equip) return;
		if (equip.data == null || equip.data.CanEnhance == false || equip.enhanceLevel >= 15)
            return;

		equip.enhanceLevel++; // 강화 처리

		if (equip is ThrowableWeaponInstance throwable)
			for (int i = 0; i < throwable.currentDurabilities.Count; i++)
				throwable.currentDurabilities[i] = throwable.weapon.Durability;
    }
}
