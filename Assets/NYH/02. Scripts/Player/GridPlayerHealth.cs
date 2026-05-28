using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 HP + 회복 포션.
/// 포션: 즉시 +50% MaxHp, 이후 25초에 걸쳐 추가 +50% (curve로 곡선 조절).
/// </summary>
public class GridPlayerHealth : PersistentSingleton<GridPlayerHealth>, IDamageable
{
    [Header("기본 체력")]
    [Tooltip("NYHPlayerStats가 없을 때 사용할 기본 최대 체력입니다. Stats 컴포넌트가 있으면 MaxHealth 값을 우선 사용합니다.")]
    [SerializeField] private int maxHp = 100;

    [Tooltip("게임 시작 시 플레이어가 들고 시작할 포션 개수입니다.")]
    [SerializeField] private int startPotionCount = 1;

    [Header("Debug Damage")]
    [Tooltip("에디터에서 지정 키를 눌러 플레이어에게 피해를 주는 테스트 기능을 사용할지 결정합니다.")]
    [SerializeField] private bool enableDebugDamageKey = true;

    [Tooltip("디버그 피해를 줄 키입니다. enableDebugDamageKey가 켜져 있을 때만 동작합니다.")]
    [SerializeField] private KeyCode debugDamageKey = KeyCode.H;

    [Tooltip("디버그 피해 키를 눌렀을 때 적용할 원본 피해량입니다. 방어/회피 계산은 그대로 거칩니다.")]
    [SerializeField, Min(1)] private int debugDamageAmount = 10;

    [Header("포션 회복 곡선")]
    [Tooltip("X: 0~1 진행률 (0=사용 직후, 1=25초 후), Y: 0~1 누적 회복량 비율")]
    [SerializeField] private AnimationCurve potionRegenCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.12f, 0.6f),   // ~3초까지 60% 누적
        new Keyframe(0.4f, 0.85f),   // ~10초까지 85%
        new Keyframe(1f, 1f));        // 25초에 100%
    [Tooltip("포션의 지속 회복이 끝날 때까지 걸리는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float potionDuration = 25f;

    private GridPlayer _player;
    private NYHPlayerStats _stats;
    private Coroutine _regenCoroutine;

    public event Action OnHealthChanged;
    public event Action<int> OnDamaged;

    /// <summary>
    /// 플레이어 체력이 0이 되어 사망이 확정됐을 때 호출된다.
    /// 사용 예: GridPlayerAnimationController가 구독해서 사망 애니메이션을 재생한다.
    /// </summary>
    public event Action OnDied;

    public int CurrentHp { get; private set; }
    public int MaxHp => _stats != null ? _stats.MaxHealth : maxHp;
    public bool IsDead => CurrentHp <= 0;
    public int PotionCount { get; private set; }

#if UNITY_EDITOR
    private void Update()
    {
        if (!enableDebugDamageKey || IsDead) return;

        if (Input.GetKeyDown(debugDamageKey))
            TakeDamage(debugDamageAmount);
    }
#endif

    /// <summary>
    /// GridPlayer에서 호출하는 초기화 진입점입니다.
    /// 스탯 컴포넌트와 연결하고 현재 체력/포션 수를 시작 값으로 세팅합니다.
    /// </summary>
    public void Init(GridPlayer player)
    {
        _player = player;
        _stats = GetComponent<NYHPlayerStats>();
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;
        if (_stats != null)
            _stats.OnStatAdded += OnStatAdded;

        CurrentHp = MaxHp;
        PotionCount = startPotionCount;
        RaiseHealthChanged();
    }

    /// <summary>
    /// 플레이어에게 피해를 적용합니다.
    /// NYHPlayerStats가 있으면 방어력/회피율 계산을 거친 뒤 최종 피해만 HP에서 차감합니다.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        int finalDamage = CalculateIncomingDamage(amount);
        if (finalDamage <= 0) return;

        CurrentHp = Mathf.Max(0, CurrentHp - finalDamage);
        OnDamaged?.Invoke(finalDamage);
        GameEvents.OnPlayerDamaged?.Invoke(finalDamage);
        RaiseHealthChanged();

        if (CurrentHp <= 0)
        {
            OnDied?.Invoke();
            GameEvents.OnPlayerDied?.Invoke();
            TimeSystem.Instance.StopTimer();
        }
    }

    /// <summary>
    /// 플레이어 HP를 회복합니다. 사망 상태에서는 회복하지 않습니다.
    /// </summary>
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        RaiseHealthChanged();
    }

    /// <summary>
    /// 포션 개수를 추가하고 HP UI가 포션 수를 다시 읽을 수 있도록 변경 이벤트를 보냅니다.
    /// </summary>
    public void AddPotion(int count = 1)
    {
        PotionCount += count;
        RaiseHealthChanged();
    }

    /// <summary>
    /// 포션을 1개 소비해서 즉시 회복과 지속 회복을 시작합니다.
    /// 포션 사용도 플레이어 행동이므로 TurnManager에 행동 비용을 알립니다.
    /// </summary>
    public void TryUsePotion()
    {
        if (PotionCount <= 0 || IsDead) return;
        if (_player.ActionDelay != null && !_player.ActionDelay.CanAct) return;

        PotionCount--;
        RaiseHealthChanged();

        // 즉시 +50% MaxHp
        Heal(Mathf.RoundToInt(MaxHp * 0.5f));

        // 진행 중인 회복 코루틴 취소 후 새로 시작
        if (_regenCoroutine != null) StopCoroutine(_regenCoroutine);
        _regenCoroutine = StartCoroutine(RegenOverTime());

        float actionDelay = _player.Config.potionTimeDealy;
        _player.ActionDelay?.StartDelay(actionDelay);
        TurnManager.Instance.OnPlayerActionCompleted(actionDelay);
    }

    private IEnumerator RegenOverTime()
    {
        float elapsed = 0f;
        float pool = MaxHp * 0.5f;  // 시간에 걸쳐 회복할 총량 = 50% MaxHp
        float deliveredFraction = 0f;

        while (elapsed < potionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / potionDuration);
            float currentFraction = Mathf.Clamp01(potionRegenCurve.Evaluate(progress));

            float deltaFraction = currentFraction - deliveredFraction;
            if (deltaFraction > 0f)
            {
                int delta = Mathf.RoundToInt(pool * deltaFraction);
                if (delta > 0)
                {
                    Heal(delta);
                    deliveredFraction += (float)delta / pool;
                }
            }

            if (IsDead) yield break;
            yield return null;
        }

        _regenCoroutine = null;
    }

    private void OnDestroy()
    {
        if (_stats != null)
            _stats.OnStatAdded -= OnStatAdded;
    }

    /// <summary>
    /// 최대 체력 스탯이 증가했을 때 현재 체력도 같은 양만큼 보정합니다.
    /// 최대 체력만 늘고 현재 체력이 그대로 남는 어색함을 막기 위한 연결 지점입니다.
    /// </summary>
    private void OnStatAdded(NYHStatType type, int amount)
    {
        if (type == NYHStatType.Health && amount > 0)
            Heal(amount);

        RaiseHealthChanged();
    }

    /// <summary>
    /// 원본 피해량을 실제 HP 차감량으로 바꿉니다.
    /// 스탯 컴포넌트가 없으면 방어/회피 없이 원본 피해를 그대로 반환합니다.
    /// </summary>
    private int CalculateIncomingDamage(int rawDamage)
    {
        if (rawDamage <= 0) return 0;
        if (_stats == null) return rawDamage;
        return _stats.CalculateIncomingDamage(rawDamage);
    }

    /// <summary>
    /// HP나 포션 수가 바뀐 뒤 UI/피드백 스크립트에게 변경 사실을 알립니다.
    /// </summary>
    private void RaiseHealthChanged()
    {
        OnHealthChanged?.Invoke();
    }
}
