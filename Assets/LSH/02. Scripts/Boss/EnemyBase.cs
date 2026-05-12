using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 적의 공통 베이스 클래스.
/// GridEnemyBase의 기능(HP, 시야, 텔레그래프, 턴 연동)과
/// 기존 EnemyBase의 YJW 호환 필드(moveSpeed, isDetected)를 통합.
/// 자식 클래스가 OnTurnUpdate()에서 AI를 구현한다.
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

    private readonly List<TelegraphIndicator> _activeTelegraphs = new();

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

        CurrentHp -= amount;
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
        GameEvents.OnEnemyKilled?.Invoke(GridPosition, data.timeBonus);
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
