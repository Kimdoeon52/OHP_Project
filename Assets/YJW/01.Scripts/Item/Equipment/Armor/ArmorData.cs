using UnityEngine;

[CreateAssetMenu(fileName = "ArmorData", menuName = "Item/Equipment/Armor/Armor Data")]
public class ArmorData : ItemBase
{
    [SerializeField] private int minDefense;
    [SerializeField] private int maxDefense;
    [SerializeField] private int minEnhanceBonus;
    [SerializeField] private int maxEnhanceBonus;
    [SerializeField] private int canUseStrength;

    public int MinDefense => minDefense;
    public int MaxDefense => maxDefense;
    public int MinEnhanceBonus => minEnhanceBonus;
    public int MaxEnhanceBonus => maxEnhanceBonus;
    public int CanUseStrength => canUseStrength;

    public override ItemInstance CreateInstance()
    {
        return new ArmorInstance { data = this };
    }

    public override string GetToolTipStats()
    {
        return $"[{Name}]  <color=red>미감정</color>\n" +
               $"--------------------\n" +
               $"{Description}\n" +
               $"착용 힘: {CanUseStrength}\n" +
               $"피해 감소: {MinDefense}~{MaxDefense}";
    }
}
