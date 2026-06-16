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
        return $"[{Name}]  <color=red>미감정</color>\n" +
               $"--------------------\n" +
               $"{Description}\n" +
               $"착용 힘: {CanUseStrength}\n" +
               $"데미지: {MinDamage}~{MaxDamage}" +
               $"공격속도: {AttackSpeed}";
    }
}
