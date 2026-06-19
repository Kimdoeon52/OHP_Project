using UnityEngine;

public class ArmorInstance : EquipmentInstance
{
    public ArmorData Armor => data as ArmorData;

    public int GetMinDefense() => Armor.MinDefense + enhanceLevel * Armor.MinEnhanceBonus;
    public int GetMaxDefense() => Armor.MaxDefense + enhanceLevel * Armor.MaxEnhanceBonus;

    public int GetDefense() => Random.Range(GetMinDefense(), GetMaxDefense() + 1);

    public void OnHit() => curse?.OnHit();

    public override string GetToolTipStats()
    {
        if (!isAnalyzed)
            return Armor.GetToolTipStats();

        return $"[{Armor.LocalizedName}] {(enhanceLevel > 0 ? $"<color=yellow>+{enhanceLevel}</color>" : "")}\n" +
               $"--------------------\n" +
               $"{Armor.LocalizedDescription}\n" +
               $"{LocalizationManager.GetOrDefault("msg_착용힘", "STR Required")}: {Armor.CanUseStrength}\n" +
               $"{LocalizationManager.GetOrDefault("msg_피해감소", "Damage Reduction")}: {GetMinDefense()}~{GetMaxDefense()}" +
               $"{(curse != null ? $"\n{curse.LocalizedName}: {curse.LocalizedDescription}" : "")}" +
               $"{(magic != null ? $"\n{magic.LocalizedName}: {magic.LocalizedDescription}" : "")}";
    }
}
