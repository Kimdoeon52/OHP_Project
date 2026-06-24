using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class RingInstance : EquipmentInstance
{
	public RingData Ring => data as RingData;
    // +0강이어도 기본 효과(1배)는 적용되고, 강화 단계마다 1배씩 더 붙는다.
    public int enhanceValue => Ring.Value * (enhanceLevel + 1);

    // OnEquip()에서 실제로 적용된 수치 — OnUnequip/Refresh에서 정확히 같은 값을 빼기 위해 기억
    private int _appliedEnhanceValue;

    // 강화 단계마다 착용 힘이 1씩 줄어들며, 0 밑으로는 내려가지 않는다.
    public int GetRequiredStrength() => Mathf.Max(0, Ring.CanUseStrength - enhanceLevel);

    public void OnEquip()
    {
        _appliedEnhanceValue = enhanceValue;
        ApplyStatDelta(enhanceValue);
    }

    public void OnUnequip()
    {
        ApplyStatDelta(-_appliedEnhanceValue);
        _appliedEnhanceValue = 0;
    }

    // 장착 중 enhanceLevel이 변경됐을 때(강화 주문서 등) 스탯 델타만 반영
    public void RefreshEquippedStat()
    {
        int delta = enhanceValue - _appliedEnhanceValue;
        if (delta == 0)
            return;
        ApplyStatDelta(delta);
        _appliedEnhanceValue = enhanceValue;
    }

    private void ApplyStatDelta(int delta)
    {
        switch (Ring.ringType)
        {
            case RingType.Fast:
                NYHPlayerStats.Instance.AddStat(NYHStatType.MoveSpeed, delta);
                break;
            case RingType.Hit:
                NYHPlayerStats.Instance.AddStat(NYHStatType.AttackSpeed, delta);
                break;
            case RingType.Strength:
                NYHPlayerStats.Instance.AddStat(NYHStatType.Strength, delta);
                break;
            case RingType.Snipe:
                if (delta > 0) ThrowableWeaponInstance.snipeBonus++;
                else if (delta < 0) ThrowableWeaponInstance.snipeBonus--;
                break;
            case RingType.Avoidance:
                NYHPlayerStats.Instance.AddStat(NYHStatType.Evasion, delta);
                break;
            case RingType.Accuracy:
                NYHPlayerStats.Instance.AddStat(NYHStatType.Accuracy, delta);
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
