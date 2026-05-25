using System;
using Unity.VisualScripting;
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
    [Tooltip("최대 체력")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [Tooltip("힘")]
    [SerializeField, Min(0)] private int strength = 1;
    [Tooltip("이동 속도")]
    [SerializeField, Min(0)] private int moveSpeed = 1;
    [Tooltip("공격 속도")]
    [SerializeField, Min(0)] private int attackSpeed = 1;
    [Tooltip("레벨")]
    [SerializeField, Min(1)] private int level = 1;
    [Tooltip("정확도")]
    [SerializeField, Range(0, 100)] private int accuracy;
    


    [Header("방어 스탯")]
    [Tooltip("방어력")]
    [SerializeField, Min(0)] private int defense;
    [Tooltip("회피")]
    [SerializeField, Range(0, 100)] private int evasion;
    [Tooltip("배고픔")]
    //배고픔 수치는 던전에서 보내는 시간 10초에 1씩 감소한다.
    //배고픔 수치의 최대치는 10, 최소치는 0이다.
    [SerializeField, Range(0, 10)] private int hungry;


    //상태이상 종류들 굶주림,독,화상,둔화,출혈,냉기,부유,신속
    [Header("상태 이상")]
    [Tooltip("굶주림")]
    [SerializeField] private bool isStarvation; // 굶주림: 체력 감소(시간이 갈때만 세이프 룸에서는 체력 안닳음) + 이동속도 감소
    [Tooltip("독")] // 독과 화상 출혈은 데미지 들어가는 방식은 같음 1 ~ n 사이의 데미지를 랜덤으로 초마다 받음
    [SerializeField] private bool isPoison;     // 독: 일정 시간동안 체력 감소 , 정화 포션 사용시 제거 , 정화 포션 사용중엔 안걸림
    [Tooltip("화상")]
    [SerializeField] private bool isBurn;       // 화상: 서리 포션 사용 시 해제
    [Tooltip("둔화")]
    [SerializeField] private bool isFrozen;     // 둔화:  이동 속도 감소
    [Tooltip("출혈")]
    [SerializeField] private bool isBleeding;   // 출혈: 풀 수 잇는 방법이 없음 대신 지속시간이 짧음
    [Tooltip("냉기")]
    [SerializeField] private bool isCold;     // 냉기: 이동 쿨타임 및 공격 쿨타임 증가
    [Tooltip("부유")]
    [SerializeField] private bool isWounds;   // 부유: Hazard 효과 무시
    [Tooltip("신속")]
    [SerializeField] private bool isSpeed;   // 신속: 이동 속도 증가


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

        // 초반 밸런싱이 쉽도록 방어력은 고정 피해 감소로만 처리
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
