using UnityEngine;

[CreateAssetMenu(fileName = "ArmorData", menuName = "Item/Equipment/Armor/Armor Data")]
public class ArmorData : ItemBase
{
    [SerializeField] private int minMinusDamage;
    [SerializeField] private int maxMinusDamage;
    [SerializeField] private int canUseStrength;
    [SerializeField] public int grade;
    [SerializeField] public bool isAnalyzed;

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
