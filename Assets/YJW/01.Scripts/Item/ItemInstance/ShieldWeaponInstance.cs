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

        return $"[{shieldWeapon.Name}] {(enhanceLevel > 0 ? $"<color=yellow>+{enhanceLevel}</color>" : "")}\n" +
               $"--------------------\n" +
               $"{shieldWeapon.Description}\n" +
               $"착용 힘: {shieldWeapon.CanUseStrength}\n" +
               $"데미지: {GetMinDamage()}~{GetMaxDamage()}" +
               $"공격속도: {shieldWeapon.AttackSpeed}" +
               $"받은 피해 감소: {GetMinMinusDamage()}~{GetMaxMinusDamage()}" +
               $"{(curse != null ? $"\n{curse.CurseName}: {curse.Description}" : "")}" +
               $"{(magic != null ? $"\n{magic.MagicName}: {magic.Description}" : "")}";
    }
}
