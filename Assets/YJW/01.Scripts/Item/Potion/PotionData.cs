using System.ComponentModel;
using UnityEngine;

public enum PotionType
{
    HP,
    strength,
    sight,
    experience,
    fire,
    ice,
    moveSpeed
}

[CreateAssetMenu(fileName = "NewPotion", menuName = "Item/Potion/NewPotion")]
public class PotionData : ItemBase
{
    [SerializeField] private PotionType potionType;
    [SerializeField] private int value;

    public override void Use()
    {
        if(NYHPlayerStats.Instance == null)
        {
            Debug.LogWarning("PotionData: NYHPlayerStats가 씬에 없어 포션 효과를 적용하지 못했습니다.");
            return;
        }
        switch(potionType)
        {
            case PotionType.HP: 
                if(NYHPlayerStats.Instance.HP >= NYHPlayerStats.Instance.MaxHP) return;
                HealPlayer(value); //체력 증가 수정
                break;
            case PotionType.strength:
                AddNYHStat(NYHStatType.Strength);
                break;
            case PotionType.sight:
                // 맵 시야 밝힘
                break;
            case PotionType.experience:
                AddPlayerExp(value); 
                break;
            case PotionType.moveSpeed:
                AddNYHStat(NYHStatType.MoveSpeed);
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

    //플레이어의 경험치를 증가시켜주는 함수 경험치는
    //PlayerLevelSystem의 별도 시스템을 사용중이라 스탯과는 분리되어 잇음
    private void AddPlayerExp(int amount)
    {
        if(PlayerLevelSystem.Instance == null)
        {
            Debug.LogWarning("PotionData: PlayerLevelSystem이 씬에 없어 포션 효과를 적용하지 못했습니다.");
            return;
        }
        PlayerLevelSystem.Instance.AddExp(amount);
    }

    //플레이어의 현재 체력은 statType이 아닌 GridPlayerHealth 에서 관리하고 있어서 변경함
    private void HealPlayer(int amount)
    {
        GridPlayerHealth health = FindFirstObjectByType<GridPlayerHealth>();
        if (health == null)
        {
            Debug.LogWarning("PotionData: GridPlayerHealth가 씬에 없어 체력 포션 효과를 적용하지 못했습니다.");
            return;
        }
        health.Heal(amount);
    }
}
