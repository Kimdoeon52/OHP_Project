using System;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨과 경험치 통을 관리한다.
/// 실제 능력치 값은 NYHPlayerStats가 담당하고, 이 클래스는 경험치 누적/레벨업/레벨업 보너스 적용만 담당한다.
/// </summary>
public class PlayerLevelSystem : MonoBehaviour
{
    public static PlayerLevelSystem Instance { get; private set; }

    [Header("레벨 / 경험치")]
    [Tooltip("현재 플레이어 레벨입니다. 실제 스탯 값은 NYHPlayerStats가 담당하고, 이 값은 성장 단계만 나타냅니다.")]
    [SerializeField, Min(1)] private int level = 1;

    [Tooltip("현재 레벨에서 누적된 경험치입니다. Required Exp 이상이 되면 레벨업하고 남은 경험치는 다음 레벨로 이월됩니다.")]
    [SerializeField, Min(0)] private int currentExp;

    [Tooltip("1레벨에서 2레벨로 오르는 데 필요한 기본 경험치입니다.")]
    [SerializeField, Min(1)] private int baseRequiredExp = 20;

    [Tooltip("레벨이 오를수록 다음 레벨 요구 경험치가 증가하는 배율입니다. 예: 1.25면 레벨마다 약 25% 증가합니다.")]
    [SerializeField, Min(1f)] private float requiredExpGrowth = 1.25f;

    [Header("레벨업 보너스")]
    [Tooltip("레벨업할 때 증가하는 최대 체력입니다. 실제 적용은 NYHPlayerStats.AddStat(Health)로 전달됩니다.")]
    [SerializeField, Min(0)] private int healthPerLevel = 5;


    /// <summary>
    /// 레벨이 하나 이상 올랐을 때 호출된다.
    /// UI 갱신, 레벨업 연출, 선택지 표시 등에 사용한다.
    /// </summary>
    public event Action OnLevelChanged;

    /// <summary>
    /// 경험치가 추가됐을 때 호출된다.
    /// amount는 이번에 들어온 경험치량이며, 현재 경험치 총량은 CurrentExp로 읽는다.
    /// </summary>
    public event Action<int> OnExpChanged;

    public int Level => level;
    // 경험치 저장
    public int CurrentExp => currentExp;
    public int RequiredExp => CalculateRequiredExp(level);

    /// <summary>
    /// 씬에서 전역 접근할 레벨 시스템 인스턴스를 등록한다.
    /// GridPlayer의 RequireComponent로 플레이어에 붙도록 보장한다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("PlayerLevelSystem: 씬에 레벨 시스템이 둘 이상 있습니다. 먼저 등록된 값을 사용합니다.");
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 적 사망 보상 이벤트를 구독한다.
    /// EnemyBase가 GameEvents.OnExpDropped를 발행하면 자동으로 경험치를 받는다.
    /// </summary>
    private void OnEnable()
    {
        GameEvents.OnExpDropped += OnExpDropped;
    }

    /// <summary>
    /// 비활성화 시 이벤트 구독을 해제해 중복 수신을 막는다.
    /// </summary>
    private void OnDisable()
    {
        GameEvents.OnExpDropped -= OnExpDropped;
    }

    /// <summary>
    /// 현재 인스턴스가 파괴되면 싱글톤 참조를 정리한다.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 경험치를 추가한다.
    /// 요구 경험치를 초과하면 여러 번 연속 레벨업할 수 있으며, 남은 경험치는 다음 레벨에 이월된다.
    /// </summary>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        currentExp += amount;
        bool leveledUp = false;

        while (currentExp >= RequiredExp)
        {
            currentExp -= RequiredExp;
            LevelUp();
            leveledUp = true;
        }

        OnExpChanged?.Invoke(amount);
        if (leveledUp)
            OnLevelChanged?.Invoke();
    }

    /// <summary>
    /// 레벨을 1 올리고 레벨업 보너스를 NYHPlayerStats에 적용한다.
    /// 이 클래스는 성장 단계만 관리하고, 실제 능력치 저장은 NYHPlayerStats가 담당한다.
    /// </summary>
    private void LevelUp()
    {
        level++;

        NYHPlayerStats stats = NYHPlayerStats.Instance;
        if (stats == null) return;

        if (healthPerLevel > 0)
            stats.AddStat(NYHStatType.Health, healthPerLevel);
    }

    /// <summary>
    /// targetLevel에서 다음 레벨로 오르는 데 필요한 경험치를 계산한다.
    /// 공식: baseRequiredExp * requiredExpGrowth^(level - 1)
    /// </summary>
    private int CalculateRequiredExp(int targetLevel)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseRequiredExp * Mathf.Pow(requiredExpGrowth, targetLevel - 1)));
    }

    /// <summary>
    /// 적 사망 보상 이벤트 콜백.
    /// position은 드랍 위치용 정보라 현재 경험치 계산에는 사용하지 않는다.
    /// </summary>
    private void OnExpDropped(Vector2Int position, int exp)
    {
        AddExp(exp);
    }

    /// <summary>
    /// 인스펙터에서 잘못된 값이 들어가지 않도록 범위를 보정한다.
    /// </summary>
    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        currentExp = Mathf.Max(0, currentExp);
        baseRequiredExp = Mathf.Max(1, baseRequiredExp);
        requiredExpGrowth = Mathf.Max(1f, requiredExpGrowth);
    }
}
