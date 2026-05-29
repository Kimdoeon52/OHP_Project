using UnityEngine;

[CreateAssetMenu(fileName = "PurifyScroll", menuName = "Item/Scroll/PurifyScroll")]
public class PurifyScroll : ItemBase
{
    public override void Use(ItemInstance targetItem)
    {
        if (targetItem.data.CanCurse == false)
            return;
        
        targetItem.Purify();

    }
}
