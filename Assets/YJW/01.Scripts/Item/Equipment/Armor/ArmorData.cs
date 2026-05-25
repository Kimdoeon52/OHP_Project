using UnityEngine;

[CreateAssetMenu(fileName = "ArmorData", menuName = "Item/Equipment/Armor/Armor Data")]
public class ArmorData : ItemBase
{
    [SerializeField] private int minMinusDamage;
    [SerializeField] private int maxMinusDamage;
    [SerializeField] private int canUseStrength;

    public int GetMinusDamage()
    {
        return Random.Range(minMinusDamage, maxMinusDamage + 1);
    }

    public override string GetToolTipStats()
    {
        return $"{itemName}\n" +
            "----------\n" +
            $"{description}\n" +
            $"착용 힘: {canUseStrength}\n" +
            $"피해 감소: {minMinusDamage}~{maxMinusDamage}";
    }
}
