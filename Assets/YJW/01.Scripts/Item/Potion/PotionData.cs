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
                AddNYHStat(NYHStatType.Health);
                break;
            case PotionType.strength:
                AddNYHStat(NYHStatType.Strength);
                break;
            case PotionType.sight:
                // 맵 시야 밝힘
                break;
            case PotionType.experience:
                AddNYHStat(NYHStatType.Level);
                break;
        }
    }

    private void AddNYHStat(NYHStatType type)
    {
        if (NYHPlayerStats.Instance == null)
        {
            Debug.LogWarning("PotionData: NYHPlayerStats가 씬에 없어 포션 효과를 적용하지 못했습니다.");
            return;
        }

        NYHPlayerStats.Instance.AddStat(type, value);
    }
}
