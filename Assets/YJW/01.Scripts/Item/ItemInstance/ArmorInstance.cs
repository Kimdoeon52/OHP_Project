using UnityEngine;

public class ArmorInstance : ItemInstance
{
    public ArmorData Armor => data as ArmorData;

    public int GetMinDefense() => Armor.MinDefense + enhanceLevel * Armor.MinEnhanceBonus;
    public int GetMaxDefense() => Armor.MaxDefense + enhanceLevel * Armor.MaxEnhanceBonus;

    public int GetDefense() => Random.Range(GetMinDefense(), GetMaxDefense() + 1);

    public string GetTooltipStats() =>
        $"{Armor.Name}\n" +
        $"----------\n" +
        $"{Armor.Description}\n" +
        $"착용 힘: {Armor.CanUseStrength}\n" +
        $"피해 감소: {GetMinDefense()}~{GetMaxDefense()}";
}
