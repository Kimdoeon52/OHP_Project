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
        return $"[{LocalizedName}]  <color=red>{LocalizationManager.GetOrDefault("msg_미감정", "Unidentified")}</color>\n" +
               $"--------------------\n" +
               $"{LocalizedDescription}\n" +
               $"{LocalizationManager.GetOrDefault("msg_착용힘", "STR Required")}: {CanUseStrength}\n" +
               $"{LocalizationManager.GetOrDefault("msg_피해감소", "Damage Reduction")}: {MinDefense}~{MaxDefense}";
    }
}
