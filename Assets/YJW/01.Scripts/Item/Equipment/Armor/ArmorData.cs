using UnityEngine;

[CreateAssetMenu(fileName = "ArmorData", menuName = "Item/Equipment/Armor/Armor Data")]
public class ArmorData : ItemBase
{
    [SerializeField] private int minMinusDamage;
    [SerializeField] private int maxMinusDamage;
    [SerializeField] protected int CanUseStrength;

    public int GetMinusDamage()
    {
        return Random.Range(minMinusDamage, maxMinusDamage + 1);
    }
}
