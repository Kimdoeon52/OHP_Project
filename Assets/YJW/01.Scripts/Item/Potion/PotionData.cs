using System.ComponentModel;
using UnityEngine;

public enum PotionType
{
    HP,
    strength,
    sight,
    experience,
    fire,
    ice
}

[CreateAssetMenu(fileName = "NewPotion", menuName = "Item/Potion/NewPotion")]
public class PotionData : ItemBase
{
    [SerializeField] private PotionType potionType;
    [SerializeField] private int value;

    public override void Use()
    {
        switch(potionType)
        {
            case PotionType.HP:
                PlayerStats.Instance.AddStat(StatType.Hp, value);
                break;
            case PotionType.strength:
                PlayerStats.Instance.AddStat(StatType.Strength, value);
                break;
            case PotionType.sight:
                // 맵 시야 밝힘
                break;
            case PotionType.experience:
                PlayerStats.Instance.AddStat(StatType.Level, value);
                break;
        }
    }
}
