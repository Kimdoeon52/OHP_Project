using System.Net.NetworkInformation;
using UnityEngine;

public class WeaponInstance : EquipmentInstance
{
    public CloseWeapon Weapon => data as CloseWeapon;

    public int GetMinDamage() => Weapon.MinDamage + enhanceLevel * Weapon.MinEnhanceBonus;
    public int GetMaxDamage() => Weapon.MaxDamage + enhanceLevel * Weapon.MaxEnhanceBonus;

    public int GetDamage() => Random.Range(GetMinDamage(), GetMaxDamage() + 1);

    public override string GetToolTipStats()
    {
        if (!isAnalyzed)
            return Weapon.GetToolTipStats();

        return $"[{Weapon.LocalizedName}] {(enhanceLevel > 0 ? $"<color=yellow>+{enhanceLevel}</color>" : "")}\n" +
               $"--------------------\n" +
               $"{Weapon.LocalizedDescription}\n" +
               $"{LocalizationManager.GetOrDefault("msg_착용힘", "STR Required")}: {Weapon.CanUseStrength}\n" +
               $"{LocalizationManager.GetOrDefault("msg_T공격력", "Damage")}: {GetMinDamage()}~{GetMaxDamage()}" +
               $"{LocalizationManager.GetOrDefault("msg_T공격속도", "Attack Speed")}: {Weapon.AttackSpeed}" +
               $"{(curse != null ? $"\n{curse.LocalizedName}: {curse.LocalizedDescription}" : "")}" +
               $"{(magic != null ? $"\n{magic.LocalizedName}: {magic.LocalizedDescription}" : "")}";
    }
}
