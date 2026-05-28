using System;
using UnityEngine;

public enum NYHStatType
{
    // 최대 체력. 레벨업/장비/보상으로 영구 증가할 수 있다.
    Health,

    // 근접 공격, 투척 무기 등 플레이어가 주는 기본 피해량 보정.
    Strength,

    // 격자 이동 속도. GridPlayerMovement의 이동 시간/후딜 계산에 사용된다.
    MoveSpeed,

    // 공격 애니메이션 속도와 공격 관련 시간 계산에 사용된다.
    AttackSpeed,

    // 받는 피해를 고정값으로 감소시키는 방어 스탯.
    Defense,

    // 피격을 확률로 완전히 무시하는 회피율. 0~100%.
    Evasion,

    // 이전 데이터 호환용. 실제 레벨/경험치는 PlayerLevelSystem에서 관리한다.
    Level
}

/// <summary>
/// 플레이어의 영구/기본 스탯을 관리한다.
/// 레벨과 경험치 진행도는 PlayerLevelSystem, 독/화상/둔화 같은 임시 상태이상은 StatusController가 담당한다.
/// </summary>
public class NYHPlayerStats : MonoBehaviour
{
    public static NYHPlayerStats Instance { get; private set; }

    [Header("기본 스탯")]
    [Tooltip("플레이어 최대 체력입니다. GridPlayerHealth의 MaxHp 계산에 사용됩니다.")]
    [SerializeField, Min(1)] private int maxHealth = 20;

    [Tooltip("플레이어의 기본 힘 입니다.일부 무기 장착 계산에 사용됩니다.")]
    [SerializeField, Min(0)] private int strength = 10;

    [Tooltip("플레이어 이동 속도입니다. 값이 높을수록 한 칸 이동 시간이 짧아집니다.")]
    [SerializeField, Min(0)] private int moveSpeed = 1;

    [Tooltip("플레이어 공격 속도입니다. 값이 높을수록 공격 애니메이션 속도가 빨라집니다.")]
    [SerializeField, Min(0)] private int attackSpeed = 1;

    [Tooltip("플레이어 명중률 보정값입니다. 현재는 확장용 스탯이며, 명중 판정 시스템에서 사용할 수 있습니다.")]
    [SerializeField, Range(0, 100)] private int accuracy = 15;

    [Header("방어 스탯")]
    [Tooltip("받는 피해를 고정값으로 감소시키는 방어력입니다. 최종 피해는 최소 0까지 내려갑니다.")]
    [SerializeField, Min(0)] private int defense;

    [Tooltip("피격을 완전히 회피할 확률입니다. 0이면 회피 없음, 100이면 항상 회피합니다.")]
    [SerializeField, Range(0, 100)] private int evasion = 20;

    /// <summary>
    /// 스탯 값이 변경됐을 때 호출된다.
    /// UI, 장비 조건 갱신, 파생 능력치 갱신 등에 사용한다.
    /// </summary>
    public event Action OnStatsChanged;

    /// <summary>
    /// 특정 스탯이 증가/감소했을 때 호출된다.
    /// GridPlayerHealth는 Health 증가 이벤트를 받아 최대 체력 증가분만큼 회복한다.
    /// </summary>
    public event Action<NYHStatType, int> OnStatAdded;

    public int MaxHealth => maxHealth;
    public int Strength => strength;
    public int MoveSpeed => moveSpeed;
    public int AttackSpeed => attackSpeed;
    public int Accuracy => accuracy;
    public int Defense => defense;
    public int Evasion => evasion;

    /// <summary>
    /// 씬에서 전역 접근할 플레이어 스탯 인스턴스를 등록한다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NYHPlayerStats: 씬에 스탯 컴포넌트가 둘 이상 있습니다. 먼저 등록된 값을 사용합니다.");
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 현재 인스턴스가 파괴될 때 싱글톤 참조를 정리한다.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 지정한 스탯을 amount만큼 변경한다.
    /// Health는 최대 체력을 변경하고, 실제 현재 체력 반영은 OnStatAdded 구독자가 처리한다.
    /// </summary>
    public void AddStat(NYHStatType type, int amount)
    {
        switch (type)
        {
            case NYHStatType.Health:
                maxHealth = Mathf.Max(1, maxHealth + amount);
                OnStatAdded?.Invoke(type, amount);
                OnStatsChanged?.Invoke();
                return;
            case NYHStatType.Strength:
                strength = Mathf.Max(0, strength + amount);
                break;
            case NYHStatType.MoveSpeed:
                moveSpeed = Mathf.Max(0, moveSpeed + amount);
                break;
            case NYHStatType.AttackSpeed:
                attackSpeed = Mathf.Max(0, attackSpeed + amount);
                break;
            case NYHStatType.Defense:
                defense = Mathf.Max(0, defense + amount);
                break;
            case NYHStatType.Evasion:
                evasion = Mathf.Clamp(evasion + amount, 0, 100);
                break;
            case NYHStatType.Level:
                Debug.LogWarning("NYHPlayerStats: Level은 PlayerLevelSystem에서 관리합니다.");
                return;
        }

        OnStatAdded?.Invoke(type, amount);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 방어력과 회피율을 적용해 최종 받는 피해량을 계산한다.
    /// 회피가 성공하면 0, 실패하면 방어력만큼 감소한 값을 반환한다.
    /// </summary>
    public int CalculateIncomingDamage(int rawDamage)
    {
        if (rawDamage <= 0) return 0;
        if (evasion > 0 && UnityEngine.Random.value < evasion / 100f) return 0;

        return Mathf.Max(0, rawDamage - defense);
    }

    /// <summary>
    /// 인스펙터에서 잘못된 값이 들어가지 않도록 범위를 보정한다.
    /// </summary>
    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        strength = Mathf.Max(0, strength);
        moveSpeed = Mathf.Max(0, moveSpeed);
        attackSpeed = Mathf.Max(0, attackSpeed);
        accuracy = Mathf.Clamp(accuracy, 0, 100);
        defense = Mathf.Max(0, defense);
        evasion = Mathf.Clamp(evasion, 0, 100);
    }
}

public readonly struct NYHAttackDamageRange
{
    public readonly int MinDamage;
    public readonly int MaxDamage;
    public readonly int BonusMax;
    public readonly bool IsUnarmed;
    public readonly string WeaponName;

    public NYHAttackDamageRange(int minDamage, int maxDamage, int bonusMax, bool isUnarmed, string weaponName)
    {
        MinDamage = Mathf.Max(0, minDamage);
        MaxDamage = Mathf.Max(MinDamage, maxDamage);
        BonusMax = Mathf.Max(0, bonusMax);
        IsUnarmed = isUnarmed;
        WeaponName = weaponName;
    }

    public string ToDisplayText()
    {
        if (MinDamage == MaxDamage)
            return MinDamage.ToString();

        return BonusMax > 0
            ? $"{MinDamage}~{MaxDamage} (+0~{BonusMax})"
            : $"{MinDamage}~{MaxDamage}";
    }
}

public static class NYHPlayerAttackCalculator
{
    private const int UnarmedDamage = 1;

    public static NYHAttackDamageRange CalculateMeleeRange(NYHPlayerStats stats, NYHEquipmentController equipmentController)
    {
        if (stats == null)
            return new NYHAttackDamageRange(UnarmedDamage, UnarmedDamage, 0, true, "맨손");

        CloseWeapon weapon = GetEquippedCloseWeapon(equipmentController);
        if (weapon == null)
            return new NYHAttackDamageRange(UnarmedDamage, UnarmedDamage, 0, true, "맨손");

        int strengthBonusMax = Mathf.Max(0, stats.Strength - weapon.CanUseStrength);
        int minDamage = Mathf.Max(0, weapon.MinDamage);
        int maxDamage = Mathf.Max(minDamage, weapon.MaxDamage + strengthBonusMax);

        return new NYHAttackDamageRange(minDamage, maxDamage, strengthBonusMax, false, weapon.Name);
    }

    public static int RollMeleeDamage(NYHPlayerStats stats, NYHEquipmentController equipmentController)
    {
        NYHAttackDamageRange range = CalculateMeleeRange(stats, equipmentController);
        if (range.MinDamage == range.MaxDamage)
            return range.MinDamage;

        // UnityEngine.Random.Range(int, int)는 maxExclusive라서 +1 한다.
        return UnityEngine.Random.Range(range.MinDamage, range.MaxDamage + 1);
    }

    private static CloseWeapon GetEquippedCloseWeapon(NYHEquipmentController equipmentController)
    {
        if (equipmentController == null)
            return null;

        return equipmentController.TryGetEquipped(EquipmentSlot.Weapon, out NYHEquipmentItemInstance item)
            ? item.Data as CloseWeapon
            : null;
    }
}
