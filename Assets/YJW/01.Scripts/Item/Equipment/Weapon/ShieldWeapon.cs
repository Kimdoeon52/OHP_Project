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

    // 방패 전용 인스턴스 생성 (받는 피해 차감용)
    public override ItemInstance CreateInstance()
    {
        return new ShieldWeaponInstance { data = this };
    }

    public override string GetToolTipStats()
    {
        return $"[{LocalizedName}]  <color=red>{LocalizationManager.GetOrDefault("msg_미감정", "Unidentified")}</color>\n" +
               $"--------------------\n" +
               $"{LocalizedDescription}\n" +
               $"{LocalizationManager.GetOrDefault("msg_착용힘", "STR Required")}: {CanUseStrength}\n" +
               $"{LocalizationManager.GetOrDefault("msg_T공격력", "Damage")}: {MinDamage}~{MaxDamage}" +
               $"{LocalizationManager.GetOrDefault("msg_T공격속도", "Attack Speed")}: {AttackSpeed}" +
               $"{LocalizationManager.GetOrDefault("msg_받은피해감소", "Block Reduction")}: {minMinusDamage}~{maxMinusDamage}";
    }
}
