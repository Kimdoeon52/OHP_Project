using System;
using UnityEngine;

public enum NYHStatType
{
    Health,
    Strength,
    MoveSpeed,
    AttackSpeed,
    Defense,
    Evasion,
    Level
}

public class NYHPlayerStats : MonoBehaviour
{
    public static NYHPlayerStats Instance { get; private set; }

    [Header("기본 스탯")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0)] private int strength = 1;
    [SerializeField, Min(0)] private int moveSpeed = 1;
    [SerializeField, Min(0)] private int attackSpeed = 1;
    [SerializeField, Min(1)] private int level = 1;

    [Header("방어 스탯")]
    [SerializeField, Min(0)] private int defense;
    [SerializeField, Range(0, 100)] private int evasion;

    public event Action OnStatsChanged;
    public event Action<NYHStatType, int> OnStatAdded;

    public int MaxHealth => maxHealth;
    public int Strength => strength;
    public int MoveSpeed => moveSpeed;
    public int AttackSpeed => attackSpeed;
    public int Level => level;
    public int Defense => defense;
    public int Evasion => evasion;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NYHPlayerStats: 씬에 스탯 컴포넌트가 둘 이상 있습니다. 먼저 등록된 값을 사용합니다.");
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddStat(NYHStatType type, int amount)
    {
        switch (type)
        {
            case NYHStatType.Health:
                OnStatAdded?.Invoke(type, amount);
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
                level = Mathf.Max(1, level + amount);
                break;
        }

        OnStatAdded?.Invoke(type, amount);
        OnStatsChanged?.Invoke();
    }

    public int CalculateIncomingDamage(int rawDamage)
    {
        if (rawDamage <= 0) return 0;
        if (evasion > 0 && UnityEngine.Random.value < evasion / 100f) return 0;

        // 초반 밸런싱이 쉽도록 방어력은 고정 피해 감소로만 처리한다.
        return Mathf.Max(0, rawDamage - defense);
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        strength = Mathf.Max(0, strength);
        moveSpeed = Mathf.Max(0, moveSpeed);
        attackSpeed = Mathf.Max(0, attackSpeed);
        level = Mathf.Max(1, level);
        defense = Mathf.Max(0, defense);
        evasion = Mathf.Clamp(evasion, 0, 100);
    }
}
