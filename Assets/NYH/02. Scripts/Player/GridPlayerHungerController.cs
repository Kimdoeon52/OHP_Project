using UnityEngine;

/// <summary>
/// 플레이어의 배고픔(hungry) 수치를 시간에 따라 감소시키고, 구간에 따른 체력 증감 틱을 관리하는 전담 컴포넌트입니다.
///
/// [허기 구간별 효과]
///   hungry == 0  : 이동 쿨타임 +0.5초 페널티 / 5초마다 체력 1 감소
///   hungry 1~3   : 10초마다 체력 1 감소
///   hungry 4~6   : 변화 없음
///   hungry 7~10  : 5초마다 체력 1 회복
///
/// [감소 주기]  10초마다 hungry -1
/// [Safe Room] TimeSystem.IsRunning == false 이면 모든 틱 동작 정지
/// </summary>
public class GridPlayerHungerController : MonoBehaviour
{
    // ── 이동 쿨타임 보정 ───────────────────────────────────────────
    /// <summary>hungry == 0 일 때 이동 행동 딜레이에 더해지는 고정 패널티(초).</summary>
    [Header("굶주림 패널티")]
    [Tooltip("hungry가 0일 때 이동 쿨타임에 추가되는 고정 시간(초)입니다. 기본값 0.5.")]
    [SerializeField, Min(0f)] private float hungerMovePenalty = 0.5f;

    [Tooltip("허기 수치가 이 값 이하이면 배고픔 상태 아이콘을 띄웁니다. 기본 3(배고픔 구간).")]
    [SerializeField, Min(0)] private int hungryStatusThreshold = 3;

    // 배부름 구간 기본 회복량(틱당). 건강한 몸 특성이 배율로 곱한다.
    private const int BaseHealPerTick = 1;

    // 배고픔은 지속 상태라 사실상 만료되지 않게 아주 긴 시간으로 걸고, 회복되면 명시적으로 제거한다.
    private const int HungerStatusDuration = 999999;

    // ── 내부 참조 ──────────────────────────────────────────────────
    private NYHPlayerStats _stats;
    private GridPlayerHealth _health;
    private StatusController _status;

    // ── 타이머 ─────────────────────────────────────────────────────
    private float _hungerDrainTimer = 0f;
    private float _healthTickTimer  = 0f;

    // 배부름 회복 소수분 누적기. 건강한 몸 배율(예 ×1.25)의 소수분을 모아 다음 회복에 넘긴다.
    private float _healAccumulator = 0f;

    // ── 이동 패널티 상태 ────────────────────────────────────────────
    /// <summary>현재 굶주림 이동 패널티가 활성화되어 있는지 반환합니다.</summary>
    public bool IsHungerPenaltyActive { get; private set; }

    /// <summary>hungry == 0 일 때 이동 딜레이에 더해야 할 고정 페널티 값입니다.</summary>
    public float HungerMovePenalty => hungerMovePenalty;

    // ── Unity 생명주기 ─────────────────────────────────────────────
    private void Start()
    {
        _stats = GetComponent<NYHPlayerStats>();
        if (_stats == null) _stats = NYHPlayerStats.Instance;

        _health = GetComponent<GridPlayerHealth>();
        _status = GetComponent<StatusController>();

        if (_stats != null)
        {
            _stats.OnStatsChanged += RefreshPenaltyState;
            RefreshPenaltyState(); // 시작 시 초기 상태 갱신
        }
    }

    private void OnDestroy()
    {
        if (_stats != null)
            _stats.OnStatsChanged -= RefreshPenaltyState;
    }

    private void Update()
    {
        // Safe Room 등 시간이 흐르지 않는 곳에서는 허기/체력 변화 없음
        if (TimeSystem.Instance == null || !TimeSystem.Instance.IsRunning) return;
        if (_stats == null || _health == null || _health.IsDead) return;

        float dt = Time.deltaTime;

        // 1. 허기 수치 자연 감소 (10초에 1)
        _hungerDrainTimer += dt;
        if (_hungerDrainTimer >= 10f)
        {
            _hungerDrainTimer -= 10f;
            if (_stats.Hungry > 0)
                _stats.AddStat(NYHStatType.Hunger, -1);
        }

        // 2. 허기 구간에 따른 체력 틱
        _healthTickTimer += dt;
        int h = _stats.Hungry;

        if (h == 0)
        {
            // 굶주림: 5초마다 체력 1 감소
            _healAccumulator = 0f;
            if (_healthTickTimer >= 5f)
            {
                _healthTickTimer -= 5f;
                _health.TakeDamage(1);
            }
        }
        else if (h <= 3)
        {
            // 배고픔: 10초마다 체력 1 감소
            _healAccumulator = 0f;
            if (_healthTickTimer >= 10f)
            {
                _healthTickTimer -= 10f;
                _health.TakeDamage(1);
            }
        }
        else if (h >= 7)
        {
            // 포식: 5초마다 회복. 건강한 몸 배율의 소수분은 누적해 다음 회복에 넘긴다.
            if (_healthTickTimer >= 5f)
            {
                _healthTickTimer -= 5f;
                _healAccumulator += BaseHealPerTick * HealMultiplier();
                int heal = Mathf.FloorToInt(_healAccumulator);
                if (heal > 0)
                {
                    _healAccumulator -= heal;
                    _health.Heal(heal);
                }
            }
        }
        // h 4~6: 변화 없음 (타이머는 계속 누적되지 않도록 초기화)
        else
        {
            _healthTickTimer = 0f;
            _healAccumulator = 0f;
        }
    }

    // 건강한 몸 회복 배율 (특성 없으면 1배)
    private float HealMultiplier()
    {
        TraitController trait = TraitController.FindAvailableInstance();
        return trait != null ? trait.GetHealthyBodyRecoveryMultiplier() : 1f;
    }

    // ── 이동 패널티 갱신 ───────────────────────────────────────────
    /// <summary>
    /// hungry가 바뀔 때마다 호출되어 이동 패널티 플래그를 최신 상태로 유지합니다.
    /// GridPlayerMovement가 이 컴포넌트의 IsHungerPenaltyActive와 HungerMovePenalty를 읽어 딜레이에 더합니다.
    /// </summary>
    private void RefreshPenaltyState()
    {
        if (_stats == null) return;
        IsHungerPenaltyActive = (_stats.Hungry == 0);
        SyncHungerStatus();
    }

    // 허기가 배고픔 구간이면 배고픔 상태를 유지하고, 회복되면 제거한다. 아이콘 표시는 StatusIconBarView가 담당.
    private void SyncHungerStatus()
    {
        if (_status == null) return;

        if (_stats.Hungry <= hungryStatusThreshold)
            _status.ApplyStatus(new StatusRequest(null, Vector2Int.zero, StatusType.Hunger, HungerStatusDuration, 1, null));
        else
            _status.TryRemoveStatus(StatusType.Hunger);
    }
}
