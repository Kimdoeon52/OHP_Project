using UnityEngine;

public class ArmorInstance : EquipmentInstance
{
    public ArmorData Armor => data as ArmorData;

    public int GetMinDefense() => Armor.MinDefense + enhanceLevel * Armor.MinEnhanceBonus;
    public int GetMaxDefense() => Armor.MaxDefense + enhanceLevel * Armor.MaxEnhanceBonus;

    public int GetDefense() => Random.Range(GetMinDefense(), GetMaxDefense() + 1);

    // 강화 단계마다 착용 힘이 1씩 줄어들며, 0 밑으로는 내려가지 않는다.
    public int GetRequiredStrength() => Mathf.Max(0, Armor.CanUseStrength - enhanceLevel);

    public void OnHit() => curse?.OnHit();

    public override string GetToolTipStats()
    {
        if (!isAnalyzed)
            return Armor.GetToolTipStats();

        return $"[{Armor.LocalizedName}] {(enhanceLevel > 0 ? $"<color=yellow>+{enhanceLevel}</color>" : "")}\n" +
               $"--------------------\n" +
               $"{Armor.LocalizedDescription}\n" +
               $"{LocalizationManager.GetOrDefault("msg_착용힘", "STR Required")}: {GetRequiredStrength()}\n" +
               $"{LocalizationManager.GetOrDefault("msg_피해감소", "Damage Reduction")}: {GetMinDefense()}~{GetMaxDefense()}" +
               (curse != null ? $"\n<color=orange>{curse.LocalizedName}: {curse.LocalizedDescription}</color>" : "") +
               (magic != null ? $"\n<color=aqua>{magic.LocalizedName}: {magic.LocalizedDescription}</color>" : "");
    }
}
