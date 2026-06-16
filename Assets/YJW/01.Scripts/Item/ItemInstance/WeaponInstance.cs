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

        return $"[{Weapon.Name}] {(enhanceLevel > 0 ? $"<color=yellow>+{enhanceLevel}</color>" : "")}\n" +
               $"--------------------\n" +
               $"{Weapon.Description}\n" +
               $"착용 힘: {Weapon.CanUseStrength}\n" +
               $"데미지: {GetMinDamage()}~{GetMaxDamage()}" + 
               $"공격속도: {Weapon.AttackSpeed}" +
               $"{(curse != null ? $"\n{curse.CurseName}: {curse.Description}" : "")}" +
               $"{(magic != null ? $"\n{magic.MagicName}: {magic.Description}" : "")}";
    }
}
