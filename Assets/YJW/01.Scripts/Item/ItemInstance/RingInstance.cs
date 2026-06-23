using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class RingInstance : EquipmentInstance
{
	public RingData Ring => data as RingData;
    // +0강이어도 기본 효과(1배)는 적용되고, 강화 단계마다 1배씩 더 붙는다.
    public int enhanceValue => Ring.Value * (enhanceLevel + 1);

    // 강화 단계마다 착용 힘이 1씩 줄어들며, 0 밑으로는 내려가지 않는다.
    public int GetRequiredStrength() => Mathf.Max(0, Ring.CanUseStrength - enhanceLevel);
    public void OnEquip()
    {
        switch (Ring.ringType)
        {
            case RingType.Fast:
                NYHPlayerStats.Instance.AddStat(NYHStatType.MoveSpeed, enhanceValue);
                break;
            case RingType.Hit:
                NYHPlayerStats.Instance.AddStat(NYHStatType.AttackSpeed, enhanceValue);
                break;
            case RingType.Strength:
                NYHPlayerStats.Instance.AddStat(NYHStatType.Strength, enhanceValue);
                break;
            case RingType.Snipe:
                ThrowableWeaponInstance.snipeBonus++;
                break;
            case RingType.Avoidance:
                NYHPlayerStats.Instance.AddStat(NYHStatType.Evasion, enhanceValue);
                break;
            case RingType.Accuracy:
                NYHPlayerStats.Instance.AddStat(NYHStatType.Accuracy, enhanceValue);
                break;
        }
    }

    public void OnUnequip()
    {
        switch (Ring.ringType)
        {
            case RingType.Fast:
                NYHPlayerStats.Instance.AddStat(NYHStatType.MoveSpeed, -enhanceValue);
                break;
            case RingType.Hit:
                NYHPlayerStats.Instance.AddStat(NYHStatType.AttackSpeed, -enhanceValue);
                break;
            case RingType.Strength:
                NYHPlayerStats.Instance.AddStat(NYHStatType.Strength, -enhanceValue);
                break;
            case RingType.Snipe:
                ThrowableWeaponInstance.snipeBonus--;
                break;
            case RingType.Avoidance:
                NYHPlayerStats.Instance.AddStat(NYHStatType.Evasion, -enhanceValue);
                break;
            case RingType.Accuracy:
                NYHPlayerStats.Instance.AddStat(NYHStatType.Accuracy, -enhanceValue);
                break;
        }
    }



    public override string GetToolTipStats()
    {
        if (!isAnalyzed)
            return Ring.GetToolTipStats();

        return $"[{Ring.LocalizedName}] {(enhanceLevel > 0 ? $"<color=yellow>+{enhanceLevel}</color>" : "")}\n" +
               $"--------------------\n" +
               $"{Ring.LocalizedDescription}\n" +
               $"{LocalizationManager.GetOrDefault("msg_착용힘", "STR Required")}: {GetRequiredStrength()}\n" +
               $"{LocalizationManager.GetOrDefault("msg_효과", "Effect")}: {Ring.LocalizedEffect} {(enhanceValue >= 0 ? "+" : "")}{enhanceValue}";
    }

}
