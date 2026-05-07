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

public class PotionData : ItemBase
{
    [SerializeField] private PotionType potionType;
    [SerializeField] private int value;

    public override void Use()
    {
        switch(potionType)
        {
            case PotionType.HP:
                // 플레이어 HP 회복
                break;
            case PotionType.strength:
                // 플레이어 힘 증가
                break;
            case PotionType.sight:
                // 맵 시야 밝힘
                break;
            case PotionType.experience:
                // 플레이어 경험치 증가
                break;
        }
    }
}
