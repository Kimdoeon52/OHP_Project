using UnityEngine;

[CreateAssetMenu(fileName = "CloseWeapon", menuName = "Item/Equipment/Weapon/CloseWeapon")]
public class CloseWeapon : ItemBase
{
    [SerializeField] private int minDamage;
    [SerializeField] private int maxDamage;
    [SerializeField] private int CanUseStrength;
    [SerializeField] private float attackSpeed;

    public int GetBaseDamage()
    {
        return Random.Range(minDamage, maxDamage + 1);
    }
}
