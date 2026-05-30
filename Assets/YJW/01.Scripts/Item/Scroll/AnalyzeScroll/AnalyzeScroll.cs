using UnityEngine;

[CreateAssetMenu(fileName = "AnalyzeScroll", menuName = "Item/Scroll/AnalyzeScroll")]
public class AnalyzeScroll : ItemBase
{
    public override void Use(ItemInstance targetItem)
    {
        if (targetItem == null || targetItem.data == null || targetItem.data.CanAnalyze == false || targetItem.isAnalyzed)
            return;

        targetItem.isAnalyzed = true; // 감정 처리
    }
}
