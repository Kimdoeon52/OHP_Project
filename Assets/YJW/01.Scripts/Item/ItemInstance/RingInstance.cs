using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class RingInstance : EquipmentInstance
{
	public RingData Ring => data as RingData;
    // +0강이어도 기본 효과(1배)는 적용되고, 강화 단계마다 1배씩 더 붙는다.
    public int enhanceValue => Ring.Value * (enhanceLevel + 1);

    // 악세사리 중독 특성 배율을 반영한 효율값(반올림 전). 실제 스탯 적용 시점에 int로 반올림한다.
    public float EffectiveEnhanceValue
    {
        get
        {
            TraitController trait = TraitController.FindAvailableInstance();
            float mult = trait != null ? trait.GetAccessoryEffectMultiplier() : 1f;
            return enhanceValue * mult;
        }
    }

    // 효율값을 실제 int 스탯으로 반올림한다 (0.5는 올림 — Mathf.RoundToInt 짝수 붙음 회피).
    private int AppliedStatValue => Mathf.FloorToInt(EffectiveEnhanceValue + 0.5f);

    // OnEquip()에서 실제로 적용된 수치 — OnUnequip/Refresh에서 정확히 같은 값을 빼기 위해 기억
    private int _appliedEnhanceValue;

    // 강화 단계마다 착용 힘이 1씩 줄어들며, 0 밑으로는 내려가지 않는다.
    public int GetRequiredStrength() => Mathf.Max(0, Ring.CanUseStrength - enhanceLevel);

    public void OnEquip()
    {
        _appliedEnhanceValue = AppliedStatValue;
        ApplyStatDelta(_appliedEnhanceValue);
    }

    public void OnUnequip()
    {
        ApplyStatDelta(-_appliedEnhanceValue);
        _appliedEnhanceValue = 0;
    }

    // 장착 중 enhanceLevel/특성 배율이 변경됐을 때(강화 주문서, 악세사리 중독 등) 스탯 델타만 반영
    public void RefreshEquippedStat()
    {
        int target = AppliedStatValue;
        int delta = target - _appliedEnhanceValue;
        if (delta == 0)
            return;
        ApplyStatDelta(delta);
        _appliedEnhanceValue = target;
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
