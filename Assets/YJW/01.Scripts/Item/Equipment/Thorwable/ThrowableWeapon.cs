using UnityEngine;

[CreateAssetMenu(fileName = "ThrowableWeapon", menuName = "Item/Equipment/Throwable/ThrowableWeapon")]
public class ThrowableWeapon : ItemBase
{
    [SerializeField] private int minDamage;
    [SerializeField] private int maxDamage;
    [SerializeField] private int minEnhanceBonus;
    [SerializeField] private int maxEnhanceBonus;
    [SerializeField] private int canUseStrength;
    [SerializeField] private PoolType poolType;
    [SerializeField] private float attackSpeed;
    [SerializeField] private int durability;

    public int MinDamage => minDamage;
    public int MaxDamage => maxDamage;
    public int MinEnhanceBonus => minEnhanceBonus;
    public int MaxEnhanceBonus => maxEnhanceBonus;
    public int CanUseStrength => canUseStrength;
    public PoolType PoolType => poolType;
    public int Durability => durability;

	public override ItemInstance CreateInstance()
	{
		return new ThrowableWeaponInstance { data = this };
	}
}
