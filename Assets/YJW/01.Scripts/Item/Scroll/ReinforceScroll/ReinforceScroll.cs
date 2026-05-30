using UnityEngine;

[CreateAssetMenu(fileName = "ReinforceScroll", menuName = "Item/Scroll/ReinforceScroll")]
public class ReinforceScroll : ItemBase
{
    public override void Use(ItemInstance targetItem)
    {
        if (targetItem == null || targetItem.data == null || targetItem.data.CanEnhance == false || targetItem.enhanceLevel >= 15)
            return;

        targetItem.enhanceLevel++; // 강화 처리
    }
}
