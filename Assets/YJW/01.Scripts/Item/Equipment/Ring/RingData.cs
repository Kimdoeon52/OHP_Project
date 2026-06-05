using UnityEngine;

enum RingType
{
    Fast,
    Blow,
    Power,
    Snipe,
    Avoidance,
    Accuracy
}

[CreateAssetMenu(fileName = "New Ring", menuName = "Item/Ring/New Ring")]
public class RingData : ItemBase
{
    [SerializeField] private RingType ringType;
    [SerializeField] private float value;

    public void Apply(float mul)
    {
        switch(ringType)
        {
            case RingType.Fast:
                NYHPlayerStats.Instance.AddStat(NYHStatType.MoveSpeed, value * mul);
                break;
            case RingType.Blow:
                NYHPlayerStats.Instance.AddStat(NYHStatType.AttackSpeed, value * mul);
                break;
            case RingType.Power:
                NYHPlayerStats.Instance.AddTempStat(NYHStatType.Strength, mul);
                break;
            case RingType.Snipe:
                NYHPlayerStats.Instance.AddTempStat(NYHStatType.CriticalChance, mul);
                break;
            case RingType.Avoidance:
                NYHPlayerStats.Instance.AddTempStat(NYHStatType.Evasion, mul);
                break;
            case RingType.Accuracy:
                NYHPlayerStats.Instance.AddTempStat(NYHStatType.Accuracy, mul);
                break;
        }
    }
}
