using UnityEngine;

[CreateAssetMenu(fileName = "PurifyScroll", menuName = "Item/Scroll/PurifyScroll")]
public class PurifyScroll : ItemBase
{
    public override void Use(ItemInstance targetItem)
    {
		if (targetItem is not EquipmentInstance equip) return;
		if (equip.data == null || equip.data.CanCurse == false || equip.curse == null)
            return;

		equip.Purify();

    }
}
