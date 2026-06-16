using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class RingInstance : EquipmentInstance
{
	public RingData Ring => data as RingData;
    public int enhanceValue => enhanceLevel * Ring.Value;
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

        return $"[{Ring.Name}] {(enhanceLevel > 0 ? $"<color=yellow>+{enhanceLevel}</color>" : "")}\n" +
               $"--------------------\n" +
               $"{Ring.Description}\n" +
               $"착용 힘: {Ring.CanUseStrength}\n" +
               $"효과: {Ring.EffectDescription} {(Ring.Value >= 0 ? "+" : "")}{Ring.Value}";
    }

}
