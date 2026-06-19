using UnityEngine;

[CreateAssetMenu(fileName = "CloseWeapon", menuName = "Item/Equipment/Weapon/CloseWeapon")]
public class CloseWeapon : ItemBase
{
    [SerializeField] private int minDamage;
    [SerializeField] private int maxDamage;
    [SerializeField] private int minEnhanceBonus;
    [SerializeField] private int maxEnhanceBonus;
    [SerializeField] private int canUseStrength;
    [SerializeField] private float attackSpeed;

    public int MinDamage => minDamage;
    public int MaxDamage => maxDamage;
    public int MinEnhanceBonus => minEnhanceBonus;
    public int MaxEnhanceBonus => maxEnhanceBonus;
    public int CanUseStrength => canUseStrength;
    public float AttackSpeed => attackSpeed;

	public override ItemInstance CreateInstance()
	{
		return new WeaponInstance { data = this };
	}

    public override string GetToolTipStats()
    {
        return $"[{LocalizedName}]  <color=red>{LocalizationManager.GetOrDefault("msg_미감정", "Unidentified")}</color>\n" +
               $"--------------------\n" +
               $"{LocalizedDescription}\n" +
               $"{LocalizationManager.GetOrDefault("msg_착용힘", "STR Required")}: {CanUseStrength}\n" +
               $"{LocalizationManager.GetOrDefault("msg_T공격력", "Damage")}: {MinDamage}~{MaxDamage}" +
               $"{LocalizationManager.GetOrDefault("msg_T공격속도", "Attack Speed")}: {AttackSpeed}";
    }
}
