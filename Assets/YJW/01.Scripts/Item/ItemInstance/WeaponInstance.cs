using System.Net.NetworkInformation;
using UnityEngine;

public class WeaponInstance : EquipmentInstance
{
    public CloseWeapon Weapon => data as CloseWeapon;

    public int GetMinDamage() => Weapon.MinDamage + enhanceLevel * Weapon.MinEnhanceBonus;
    public int GetMaxDamage() => Weapon.MaxDamage + enhanceLevel * Weapon.MaxEnhanceBonus;

    public int GetDamage() => Random.Range(GetMinDamage(), GetMaxDamage() + 1);

    // 강화 단계마다 착용 힘이 1씩 줄어들며, 0 밑으로는 내려가지 않는다.
    public int GetRequiredStrength() => Mathf.Max(0, Weapon.CanUseStrength - enhanceLevel);

    public override string GetToolTipStats()
    {
        if (!isAnalyzed)
            return Weapon.GetToolTipStats();

        return $"[{Weapon.LocalizedName}] {(enhanceLevel > 0 ? $"<color=yellow>+{enhanceLevel}</color>" : "")}\n" +
               $"--------------------\n" +
               $"{Weapon.LocalizedDescription}\n" +
               $"{LocalizationManager.GetOrDefault("msg_착용힘", "STR Required")}: {GetRequiredStrength()}\n" +
               $"{LocalizationManager.GetOrDefault("msg_T공격력", "Damage")}: {GetMinDamage()}~{GetMaxDamage()}" +
               $"{LocalizationManager.GetOrDefault("msg_T공격속도", "Attack Speed")}: {Weapon.AttackSpeed}" +
               (curse != null ? $"\n<color=orange>{curse.LocalizedName}: {curse.LocalizedDescription}</color>" : "") +
               (magic != null ? $"\n<color=#00ffff>{magic.LocalizedName}: {magic.LocalizedDescription}</color>" : "");
    }
}
