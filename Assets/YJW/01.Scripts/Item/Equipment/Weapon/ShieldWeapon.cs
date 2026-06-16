using UnityEngine;

[CreateAssetMenu(fileName = "ShieldWeapon", menuName = "Item/Equipment/Weapon/ShieldWeapon")]
public class ShieldWeapon : CloseWeapon
{
    [SerializeField] private int minMinusDamage;
    [SerializeField] private int maxMinusDamage;

    public int MinMinusDamage => minMinusDamage;
    public int MaxMinusDamage => maxMinusDamage;

    public int GetMinusDamage()
    {
        return Random.Range(minMinusDamage, maxMinusDamage + 1);
    }

    public override string GetToolTipStats()
    {
        return $"[{Name}]  <color=red>미감정</color>\n" +
               $"--------------------\n" +
               $"{Description}\n" +
               $"착용 힘: {CanUseStrength}\n" +
               $"데미지: {MinDamage}~{MaxDamage}" +
               $"공격속도: {AttackSpeed}" +
               $"받은 피해 감소: {minMinusDamage}~{maxMinusDamage}";
    }
}
