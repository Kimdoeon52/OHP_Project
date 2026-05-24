using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 적의 공통 베이스 클래스.
/// GridEnemyBase의 기능(HP, 시야, 텔레그래프, 턴 연동)과
/// 기존 EnemyBase의 YJW 호환 필드(moveSpeed, isDetected)를 통합.
///
/// Phase 0 확장:
///   - 공통 이동 로직 (TryStepTowardPlayer / TryStep) 추출
///   - 템플릿 메서드: ExecutePendingAttack → PerformMovement → CheckAttackTelegraph
///   - 범위 데미지/방어/회피 판정 메서드
///   - 경험치/골드 드롭 이벤트
///   - _pendingAttackCell 공통화
/// </summary>
public abstract class EnemyBase : EntityBase, IDamageable
{
    [SerializeField] protected EnemyData data;
    [SerializeField] protected TelegraphIndicator telegraphPrefab;

    // ── IDamageable ──
    public int CurrentHp { get; protected set; }
    public int MaxHp => data != null ? data.maxHp : 1;
    public bool IsDead => CurrentHp <= 0;

    // ── YJW 호환용 public 필드 (Noose, Kunai 등에서 직접 접근) ──
    [Header("상태")]
    public float moveSpeed;
    public bool isDetected = false;

    // ── 내부 참조 ──
    protected GridPlayer Player { get; private set; }

    // ── 공통 공격 의도 셀 (대부분의 적이 사용) ──
    protected Vector2Int? _pendingAttackCell;

    // ── 액션 포인트 (TimeSystem 연동) ──
    protected float _moveActionPoints = 0f;
    protected float _attackActionPoints = 0f;

    private readonly List<TelegraphIndicator> _activeTelegraphs = new();

    // ── 데이터 접근 헬퍼 (자식 클래스 편의) ──
    public EnemyData Data => data;

    // ══════════════ 라이프사이클 ══════════════

    protected override void Awake()
    {
        base.Awake();
        if (data == null)
        {
            Debug.LogError($"{name}: EnemyData가 할당되지 않았습니다.");
            return;
        }

        CurrentHp = data.maxHp;
        if (data.sprite != null) SpriteRenderer.sprite = data.sprite;
    }

    private void OnEnable()
    {
        GameEvents.OnVisionUpdated += OnVisionUpdated;
    }

    private void OnDisable()
    {
        GameEvents.OnVisionUpdated -= OnVisionUpdated;
    }

    protected virtual void Start()
    {
        Player = FindFirstObjectByType<GridPlayer>();
        if (Player == null)
        {
            Debug.LogError($"{name}: 씬에 GridPlayer가 없습니다.");
            return;
        }

        // 씬 배치 위치를 격자에 스냅
        var startPos = GridSystem.Instance.WorldToGrid(transform.position);
        Initialize(startPos);

        TurnManager.Instance.Register(this);
        UpdateVisibility();
    }

    // ══════════════ 시야 ══════════════

    private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible, IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    protected void UpdateVisibility()
    {
        bool visible = VisionSystem.Instance != null && VisionSystem.Instance.IsVisible(GridPosition);
        SpriteRenderer.enabled = visible;
        foreach (var t in _activeTelegraphs)
            if (t != null) t.RefreshVisibility();
    }

    // ══════════════ HP / 데미지 ══════════════

    public virtual void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        int finalDamage = ApplyDefense(amount);
        CurrentHp -= finalDamage;
        if (CurrentHp <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
    }

    protected virtual void Die()
    {
        ClearTelegraphs();
        TurnManager.Instance.Unregister(this);

        // 경험치/골드 드롭 이벤트
        GameEvents.OnEnemyKilled?.Invoke(GridPosition, data.timeBonus);
        GameEvents.OnExpDropped?.Invoke(GridPosition, data.expReward);
        if (data.goldMax > 0)
        {
            int gold = Random.Range(data.goldMin, data.goldMax + 1);
            if (gold > 0) GameEvents.OnGoldDropped?.Invoke(GridPosition, gold);
        }

        TimeSystem.Instance.AddTime(data.timeBonus);
        Destroy(gameObject);
    }

    // ══════════════ 텔레그래프 ══════════════

    /// <summary>
    /// 셀에 공격 의도 텔레그래프 표시. 호출자가 ClearTelegraphs로 정리.
    /// </summary>
    protected void ShowTelegraph(Vector2Int cell)
    {
        if (telegraphPrefab == null) return;
        var ind = Instantiate(telegraphPrefab);
        ind.Show(cell);
        _activeTelegraphs.Add(ind);
    }

    protected void ClearTelegraphs()
    {
        foreach (var t in _activeTelegraphs)
            if (t != null) Destroy(t.gameObject);
        _activeTelegraphs.Clear();
    }

    // ══════════════ 공통 이동 로직 ══════════════

    /// <summary>
    /// 플레이어 방향으로 1칸 이동 시도.
    /// 더 먼 축 우선, 차단 시 다른 축 시도.
    /// </summary>
    protected void TryStepTowardPlayer()
    {
        var diff = Player.GridPosition - GridPosition;

        var dirX = diff.x != 0 ? new Vector2Int(System.Math.Sign(diff.x), 0) : Vector2Int.zero;
        var dirY = diff.y != 0 ? new Vector2Int(0, System.Math.Sign(diff.y)) : Vector2Int.zero;

        bool xPrimary = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);
        var primary = xPrimary ? dirX : dirY;
        var secondary = xPrimary ? dirY : dirX;

        if (primary != Vector2Int.zero && TryStep(primary)) return;
        if (secondary != Vector2Int.zero && TryStep(secondary)) return;
    }

    /// <summary>
    /// 한 칸 이동 시도. virtual — 벽 통과 적이 override 가능.
    /// </summary>
    protected virtual bool TryStep(Vector2Int dir)
    {
        var next = GridPosition + dir;
        if (!GridSystem.Instance.IsWalkable(next)) return false;
        if (next == Player.GridPosition) return false;
        if (TurnManager.Instance.GetEntityAt(next) != null) return false;

        SetGridPosition(next);
        UpdateVisibility();
        return true;
    }

    // ══════════════ 전투 판정 메서드 ══════════════

    /// <summary>
    /// 공격력 범위 내 랜덤 데미지 산출
    /// </summary>
    protected int RollAttackDamage()
    {
        return Random.Range(data.attackMin, data.attackMax + 1);
    }

    /// <summary>
    /// 방어력 적용 후 최종 데미지 반환 (최소 0)
    /// </summary>
    protected int ApplyDefense(int rawDamage)
    {
        int defense = Random.Range(data.defenseMin, data.defenseMax + 1);
        return Mathf.Max(0, rawDamage - defense);
    }

    /// <summary>
    /// 명중 판정: accuracy vs targetEvasion. true면 명중.
    /// </summary>
    protected bool RollHit(int targetEvasion)
    {
        // 간단한 판정: accuracy - evasion >= 0이면 기본 명중
        // 차이가 클수록 명중률 상승. 최소 5% 명중, 최대 95% 명중.
        float hitChance = Mathf.Clamp(0.5f + (data.accuracy - targetEvasion) * 0.05f, 0.05f, 0.95f);
        return Random.value <= hitChance;
    }

    /// <summary>
    /// 이동 비용 (초/칸)
    /// </summary>
    public float GetMoveCost()
    {
        return data != null ? data.moveSpeed : 1.0f;
    }

    /// <summary>
    /// 공격 비용 (초/회)
    /// </summary>
    public float GetAttackCost()
    {
        return (data != null && data.attackSpeed > 0f) ? (1f / data.attackSpeed) : 1.0f;
    }

    /// <summary>
    /// 명중 시 상태이상 적용 (onHitEffect가 설정된 경우)
    /// </summary>
    protected void TryApplyOnHitEffect(IDamageable target)
    {
        if (data.onHitEffect == null) return;
        if (target is not Component component) return;

        var effectData = data.onHitEffect;

        // StatusRequest를 통해 기존 StatusController와 통합
        var request = new StatusRequest(
            target as IGridEntity,
            (target as IGridEntity)?.GridPosition ?? GridPosition,
            effectData.statusType,
            effectData.durationTurns,
            effectData.power,
            null // sourceTrap은 null (적이 원인)
        );

        StatusController controller = component.GetComponent<StatusController>();
        if (controller == null)
            controller = component.gameObject.AddComponent<StatusController>();
        controller.ApplyStatus(request);

        GameEvents.OnStatusEffectApplied?.Invoke(request);
    }

    // ══════════════ 템플릿 메서드 (자식 클래스 오버라이드용) ══════════════

    /// <summary>
    /// 기본 턴 흐름: 공격 발동 → 이동 → 공격 예고.
    /// 자식 클래스는 이 메서드를 override하거나,
    /// 세부 단계(ExecutePendingAttack / PerformMovement / CheckAttackTelegraph)만 override.
    /// </summary>
    public override void OnTurnUpdate()
    {
        if (IsDead || Player == null) return;

        // 턴 시작 시 직전 플레이어 행동 비용만큼 액션 포인트(초) 충전
        float elapsedSeconds = TurnManager.Instance != null ? TurnManager.Instance.LastPlayerActionTimeCost : 1.0f;
        _moveActionPoints += elapsedSeconds;
        _attackActionPoints += elapsedSeconds;

        // 1단계: 이전 턴 공격 발동
        ExecutePendingAttack();

        // 2단계: 이동 처리 (이동 비용이 충족될 때 실행)
        float moveCost = GetMoveCost();
        while (_moveActionPoints >= moveCost)
        {
            PerformMovement();
            _moveActionPoints -= moveCost;

            // moveCost가 1.0초 이상인 보통/느린 적은 한 턴에 최대 한 칸만 이동하도록 제어
            if (moveCost >= 1.0f)
                break;
        }

        // 3단계: 공격 예고 (공격 비용이 충족될 때 실행)
        float attackCost = GetAttackCost();
        if (_attackActionPoints >= attackCost)
        {
            CheckAttackTelegraph();
            _attackActionPoints -= attackCost;
        }
    }

    /// <summary>
    /// 이전 턴에 예고한 공격 발동
    /// </summary>
    protected virtual void ExecutePendingAttack()
    {
        if (_pendingAttackCell.HasValue && Player.GridPosition == _pendingAttackCell.Value)
        {
            int damage = RollAttackDamage();
            Player.TakeDamage(damage);
            TryApplyOnHitEffect(Player);
        }
        _pendingAttackCell = null;
        ClearTelegraphs();
    }

    /// <summary>
    /// 이동 처리. 기본: 시야 안이면 플레이어 방향 1칸 이동.
    /// </summary>
    protected virtual void PerformMovement()
    {
        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            TryStepTowardPlayer();
        }
    }

    /// <summary>
    /// 공격 예고. 기본: 인접 시 다음 턴 공격 의도 표시.
    /// </summary>
    protected virtual void CheckAttackTelegraph()
    {
        if (IsAdjacentToPlayer())
        {
            _pendingAttackCell = Player.GridPosition;
            ShowTelegraph(_pendingAttackCell.Value);
        }
    }

    // ══════════════ 유틸리티 ══════════════

    protected bool IsAdjacentToPlayer()
    {
        var diff = Player.GridPosition - GridPosition;
        return Mathf.Abs(diff.x) + Mathf.Abs(diff.y) == 1;
    }

    protected float SquaredDistanceToPlayer()
    {
        var diff = Player.GridPosition - GridPosition;
        return diff.x * diff.x + diff.y * diff.y;
    }
}
