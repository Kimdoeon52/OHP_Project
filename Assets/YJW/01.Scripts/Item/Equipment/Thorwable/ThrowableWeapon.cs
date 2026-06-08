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
    [SerializeField] private int startCount;

    public int MinDamage => minDamage;
    public int MaxDamage => maxDamage;
    public int MinEnhanceBonus => minEnhanceBonus;
    public int MaxEnhanceBonus => maxEnhanceBonus;
    public int CanUseStrength => canUseStrength;
    public PoolType PoolType => poolType;
    public int Durability => durability;
    public int StartCount => startCount;

    public override ItemInstance CreateInstance()
    {
        var inst = new ThrowableWeaponInstance { data = this };
        for (int i = 0; i < startCount; i++)
            inst.currentDurabilities.Add(durability);
        return inst;
    }
}
