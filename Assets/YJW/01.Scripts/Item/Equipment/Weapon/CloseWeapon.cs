using UnityEngine;

[CreateAssetMenu(fileName = "CloseWeapon", menuName = "Item/Equipment/Weapon/CloseWeapon")]
public class CloseWeapon : ItemBase
{
    [SerializeField] private int minDamage;
    [SerializeField] private int maxDamage;
    [SerializeField] private int CanUseStrength;
    [SerializeField] private float attackSpeed;
    [SerializeField] public int grade;
    [SerializeField] public bool isAnalyzed;

    public int GetDamage()
    {
        return Random.Range(minDamage, maxDamage + 1);
    }
}
