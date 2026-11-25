using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 모든 적 캐릭터의 공통 베이스 클래스 (격자 이동·시야·텔레그래프·AP 턴·전투·사망)
public abstract class EnemyBase : EntityBase, IDamageable
{
    [Header("설정 및 에셋 데이터")]
    [Tooltip("적 데이터 SO")]
    [SerializeField] protected EnemyData data;

    [Header("공격 예고 (오브젝트 방식)")]
    [Tooltip("공격 예고 바닥 프리팹")]
    [SerializeField] protected GameObject warningPrefab;

    [Header("피격 연출")]
    [Tooltip("피격 파티클 프리팹")]
    [SerializeField] protected GameObject hitParticlePrefab;

    [Header("사망 연출")]
    [Tooltip("사망 파티클 프리팹")]
    [SerializeField] protected GameObject deathEffectPrefab;

    [Header("보상 드롭")]
    [Tooltip("골드 픽업 프리팹")]
    [SerializeField] private GoldPickup goldPickupPrefab;
    [Tooltip("아이템 픽업 프리팹")]
    [SerializeField] private ItemPickup dropItemPickupPrefab;

    [Header("생명 파편 (확정 드롭)")]
    [Tooltip("사망 시 확정 드롭 아이템")]
    [SerializeField] protected ItemBase fragmentsOfLifeItem;

    [Header("애니메이션 관련 설정")]
    [Tooltip("사망 트리거 이름")]
    [SerializeField] private string deathTriggerName = "Die";

    [Tooltip("사망 후 파괴 대기 시간")]
    [SerializeField, Min(0f)] private float deathDestroyDelay = 0.6f;

    public int CurrentHp { get; protected set; }
    public int MaxHp => data != null ? data.maxHp : 1;
    public bool IsDead => CurrentHp <= 0;

    // 외부 호환 필드 (무기·투척 효과 연동)
    [Header("외부 호환 상태")]
    [Tooltip("이동속도 (외부 디버프 연동)")]
    public float moveSpeed;
    [Tooltip("플레이어 감지 여부")]
    public bool isDetected = false;

    protected GridPlayer Player { get; private set; }
    protected Animator _animator;
    private StatusController _statusController;
    private bool _isDying;
    private DungeonRandom _fallbackRng;
    protected Vector2Int? _alarmTarget = null;

    protected Vector2Int? _pendingAttackCell;
    protected bool _isAttacking = false;

    protected float _moveActionPoints = 0f;
    protected float _attackActionPoints = 0f;

    private readonly Dictionary<Vector2Int, GameObject> _activeTelegraphs = new();
    public EnemyData Data => data;

    protected override void Awake()
    {
        base.Awake();

        _animator = GetComponent<Animator>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _statusController = gameObject.GetOrAddComponent<StatusController>();

        if (data == null)
        {
            Debug.LogError($"{name}: EnemyData가 할당 안 됨. 인스펙터 확인 필요.");
            return;
        }

        CurrentHp = data.maxHp;
        moveSpeed = data.moveSpeed;

        if (data.sprite != null)
            SpriteRenderer.sprite = data.sprite;
    }

    private void OnEnable()
    {
        GameEvents.OnVisionUpdated += OnVisionUpdated;
        GameEvents.OnAlertTriggered += OnAlertTriggered;

        if (TurnManager.Instance != null)
            TurnManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        GameEvents.OnVisionUpdated -= OnVisionUpdated;
        GameEvents.OnAlertTriggered -= OnAlertTriggered;

        ClearTelegraphs();

        if (TurnManager.Instance != null)
            TurnManager.Instance.Unregister(this);
    }

    protected override void OnDestroy()
    {
        ClearTelegraphs();
        base.OnDestroy();
    }

    protected virtual void Start()
    {
        Player = FindFirstObjectByType<GridPlayer>();
        if (Player == null)
        {
            Debug.LogError($"{name}: 씬에 GridPlayer 없음. 플레이어를 먼저 배치해야 함.");
            return;
        }

        if (GridSystem.Instance != null)
        {
            var startPos = GridSystem.Instance.WorldToGrid(transform.position);
            Initialize(startPos);
        }

        if (TurnManager.Instance != null)
            TurnManager.Instance.Register(this);

        UpdateVisibility();
    }

    // 결정론적 난수 반환 (맵 미초기화 테스트 환경에서는 인스턴스 기반 시드 폴백)
    protected DungeonRandom GetRng()
    {
        if (MapGenerator.Instance != null && MapGenerator.Instance.CurrentDepth > 0 && MapGenerator.Instance.Rng != null)
            return MapGenerator.Instance.Rng;

        if (_fallbackRng == null)
        {
            _fallbackRng = new DungeonRandom();
            int seed = (GetInstanceID() & 0x7FFFFFFF) + 1;
            _fallbackRng.Init(seed);
        }

        return _fallbackRng;
    }

    // TimeSystem 실행 중에만 흐르는 대기 코루틴
    protected IEnumerator WaitGameSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (TimeSystem.Instance == null || TimeSystem.Instance.IsRunning)
                elapsed += Time.deltaTime;

            yield return null;
        }
    }

    private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible, IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    private void OnAlertTriggered()
    {
        if (Player != null && !SafeRoomTimerGate.PlayerInSafeRoom)
        {
            _alarmTarget = Player.GridPosition;
            isDetected = true;
        }
    }

    protected virtual void UpdateVisibility()
    {
        bool visible = VisionSystem.Instance != null && VisionSystem.Instance.IsVisible(GridPosition);
        SpriteRenderer.enabled = visible;
    }

    public virtual void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        isDetected = true;

        int finalDamage = ApplyDefense(amount);
        CurrentHp -= finalDamage;

        if (finalDamage > 0)
        {
            GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.MonsterHit);
            GameEvents.OnPlayVFX?.Invoke(transform.position, VFXType.MonsterHit);
        }

        if (_animator != null && CurrentHp > 0)
            _animator.SetTrigger("Hit");

        if (CurrentHp <= 0)
            Die();
    }

    public virtual void TakeDamage(int amount, DamageType damageType)
    {
        TakeDamage(amount);
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
    }

    protected virtual void Die()
    {
        if (_isDying)
            return;

        _isDying = true;
        CurrentHp = 0;

        GameEvents.OnPlayVFX?.Invoke(transform.position, VFXType.MonsterDeath);

        StopAllCoroutines();
        ClearTelegraphs();

        TurnManager.Instance?.Unregister(this);

        float bonus = data != null ? data.timeBonus : 0f;
        int exp = data != null ? data.expReward : 0;

        GameEvents.OnEnemyKilled?.Invoke(GridPosition, bonus);
        GameEvents.OnExpDropped?.Invoke(GridPosition, exp);

        EnemyLootDropper.DropLoot(data, GridPosition, goldPickupPrefab, dropItemPickupPrefab, fragmentsOfLifeItem, GetRng(), name);

        if (TimeSystem.Instance != null && bonus > 0f)
            TimeSystem.Instance.AddTime(bonus);

        PlayDeathAnimation();
        StartCoroutine(DestroyAfterDeath());
    }

    protected void ShowTelegraph(Vector2Int cell)
    {
        if (warningPrefab == null)
            return;

        if (_activeTelegraphs.ContainsKey(cell))
            return;

        Vector3 worldPos = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(cell) : new Vector3(cell.x, cell.y, 0);
        GameObject indicator = Instantiate(warningPrefab, worldPos, Quaternion.identity);

        TelegraphIndicator ti = indicator.GetComponent<TelegraphIndicator>();
        if (ti != null)
            ti.Show(cell);

        _activeTelegraphs.Add(cell, indicator);
    }

    protected void ClearTelegraphs()
    {
        foreach (var kvp in _activeTelegraphs)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }

        _activeTelegraphs.Clear();
    }

    protected void TryStepToward(Vector2Int targetPos)
    {
        var diff = targetPos - GridPosition;

        var dirX = diff.x != 0 ? new Vector2Int(System.Math.Sign(diff.x), 0) : Vector2Int.zero;
        var dirY = diff.y != 0 ? new Vector2Int(0, System.Math.Sign(diff.y)) : Vector2Int.zero;

        bool xPrimary = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);
        var primary = xPrimary ? dirX : dirY;
        var secondary = xPrimary ? dirY : dirX;

        if (primary != Vector2Int.zero && TryStep(primary))
            return;

        if (secondary != Vector2Int.zero && TryStep(secondary))
            return;
    }

    protected void TryStepTowardPlayer()
    {
        TryStepToward(Player.GridPosition);
    }

    protected virtual void PerformMovement()
    {
        if (IsDead)
            return;

        bool inVision = data != null && SquaredDistanceToPlayer() <= data.visionRange * data.visionRange;
        if (inVision)
            isDetected = true;

        if (data != null && data.moveType == EnemyMoveType.Stationary)
            return;

        if (GameManager.Instance != null && GameManager.Instance.isAlerted && !SafeRoomTimerGate.PlayerInSafeRoom)
        {
            if (_alarmTarget == null)
                _alarmTarget = Player.GridPosition;

            isDetected = true;
        }

        if (inVision)
        {
            TryStepTowardPlayer();
            _alarmTarget = null;
            return;
        }

        if (_alarmTarget != null)
        {
            Vector2Int targetCell = _alarmTarget.Value;
            if (GridPosition != targetCell)
            {
                TryStepToward(targetCell);
                return;
            }

            _alarmTarget = null;
        }
    }

    protected virtual bool TryStep(Vector2Int dir)
    {
        var next = GridPosition + dir;

        if (IsSafeRoomCell(next))
            return false;

        if (!GridSystem.Instance.IsWalkable(next))
            return false;

        if (next == Player.GridPosition)
            return false;

        if (GridSystem.Instance.IsCellOccupied(next))
            return false;

        if (_animator != null)
            _animator.SetBool("Walk", true);

        SetGridPosition(next);
        TrapTerrainController.TryApplyTerrainAt(this, next);
        UpdateVisibility();
        UpdateSpriteDirection(next);
        return true;
    }

    protected virtual int RollAttackDamage()
    {
        if (data == null)
            return 1;

        return GetRng().Range(data.attackMin, data.attackMax + 1);
    }

    protected int ApplyDefense(int rawDamage)
    {
        if (data == null)
            return Mathf.Max(0, rawDamage);

        int defense = GetRng().Range(data.defenseMin, data.defenseMax + 1);
        return Mathf.Max(0, rawDamage - defense);
    }

    protected bool RollHit(int targetEvasion)
    {
        int attackerAccuracy = data != null ? data.accuracy : 0;
        int missChancePercent = Mathf.Clamp(targetEvasion - attackerAccuracy, 0, 50);
        return GetRng().Range(0, 100) >= missChancePercent;
    }

    protected bool TryHitPlayer(int rawDamage)
    {
        if (Player == null || Player.IsDead || rawDamage <= 0)
            return false;

        NYHPlayerStats playerStats = Player.GetComponent<NYHPlayerStats>();
        if (playerStats == null)
            playerStats = NYHPlayerStats.Instance;

        int targetEvasion = playerStats != null ? playerStats.Evasion : 0;
        if (!RollHit(targetEvasion))
        {
            GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(Player.transform.position, "Miss", 0, CombatTextType.Miss));
            return false;
        }

        int criticalReferenceMaxDamage = data != null ? data.attackMax : rawDamage;
        Player.Health.TakeDamageWithCombatText(rawDamage, criticalReferenceMaxDamage);
        TryApplyOnHitEffect(Player);
        return true;
    }

    public float GetMoveCost()
    {
        float cost = moveSpeed > 0f ? moveSpeed : (data != null ? data.moveSpeed : 1.0f);
        if (_statusController == null)
            _statusController = GetComponent<StatusController>();

        if (_statusController != null)
            cost = _statusController.ModifyMoveTimeCost(cost);

        return cost;
    }

    public float GetAttackCost()
    {
        float cost = (data != null && data.attackSpeed > 0f) ? (1f / data.attackSpeed) : 1.0f;
        if (_statusController == null)
            _statusController = GetComponent<StatusController>();

        if (_statusController != null)
            return _statusController.ModifyAttackCooldown(cost);

        return cost;
    }

    protected void TryApplyOnHitEffect(IDamageable target)
    {
        if (data == null || data.onHitEffect == null)
            return;

        if (target == null || target.IsDead)
            return;

        if (target is not Component component)
            return;

        var effectData = data.onHitEffect;
        var request = new StatusRequest(target as IGridEntity, (target as IGridEntity)?.GridPosition ?? GridPosition, effectData.statusType, effectData.durationTurns, effectData.power);

        StatusController controller = component.GetOrAddComponent<StatusController>();
        controller.ApplyStatus(request);
        GameEvents.OnStatusEffectApplied?.Invoke(request);
    }

    protected virtual void Update()
    {
        if (IsDead || Player == null)
            return;

        if (TimeSystem.Instance == null || !TimeSystem.Instance.IsRunning)
            return;

        _moveActionPoints += Time.deltaTime;
        _attackActionPoints += Time.deltaTime;

        float moveCost = GetMoveCost();
        if (!_isAttacking && _moveActionPoints >= moveCost)
        {
            PerformMovement();
            _moveActionPoints -= moveCost;
        }

        float attackCost = GetAttackCost();
        if (_attackActionPoints >= attackCost)
        {
            CheckAttackTelegraph();
            _attackActionPoints -= attackCost;
        }
    }

    protected virtual bool HasCustomMovementVisual()
    {
        return false;
    }

    protected virtual IEnumerator ExecutePendingAttack(Vector2Int targetCell, float delaySeconds)
    {
        _isAttacking = true;
        ShowTelegraph(targetCell);

        yield return WaitGameSeconds(delaySeconds);

        if (!IsDead)
        {
            UpdateSpriteDirection(targetCell);

            if (_animator != null)
                _animator.SetTrigger("Attack");

            yield return WaitGameSeconds(0.25f);

            if (!IsDead && Player.GridPosition == targetCell)
            {
                int damage = RollAttackDamage();
                TryHitPlayer(damage);
            }
        }

        ClearTelegraphs();
        _attackActionPoints = 0f;
        _isAttacking = false;
        _pendingAttackCell = null;
    }

    protected virtual void CheckAttackTelegraph()
    {
        if (IsDead || _isAttacking)
            return;

        if (_animator != null)
            _animator.SetBool("Walk", false);

        if (IsAdjacentToPlayer())
        {
            isDetected = true;
            _pendingAttackCell = Player.GridPosition;
            UpdateSpriteDirection(_pendingAttackCell.Value);
            StartCoroutine(ExecutePendingAttack(_pendingAttackCell.Value, 0.5f));
        }
    }

    protected void UpdateSpriteDirection(Vector2Int targetPos)
    {
        if (SpriteRenderer == null)
            return;

        if (targetPos.x < GridPosition.x)
            SpriteRenderer.flipX = true;
        else if (targetPos.x > GridPosition.x)
            SpriteRenderer.flipX = false;
    }

    protected bool IsSafeRoomCell(Vector2Int cell)
    {
        if (MapGenerator.Instance == null || MapGenerator.Instance.CurrentRooms == null)
            return false;

        for (int i = 0; i < MapGenerator.Instance.CurrentRooms.Count; i++)
        {
            var room = MapGenerator.Instance.CurrentRooms[i];
            if (room.type == RoomType.Safe)
                return room.floorCells.Contains(cell);
        }

        return false;
    }

    protected bool IsAdjacentToPlayer()
    {
        var diff = Player.GridPosition - GridPosition;
        return Mathf.Abs(diff.x) + Mathf.Abs(diff.y) == 1;
    }

    protected float SquaredDistanceToPlayer()
    {
        if (SafeRoomTimerGate.PlayerInSafeRoom)
            return float.MaxValue;

        var diff = Player.GridPosition - GridPosition;
        return diff.x * diff.x + diff.y * diff.y;
    }

    private void PlayDeathAnimation()
    {
        if (_animator == null)
            return;

        _animator.SetBool("Walk", false);

        if (string.IsNullOrEmpty(deathTriggerName))
            return;

        if (!HasAnimatorParameter(deathTriggerName, AnimatorControllerParameterType.Trigger))
            return;

        _animator.speed = 1f;
        _animator.SetTrigger(deathTriggerName);
    }

    private IEnumerator DestroyAfterDeath()
    {
        if (deathDestroyDelay > 0f)
            yield return new WaitForSeconds(deathDestroyDelay);

        Destroy(gameObject);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
            return false;

        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.type == parameterType && parameter.name == parameterName)
                return true;
        }

        return false;
    }
}

public enum DamageType
{
    Melee,
    Ranged,
    StatusEffect
}

public static class DamageContext
{
    public static DamageType CurrentDamageType = DamageType.Melee;
}
