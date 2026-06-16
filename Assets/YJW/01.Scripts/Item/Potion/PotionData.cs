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
    moveSpeed,
    levitation,
    purify
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
                if(GridPlayerHealth.Instance.CurrentHp >= GridPlayerHealth.Instance.MaxHp) return;
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
            case PotionType.levitation:
                ApplyPlayerStatus(StatusType.Float, value);
                break;
            case PotionType.purify:
                PurifyPlayer();
                break;
        }
    }

	public override ItemInstance CreateInstance()
	{
		return new PotionInstance { data = this, count = 1 };
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

    // 플레이어에게 지정 상태이상을 적용한다. value 필드를 지속 시간(초)으로 사용
    private void ApplyPlayerStatus(StatusType type, int durationSeconds)
    {
        StatusController status = GetPlayerStatusController();
        if (status == null)
        {
            Debug.LogWarning("PotionData: StatusController를 찾지 못해 포션 효과를 적용하지 못했습니다.");
            return;
        }
        status.ApplyStatus(new StatusRequest(null, Vector2Int.zero, type, durationSeconds, 1, null));
    }

    // 정화 포션: 독·둔화 제거 후 10초간 면역
    private void PurifyPlayer()
    {
        StatusController status = GetPlayerStatusController();
        if (status == null)
        {
            Debug.LogWarning("PotionData: StatusController를 찾지 못해 정화 포션 효과를 적용하지 못했습니다.");
            return;
        }
        status.TryRemoveStatus(StatusType.Poison);
        status.TryRemoveStatus(StatusType.Slow);
        status.TryRemoveStatus(StatusType.Cripple);
        status.ApplyStatus(new StatusRequest(null, Vector2Int.zero, StatusType.Purified, value, 1, null));
    }

    private static StatusController GetPlayerStatusController()
    {
        if (GameManager.Instance?.player != null)
            return GameManager.Instance.player.GetComponent<StatusController>();
        return null;
    }
}
