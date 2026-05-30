using UnityEngine;

[CreateAssetMenu(fileName = "PurifyScroll", menuName = "Item/Scroll/PurifyScroll")]
public class PurifyScroll : ItemBase
{
    public override void Use(ItemInstance targetItem)
    {
        if (targetItem == null || targetItem.data == null || targetItem.data.CanCurse == false || targetItem.curse == null)
            return;
        
        targetItem.Purify();

    }
}
