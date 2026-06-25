using System.ComponentModel;
using UnityEngine;
using System.Collections;

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
    private const float MoveSpeedPotionDurationSeconds = 10f;

    private static NYHPlayerStats activeMoveSpeedPotionStats;
    private static Coroutine activeMoveSpeedPotionCoroutine;
    private static int activeMoveSpeedPotionAmount;

    [SerializeField] private PotionType potionType;
    [SerializeField] private int value;

    public override void Use()
    {
        if(NYHPlayerStats.Instance == null)
        {
            Debug.LogWarning("PotionData: NYHPlayerStats가 씬에 없어 포션 효과를 적용하지 못했습니다.");
            return;
        }

        bool used = false;
        switch(potionType)
        {
            case PotionType.HP:
                if(GridPlayerHealth.Instance.CurrentHp >= GridPlayerHealth.Instance.MaxHp) return;
                // 회복 봉인 챌린지 — 회복 효과 0 (포션은 소모됨)
                if(!ChallengeState.Has(ChallengeType.HealSeal))
                    HealPlayer(value); //체력 증가 수정
                used = true;
                break;
            case PotionType.strength:
                AddNYHStat(NYHStatType.Strength);
                used = true;
                break;
            case PotionType.sight:
                // 맵 시야 밝힘
                used = true;
                break;
            case PotionType.experience:
                AddPlayerExp(value); 
                used = true;
                break;
            case PotionType.moveSpeed:
                ApplyTemporaryMoveSpeed(value, MoveSpeedPotionDurationSeconds);
                used = true;
                break;
            case PotionType.levitation:
                ApplyPlayerStatus(StatusType.Float, value);
                used = true;
                break;
            case PotionType.purify:
                PurifyPlayer();
                used = true;
                break;
        }

        if (used)
            GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.PotionDrink);
    }

    public override ItemInstance CreateInstance()
    {
        return new PotionInstance { data = this, count = 1 };
    }

    public override string GetToolTipStats()
    {
        string template = LocalizationManager.GetOrDefault($"item_potionEffect_{potionType}", LocalizedDescription);
        string effect = string.Format(template, value);

        return $"[{LocalizedName}]\n" +
               $"─────────────\n" +
               $"{LocalizedDescription}\n" +
               $"\n" +
               $"{LocalizationManager.GetOrDefault("msg_효과", "Effect")}   {effect}";
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

    private void ApplyTemporaryMoveSpeed(int amount, float durationSeconds)
    {
        NYHPlayerStats stats = NYHPlayerStats.Instance;
        if (stats == null)
        {
            Debug.LogWarning("PotionData: NYHPlayerStats가 씬에 없어 이동속도 포션 효과를 적용하지 못했습니다.");
            return;
        }

        if (amount <= 0)
            return;

        RefreshMoveSpeedPotion(stats, amount, durationSeconds);
    }

    private static void RefreshMoveSpeedPotion(NYHPlayerStats stats, int amount, float durationSeconds)
    {
        if (activeMoveSpeedPotionStats != null && activeMoveSpeedPotionStats != stats)
        {
            if (activeMoveSpeedPotionCoroutine != null)
                activeMoveSpeedPotionStats.StopCoroutine(activeMoveSpeedPotionCoroutine);

            activeMoveSpeedPotionStats.AddStat(NYHStatType.MoveSpeed, -activeMoveSpeedPotionAmount);
            activeMoveSpeedPotionStats = null;
            activeMoveSpeedPotionAmount = 0;
            activeMoveSpeedPotionCoroutine = null;
        }

        if (activeMoveSpeedPotionStats == stats && activeMoveSpeedPotionCoroutine != null)
            stats.StopCoroutine(activeMoveSpeedPotionCoroutine);

        // 이미 같은 버프가 켜져 있으면 중첩하지 않고 차이만 반영한 뒤 지속시간만 갱신합니다.
        int delta = activeMoveSpeedPotionStats == stats
            ? amount - activeMoveSpeedPotionAmount
            : amount;

        if (delta != 0)
            stats.AddStat(NYHStatType.MoveSpeed, delta);

        activeMoveSpeedPotionStats = stats;
        activeMoveSpeedPotionAmount = amount;
        activeMoveSpeedPotionCoroutine = stats.StartCoroutine(RemoveMoveSpeedAfterDuration(stats, amount, durationSeconds));
    }

    private static IEnumerator RemoveMoveSpeedAfterDuration(NYHPlayerStats stats, int expectedAmount, float durationSeconds)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, durationSeconds);

        while (elapsed < duration)
        {
            // Safe Room처럼 타이머가 멈춘 상태에서는 버프 시간도 소모하지 않습니다.
            if (TimeSystem.Instance == null || TimeSystem.Instance.IsRunning)
                elapsed += Time.deltaTime;

            yield return null;
        }

        if (stats != null && activeMoveSpeedPotionStats == stats && activeMoveSpeedPotionAmount == expectedAmount)
        {
            stats.AddStat(NYHStatType.MoveSpeed, -activeMoveSpeedPotionAmount);
            activeMoveSpeedPotionStats = null;
            activeMoveSpeedPotionCoroutine = null;
            activeMoveSpeedPotionAmount = 0;
        }
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
