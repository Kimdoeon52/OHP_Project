using UnityEngine;
public enum RingType
{
    Fast,
    Hit,
    Strength,
    Snipe,
    Avoidance,
	Accuracy
}

[CreateAssetMenu(fileName = "New Ring", menuName = "Item/Ring/New Ring")]
public class RingData : ItemBase
{
	[SerializeField] private int canUseStrength;
    [SerializeField] private int value;
    [SerializeField] private string effectDescription;
    public RingType ringType;


	public int CanUseStrength => canUseStrength;
    public int Value => value;
	public string EffectDescription => effectDescription;
	public string LocalizedEffect => LocalizationManager.GetOrDefault($"item_effect_{Id}", effectDescription);

	private void OnEquip()
    {
        switch(ringType)
        {
            case RingType.Fast:
                NYHPlayerStats.Instance.AddStat(NYHStatType.MoveSpeed, value);
                break;
			case RingType.Hit:
				NYHPlayerStats.Instance.AddStat(NYHStatType.AttackSpeed, value);
				break;
			case RingType.Strength:
				NYHPlayerStats.Instance.AddStat(NYHStatType.Strength, value);
				break;
			case RingType.Snipe:
				// 투척 무기 강화 + 1
				break;
			case RingType.Avoidance:
				NYHPlayerStats.Instance.AddStat(NYHStatType.Evasion, value);
				break;
			case RingType.Accuracy:
				NYHPlayerStats.Instance.AddStat(NYHStatType.Accuracy, value);
				break;
		}
    }
    public override ItemInstance CreateInstance()
    {
        // 강화 주문서가 enhanceLevel을 올릴 수 있도록 반지도 장비 인스턴스로 생성한다.
        return new RingInstance { data = this };
    }

    public override string GetToolTipStats()
    {
        return $"[{LocalizedName}]  <color=red>{LocalizationManager.GetOrDefault("msg_미감정", "Unidentified")}</color>\n" +
               $"--------------------\n" +
               $"{LocalizedDescription}\n" +
               $"{LocalizationManager.GetOrDefault("msg_착용힘", "STR Required")}: {CanUseStrength}\n" +
               $"{LocalizationManager.GetOrDefault("msg_효과", "Effect")}: {LocalizedEffect} +{value}";
    }
}
