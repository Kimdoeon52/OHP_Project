using UnityEngine;

public enum TrapType
{
    Spike,
    Alarm,
    WornDart,
    PoisonDart,
    Disintegration,
    Gateway,
    Chilling,
    Frost,
    Burning,
    Blazing,
    Shocking,
    Storm,
    Guardian,
    Gripping,
    Flashing,
    Teleportation,
    Warping,
    Ooze,
    ConfusionGas,
    ToxicGas,
    CorrosiveGas,
    Flock,
    Summoning,
    Weakening,
    Cursing,
    Geyser,
    Explosive,
    Rockfall,
    Pitfall,
    Distortion,
    Disarming,
    Grim
}

public enum TrapDamageMode
{
    FlatRange,
    CurrentHpRatio,
    MaxHpMinimum,
    Grim
}

[CreateAssetMenu(fileName = "TrapData", menuName = "60s Dungeon/Trap Data")]
public class TrapData : ScriptableObject
{
    [Header("기본 정보")]
    public string id;
    public string displayName;
    public TrapType trapType;

    [Header("등장/공개")]
    [Min(1)] public int floorMin = 1;
    [Min(1)] public int floorMax = 24;
    public bool startsHidden;
    public bool isAlwaysVisible;
    [Range(0f, 1f)] public float revealChance = 0.3f;

    [Header("발동")]
    public bool repeatable;
    public bool destroyAfterTrigger = true;

    [Header("공통 수치")]
    public TrapDamageMode damageMode = TrapDamageMode.FlatRange;
    public int damageMin = 10;
    public int damageMax = 10;
    [Range(0f, 2f)] public float currentHpRatioMin = 0.5f;
    [Range(0f, 2f)] public float currentHpRatioMax = 0.67f;
    [Range(0f, 1f)] public float maxHpMinimumRatio = 0.2f;
    [Min(0)] public int radius;
    [Min(0)] public int durationTurns;
    public int statusPower;
    [Min(0)] public int delayTurns;

    [Header("호환/기존 수치")]
    public int damage = 10;
    public int alarmDuration = 10;

    [Header("표시")]
    public Sprite visibleSprite;
    public Sprite hiddenSprite;
    public Sprite triggeredSprite;

    public int RollDamage()
    {
        int min = Mathf.Min(damageMin, damageMax);
        int max = Mathf.Max(damageMin, damageMax);
        if (min == 0 && max == 0) return damage;
        return Random.Range(min, max + 1);
    }

    public int CalculateDamage(IDamageable target)
    {
        if (target == null) return RollDamage();

        switch (damageMode)
        {
            case TrapDamageMode.CurrentHpRatio:
                return Mathf.Max(1, Mathf.RoundToInt(target.CurrentHp * Random.Range(currentHpRatioMin, currentHpRatioMax)));
            case TrapDamageMode.MaxHpMinimum:
            {
                int ratioDamage = Mathf.RoundToInt(target.CurrentHp * Random.Range(currentHpRatioMin, currentHpRatioMax));
                int minimumDamage = Mathf.RoundToInt(target.MaxHp * maxHpMinimumRatio);
                return Mathf.Max(1, Mathf.Max(ratioDamage, minimumDamage));
            }
            case TrapDamageMode.Grim:
                if (target.CurrentHp >= Mathf.CeilToInt(target.MaxHp * 0.9f))
                    return Mathf.Max(0, target.CurrentHp - 1);
                return Mathf.Max(1, target.CurrentHp);
            default:
                return RollDamage();
        }
    }
}
