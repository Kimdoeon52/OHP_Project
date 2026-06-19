using UnityEngine;

public enum MagicType
{
    EmptyStep,
    LastSprint,
    SecondOfGreed,
    BloodClock,
    DrainLife,
    Execution,
    Focus,
    Chase
}

[CreateAssetMenu(fileName = "New Magic", menuName = "Item/Magic")]
public class MagicData : ScriptableObject
{
	[SerializeField] private string magicName;
	[SerializeField] private string description;
	[SerializeField] private MagicType magicType;

	public string MagicName => magicName;
	public string Description => description;
	public MagicType Type => magicType;
	// 마법 이름/설명은 MagicType 1:1 매핑이라 enum 이름을 그대로 키로 쓴다.
	public string LocalizedName => LocalizationManager.GetOrDefault($"item_name_magic_{magicType}", magicName);
	public string LocalizedDescription => LocalizationManager.GetOrDefault($"item_desc_magic_{magicType}", description);

	private bool _focusGuaranteed = false;
    private float _chaseEndTime = -1f;

    // 공허 발걸음 패시브
    public bool IgnoresHazardSlow => magicType == MagicType.EmptyStep;
    public bool HalvesHazardDamage => magicType == MagicType.EmptyStep;

    // 탐욕의 초침 패시브
    public bool OverridesPickupTime => magicType == MagicType.SecondOfGreed;

    // 집중: 다음 공격 보장 여부
    public bool IsHitGuaranteed => magicType == MagicType.Focus && _focusGuaranteed;

    // 마지막 질주 / 추격: 이동 쿨타임 보너스
    public float GetMoveCooldownBonus()
    {
        float bonus = 0f;
        if (magicType == MagicType.LastSprint
            && TimeSystem.Instance != null
            && TimeSystem.Instance.TimeRemaining <= 10f)
        {
            bonus -= 0.2f;
        }
        if (magicType == MagicType.Chase && Time.time < _chaseEndTime)
        {
            bonus -= 0.2f;
        }
        return bonus;
    }

    // 갑옷: 피해를 받을 때
    public void OnHit()
    {
        if (magicType != MagicType.BloodClock)
            return;
        if (Random.value < 0.25f)
            TimeSystem.Instance?.AddTime(1f);
    }

    // 무기: 공격이 적중했을 때
    public void OnAttack(ref int damage, EnemyBase enemy, int maxWeaponDamage)
    {
        switch (magicType)
        {
            case MagicType.DrainLife:
                if (Random.value < 0.30f)
                {
                    int heal = Mathf.Max(1, Mathf.RoundToInt(damage * 0.2f));
                    PlayerStats.Instance?.AddStat(StatType.Hp, heal);
                }
                break;
            case MagicType.Execution:
                if (enemy != null && !enemy.IsDead && enemy.CurrentHp <= maxWeaponDamage / 2)
                    damage = enemy.CurrentHp * 100 + 9999;
                break;
            case MagicType.Focus:
                _focusGuaranteed = false;
                break;
        }
    }

    // 무기: 공격이 빗나갔을 때
    public void OnMiss()
    {
        if (magicType == MagicType.Focus)
            _focusGuaranteed = true;
    }

    // 무기: 적을 처치했을 때
    public void OnKill()
    {
        if (magicType == MagicType.Chase)
            _chaseEndTime = Time.time + 3f;
    }
}