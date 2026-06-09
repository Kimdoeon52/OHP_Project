using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class RingInstance : EquipmentInstance
{
	public RingData Weapon => data as RingData;
    public int enhanceValue => enhanceLevel * Weapon.Value;
    private void OnEquip()
    {
        switch (Weapon.ringType)
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
                // 투척 무기 강화 + 1
                break;
            case RingType.Avoidance:
                NYHPlayerStats.Instance.AddStat(NYHStatType.Evasion, enhanceValue);
                break;
            case RingType.Accuracy:
                NYHPlayerStats.Instance.AddStat(NYHStatType.Accuracy, enhanceValue);
                break;
        }
    }



    public override string GetToolTipStats() =>
		$"{Weapon.Name}\n" +
		$"----------\n" +
		$"{Weapon.Description}\n" +
		$"착용 힘: {Weapon.CanUseStrength}\n" +
		$"효과: {Weapon.EffectDescription} {(Weapon.Value >= 0 ? "+" : "")}{Weapon.Value}";

}
