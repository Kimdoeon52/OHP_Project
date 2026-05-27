using UnityEngine;

public class WeaponInstance : ItemInstance
{
    public CloseWeapon Weapon => data as CloseWeapon;

    public int GetMinDamage() => Weapon.MinDamage + enhanceLevel * Weapon.MinEnhanceBonus;
    public int GetMaxDamage() => Weapon.MaxDamage + enhanceLevel * Weapon.MaxEnhanceBonus;

    public int GetDamage() => Random.Range(GetMinDamage(), GetMaxDamage() + 1);

    public string GetTooltipStats() =>
        $"{Weapon.Name}\n" +
        $"----------\n" +
        $"{Weapon.Description}\n" +
        $"착용 힘: {Weapon.CanUseStrength}\n" +
        $"데미지: {GetMinDamage()}~{GetMaxDamage()}";
}
