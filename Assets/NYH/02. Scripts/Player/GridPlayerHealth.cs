using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 HP + 회복 포션.
/// 포션: 즉시 +50% MaxHp, 이후 25초에 걸쳐 추가 +50% (curve로 곡선 조절).
/// </summary>
public class GridPlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int startPotionCount = 1;

    [Header("Debug Damage")]
    [SerializeField] private bool enableDebugDamageKey = true;
    [SerializeField] private KeyCode debugDamageKey = KeyCode.H;
    [SerializeField, Min(1)] private int debugDamageAmount = 10;

    [Header("포션 회복 곡선")]
    [Tooltip("X: 0~1 진행률 (0=사용 직후, 1=25초 후), Y: 0~1 누적 회복량 비율")]
    [SerializeField] private AnimationCurve potionRegenCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.12f, 0.6f),   // ~3초까지 60% 누적
        new Keyframe(0.4f, 0.85f),   // ~10초까지 85%
        new Keyframe(1f, 1f));        // 25초에 100%
    [SerializeField] private float potionDuration = 25f;

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (!enableDebugDamageKey || IsDead) return;

        if (Input.GetKeyDown(debugDamageKey))
            TakeDamage(debugDamageAmount);
    }
#endif

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

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        RaiseHealthChanged();
    }

    public void AddPotion(int count = 1)
    {
        PotionCount += count;
        RaiseHealthChanged();
    }

    public void TryUsePotion()
    {
        if (PotionCount <= 0 || IsDead) return;

        PotionCount--;
        RaiseHealthChanged();

        // 즉시 +50% MaxHp
        Heal(Mathf.RoundToInt(MaxHp * 0.5f));

        // 진행 중인 회복 코루틴 취소 후 새로 시작
        if (_regenCoroutine != null) StopCoroutine(_regenCoroutine);
        _regenCoroutine = StartCoroutine(RegenOverTime());

        TurnManager.Instance.OnPlayerActionCompleted(_player.Config.potionTimeCost);
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

    private void OnStatAdded(NYHStatType type, int amount)
    {
        if (type == NYHStatType.Health && amount > 0)
            Heal(amount);

        RaiseHealthChanged();
    }

    private int CalculateIncomingDamage(int rawDamage)
    {
        if (rawDamage <= 0) return 0;
        if (_stats == null) return rawDamage;
        return _stats.CalculateIncomingDamage(rawDamage);
    }

    private void RaiseHealthChanged()
    {
        OnHealthChanged?.Invoke();
    }
}
