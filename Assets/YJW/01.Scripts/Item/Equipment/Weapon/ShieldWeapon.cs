using UnityEngine;

[CreateAssetMenu(fileName = "ShieldWeapon", menuName = "Item/Equipment/Weapon/ShieldWeapon")]
public class ShieldWeapon : CloseWeapon
{
    [SerializeField] private int minMinusDamage;
    [SerializeField] private int maxMinusDamage;

    public int GetMinusDamage()
    {
        return Random.Range(minMinusDamage, maxMinusDamage + 1);
    }   
}
