using UnityEngine;

public class ShieldWeaponInstance : WeaponInstance
{
    public ShieldWeapon shieldWeapon => data as ShieldWeapon;

    public int GetMinMinusDamage() => shieldWeapon.MinMinusDamage + enhanceLevel * shieldWeapon.MinEnhanceBonus;
    public int GetMaxMinusDamage() => shieldWeapon.MaxMinusDamage + enhanceLevel * shieldWeapon.MaxEnhanceBonus;

    public int GetMinusDamage() => Random.Range(GetMinMinusDamage(), GetMaxMinusDamage() + 1);

    public override string GetToolTipStats()
    {
        if (!isAnalyzed)
            return shieldWeapon.GetToolTipStats();

        return $"[{shieldWeapon.LocalizedName}] {(enhanceLevel > 0 ? $"<color=yellow>+{enhanceLevel}</color>" : "")}\n" +
               $"--------------------\n" +
               $"{shieldWeapon.LocalizedDescription}\n" +
               $"{LocalizationManager.GetOrDefault("msg_착용힘", "STR Required")}: {shieldWeapon.CanUseStrength}\n" +
               $"{LocalizationManager.GetOrDefault("msg_T공격력", "Damage")}: {GetMinDamage()}~{GetMaxDamage()}" +
               $"{LocalizationManager.GetOrDefault("msg_T공격속도", "Attack Speed")}: {shieldWeapon.AttackSpeed}" +
               $"{LocalizationManager.GetOrDefault("msg_받은피해감소", "Block Reduction")}: {GetMinMinusDamage()}~{GetMaxMinusDamage()}" +
               $"{(curse != null ? $"\n{curse.LocalizedName}: {curse.LocalizedDescription}" : "")}" +
               $"{(magic != null ? $"\n{magic.LocalizedName}: {magic.LocalizedDescription}" : "")}";
    }
}
