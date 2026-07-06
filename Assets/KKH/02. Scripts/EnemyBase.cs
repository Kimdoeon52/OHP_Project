using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 모든 적 캐릭터의 최상위 공통 베이스 클래스.
///
/// 담당 범위:
///   - 격자 이동 / 시야 갱신 / 텔레그래프 생성·해제
///   - 턴 매니저(TurnManager) 연동 (등록/해제)
///   - HP 관리, 데미지 판정, 사망 처리
///   - 애니메이션 파라미터 제어 (Walk / Hit / Die / Attack)
///   - 상태이상 적용 (TryApplyOnHitEffect)
///
/// 협업 포인트:
///   새 적 종류 추가 시 이 클래스를 상속받고 필요한 메서드만 override 하면 됨.
///   세 개의 템플릿 메서드(ExecutePendingAttack / PerformMovement / CheckAttackTelegraph)가
///   OnTurnUpdate에서 순서대로 호출되는 구조이므로, 커스텀 행동이 필요한 단계만 override.
/// </summary>
public abstract class EnemyBase : EntityBase, IDamageable
{
    [Header("설정 및 에셋 데이터")]
    [Tooltip("적 스탯·보상·시야 범위 등을 담고 있는 ScriptableObject. 인스펙터에서 반드시 할당해야 함.")]
    [SerializeField] protected EnemyData data;

    [Header("공격 예고 (오브젝트 방식)")]
    [Tooltip("공격 예고 위치에 생성할 붉은 바닥 프리팹")]
    [SerializeField] protected GameObject warningPrefab;

    [Header("피격 연출")]
    [Tooltip("피격 당할 때 터질 파티클 프리팹 (예: GreenBlood_Hit_Effect)")]
    [SerializeField] protected GameObject hitParticlePrefab;

    [Header("사망 연출")]
    [Tooltip("사망 시 터질 파티클 프리팹 (예: Death_Gib_Effect)")]
    [SerializeField] protected GameObject deathEffectPrefab;

    [Header("보상 드롭")]
    [Tooltip("처치 시 떨어뜨릴 골드 픽업 프리팹 (Gold_Pickup). 플레이어가 주워야 지갑에 들어감.")]
    [SerializeField] private GoldPickup goldPickupPrefab;
    [Tooltip("처치 시 떨어뜨릴 아이템 픽업 프리팹 (ItemPickUp). EnemyData.itemDrops 에 든 아이템(체력 포션 등) 스폰용.")]
    [SerializeField] private ItemPickup dropItemPickupPrefab;

    [Header("생명 파편 (확정 드롭)")]
    [Tooltip("사망 시 100% 확률로 무조건 떨어뜨릴 생명 파편 아이템")]
    [SerializeField] protected ItemBase fragmentsOfLifeItem;

    [Header("애니메이션 관련 설정")]
    [Tooltip("Animator에서 사망 트리거 파라미터 이름. 기본값 \"Die\".")]
    [SerializeField] private string deathTriggerName = "Die";

    [Tooltip("Die 트리거 발동 후 오브젝트가 실제로 Destroy되기까지의 대기 시간 (초).")]
    [SerializeField, Min(0f)] private float deathDestroyDelay = 0.6f;

    // ── IDamageable 인터페이스 구현 ──
    /// <summary> 현재 남은 체력 </summary>
    public int CurrentHp { get; protected set; }
    /// <summary> 최대 체력 (EnemyData.maxHp, data 없으면 기본값 1) </summary>
    public int MaxHp => data != null ? data.maxHp : 1;
    /// <summary> CurrentHp가 0 이하이면 true </summary>
    public bool IsDead => CurrentHp <= 0;

    // ── 외부 호환 필드 (YJW 무기·아이템 스크립트 등 타 팀원 접근용) ──
    [Header("외부 호환 상태")]
    [Tooltip("이동속도 필드. YJW 무기 시스템 호환용으로 public 유지.")]
    public float moveSpeed;
    [Tooltip("감지 여부 플래그. 외부 스크립트에서 isDetected를 읽어 감지 연출에 활용 가능.")]
    public bool isDetected = false;

    // ── 컴포넌트 내부 참조 ──
    /// <summary> 씬 내 GridPlayer 단일 인스턴스 참조. Start()에서 한 번만 탐색해 캐싱. </summary>
    protected GridPlayer Player { get; private set; }
    /// <summary> 자신 또는 자식 오브젝트에서 캐싱한 Animator. 없으면 null. </summary>
    protected Animator _animator;
    private StatusController _statusController;
    /// <summary> 중복 사망 처리 방지 플래그 </summary>
    private bool _isDying;
    /// <summary> 경보 발동 여부 </summary>
    private bool isAlerted = false;
    /// <summary> 경보 발생 시 목표 지점 </summary>
    private Vector2Int? _alarmTarget = null;

    // ── 전투 내부 필드 ──
    /// <summary>
    /// 다음 턴에 공격할 격자 좌표 예약지.
    /// null이면 예약된 공격 없음.
    /// 자식 클래스가 CheckAttackTelegraph에서 값을 쓰고, ExecutePendingAttack에서 소비.
    /// </summary>
    protected Vector2Int? _pendingAttackCell;
    protected bool _isAttacking = false;

    // ── 액션 포인트 (AP) ──
    // 플레이어가 행동할 때마다 그 행동 비용(초)만큼 누적됨.
    // 누적값이 적의 이동 비용 / 공격 비용에 도달하면 행동 1회 수행.
    /// <summary> 이동용 AP 누적기 </summary>
    protected float _moveActionPoints = 0f;
    /// <summary> 공격용 AP 누적기 </summary>
    protected float _attackActionPoints = 0f;

    /// <summary> 현재 활성화된 텔레그래프 인스턴스 목록. Die() 시 일괄 정리. </summary>
    private readonly Dictionary<Vector2Int, GameObject> _activeTelegraphs = new();
    /// <summary> 자식에서 data에 안전 접근할 수 있는 읽기 전용 프로퍼티 </summary>
    public EnemyData Data => data;

    // ══════════════ 라이프사이클 ══════════════

    protected override void Awake()
    {
        base.Awake();

        // Animator 자동 캐싱 (자식 오브젝트까지 탐색)
        _animator = GetComponent<Animator>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        _statusController = GetComponent<StatusController>();
        if (_statusController == null)
            _statusController = gameObject.AddComponent<StatusController>();

        if (data == null)
        {
            Debug.LogError($"{name}: EnemyData가 할당 안 됨. 인스펙터 확인 필요.");
            return;
        }

        // 초기 HP 세팅
        CurrentHp = data.maxHp;

        // data에 스프라이트 있으면 SpriteRenderer에 자동 적용
        if (data.sprite != null) SpriteRenderer.sprite = data.sprite;
    }

    private void OnEnable()
    {
        // VisionSystem 갱신 이벤트 구독 → 안개/시야 내 가시성 자동 갱신
        GameEvents.OnVisionUpdated += OnVisionUpdated;

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.Register(this);
        }
    }

    private void OnDisable()
    {
        // 메모리 누수 방지: 씬 제거 또는 오브젝트 비활성화 시 구독 해제
        GameEvents.OnVisionUpdated -= OnVisionUpdated;

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.Unregister(this);
        }
    }

    protected virtual void Start()
    {
        // 씬 내 플레이어 탐색. 없으면 에러 출력 후 조기 리턴.
        Player = FindFirstObjectByType<GridPlayer>();
        if (Player == null)
        {
            Debug.LogError($"{name}: 씬에 GridPlayer 없음. 플레이어를 먼저 배치해야 함.");
            return;
        }

        // 에디터에서 배치한 월드 좌표 → 격자 좌표 스냅 초기화
        var startPos = GridSystem.Instance.WorldToGrid(transform.position);
        Initialize(startPos);

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.Register(this);
        }

        // 초기 안개 가시성 상태 적용
        UpdateVisibility();
    }

    // ══════════════ 시야 (Vision) ══════════════

    /// <summary>
    /// VisionSystem이 시야를 재계산할 때 호출되는 이벤트 핸들러.
    /// 이 적이 시야 범위 안에 있는지 여부를 갱신.
    /// </summary>
    private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible, IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    /// <summary>
    /// 이 적이 현재 VisionSystem 시야 내에 있으면 SpriteRenderer 활성화,
    /// 아니면 비활성화. 소속 텔레그래프도 동기화.
    /// </summary>
    protected virtual void UpdateVisibility()
    {
        bool visible = VisionSystem.Instance != null && VisionSystem.Instance.IsVisible(GridPosition);

        SpriteRenderer.enabled = visible;

        // Tilemap 방식 : 워닝 타일은 항상 표시
    }

    // ══════════════ HP / 데미지 처리 ══════════════

    /// <summary>
    /// 피해 처리 메서드.
    /// 방어력 계산 후 HP 차감, 피격 애니메이션 재생, 사망 여부 판정.
    /// </summary>
    /// <param name="amount">적용 전 원시 피해량</param>
    public virtual void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        // 방어력 적용 후 최종 피해 산출
        int finalDamage = ApplyDefense(amount);
        CurrentHp -= finalDamage;

        if (finalDamage > 0)
        {
            GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.MonsterHit);

            // 피격 파티클 시스템 스폰
            GameEvents.OnPlayVFX?.Invoke(transform.position, VFXType.MonsterHit);
        }

        // 살아있으면 Hit 트리거 실행 (흔들림 / 피격 연출)
        if (_animator != null && CurrentHp > 0)
        {
            _animator.SetTrigger("Hit");
        }

        if (CurrentHp <= 0) Die();
    }

    /// <summary>
    /// 데미지 타입을 포함한 피해 처리 메서드.
    /// </summary>
    public virtual void TakeDamage(int amount, DamageType damageType)
    {
        TakeDamage(amount);
    }

    /// <summary>
    /// HP 회복. MaxHp 초과 안 됨.
    /// </summary>
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
    }

    /// <summary>
    /// 사망 처리.
    /// 순서: 코루틴 정지 → 텔레그래프 정리 → TurnManager 해제 → 이벤트 발행 → 사망 애니메이션 → Destroy 예약.
    ///
    /// 이벤트 발행 대상:
    ///   GameEvents.OnEnemyKilled   → 처치 좌표 + 시간 보너스
    ///   GameEvents.OnExpDropped    → 경험치
    ///   GameEvents.OnGoldDropped   → 골드 (goldMax > 0일 때만)
    /// </summary>
    protected virtual void Die()
    {
        if (_isDying) return; // 중복 실행 방지

        _isDying = true;
        CurrentHp = 0;

        // 사망 파티클 이펙트 생성
        GameEvents.OnPlayVFX?.Invoke(transform.position, VFXType.MonsterDeath);

        StopAllCoroutines();
        ClearTelegraphs();

        // 턴 루프에서 제외
        TurnManager.Instance.Unregister(this);

        // ── 이벤트 발행 ──
        // UI, 사운드, 타임 보너스 등 구독자들이 처리
        GameEvents.OnEnemyKilled?.Invoke(GridPosition, data.timeBonus);
        GameEvents.OnExpDropped?.Invoke(GridPosition, data.expReward);

        // 골드·아이템은 바닥 픽업으로 떨어뜨린다 — 플레이어가 주워야 획득 (상자/맵 드롭과 동일 계약).
        DropLoot();

        // TimeSystem에 남은 시간 추가
        TimeSystem.Instance.AddTime(data.timeBonus);

        PlayDeathAnimation();
        StartCoroutine(DestroyAfterDeath());
    }

    // 처치 드롭 (골드 / 아이템)
    private void DropLoot()
    {
        if (data == null) return;

        if (data.goldMax > 0 && Random.value <= data.goldDropChance)
        {
            int gold = Random.Range(data.goldMin, data.goldMax + 1);
            if (gold > 0) SpawnGoldDrop(gold);
        }

        // 아이템 — 항목별 확률 판정
        if (data.itemDrops != null)
        {
            foreach (var drop in data.itemDrops)
            {
                if (drop == null || drop.item == null) continue;
                if (Random.value > drop.chance) continue;
                for (int i = 0; i < drop.count; i++)
                    SpawnItemDrop(drop.item);
            }
        }

        // 생명 파편 무조건(100% 확률) 드롭 — 파편 봉인 챌린지 시 드롭 없음
        if (fragmentsOfLifeItem != null && !ChallengeState.Has(ChallengeType.FragmentSeal))
        {
            SpawnItemDrop(fragmentsOfLifeItem);
        }
    }

    private void SpawnGoldDrop(int amount)
    {
        if (goldPickupPrefab == null)
        {
            Debug.LogWarning($"{name}: goldPickupPrefab 미할당 — 골드 {amount} 드롭 실패. 적 프리팹에 Gold_Pickup 을 연결하세요.");
            return;
        }
        GoldPickup pickup = Instantiate(goldPickupPrefab, WorldOf(GridPosition), Quaternion.identity);
        pickup.Initialize(GridPosition, amount);
    }

    private void SpawnItemDrop(ItemBase item)
    {
        var groundItemController = FindFirstObjectByType<PrototypeRT.GroundItemController>();
        ItemInstance itemInst = item.CreateInstance();

        // 장비 아이템의 경우 강화도 및 저주 롤링 적용
        if (itemInst is EquipmentInstance equipInst)
        {
            DungeonRandom rng = MapGenerator.Instance != null ? MapGenerator.Instance.Rng : new DungeonRandom();
            if (rng.Seed == 0 && MapGenerator.Instance == null)
            {
                rng.Init(Random.Range(1, 100000));
            }

            // 등장 강화도 확률 롤링 (0~3강)
            equipInst.enhanceLevel = ItemRoll.Enhance(rng);
            // 장비 슬롯 매칭 저주 롤링
            equipInst.curse = RollCurseForEquipment(equipInst, rng);
            // 기본 미감정
            equipInst.isAnalyzed = false;
        }

        if (groundItemController != null)
        {
            groundItemController.DropItem(itemInst, 1, GridPosition);
        }
        else 
        {
            if (dropItemPickupPrefab == null)
            {
                Debug.LogWarning($"{name}: {item.name} 드롭 실패. Item_Pickup을 연결해주세요.");
                return;
            }
            ItemPickup pickup = Instantiate(dropItemPickupPrefab, WorldOf(GridPosition), Quaternion.identity);
            pickup.item = itemInst; // 장비 롤링 상태를 보존하기 위해 인스턴스 직접 주입
            pickup.itemSO = item;
            pickup.Initialize(GridPosition);
        }
    }

    private CurseData RollCurseForEquipment(EquipmentInstance equipInst, DungeonRandom rng)
    {
        // 저주는 근접무기·방어구에만 붙는다 (투척무기 등 제외)
        if (!ItemRoll.IsCurseEligible(equipInst.data)) return null;

        // 50% 확률로 저주 없음
        if (rng.Value() > 0.5f) return null;

        var database = ItemDatabase.Instance;
        if (database == null) return null;

        EquipmentSlot slot = equipInst.data.Slot;

        // 전체 저주 목록 중에서 이 장비의 슬롯과 일치하는 저주 필터링
        List<CurseData> matchedCurses = new List<CurseData>();
        foreach (var curse in database.Curses)
        {
            if (curse != null && curse.TargetSlot == slot)
            {
                matchedCurses.Add(curse);
            }
        }

        if (matchedCurses.Count == 0) return null;

        return rng.Pick(matchedCurses);
    }

    private static Vector3 WorldOf(Vector2Int cell)
        => GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(cell) : new Vector3(cell.x, cell.y, 0);

    // ══════════════ 텔레그래프 (공격 범위 예고) ══════════════

    /// <summary>
    /// 지정 격자에 공격 예고 타일을 생성.
    /// _activeTelegraphs에 등록되어 ClearTelegraphs / UpdateVisibility에서 일괄 관리됨.
    /// </summary>
    protected void ShowTelegraph(Vector2Int cell)
    {
        if (warningPrefab == null) return; // 프리팹이 할당되어 있지 않으면 즉시 리턴
        if (_activeTelegraphs.ContainsKey(cell)) return; // 이미 예고가 뜬 셀이면 리턴
        // 격자 좌표 -> 월드 좌표 변환
        Vector3 worldPos = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);
        // 프리팹 스폰 후 관리 목록에 등록
        GameObject indicator = Instantiate(warningPrefab, worldPos, Quaternion.identity);

        // TelegraphIndicator가 있다면 Show를 호출하여 내부 오프셋 반영 및 시야 업데이트
        TelegraphIndicator ti = indicator.GetComponent<TelegraphIndicator>();
        if (ti != null)
        {
            ti.Show(cell);
        }

        _activeTelegraphs.Add(cell, indicator);
    }

    /// <summary>
    /// 현재 활성화된 텔레그래프 인스턴스 전부 Destroy 후 목록 초기화.
    /// Die() 및 ExecutePendingAttack() 후반부에서 호출.
    /// </summary>
    protected void ClearTelegraphs()
    {
        foreach (var kvp in _activeTelegraphs)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value); // 오브젝트 제거 (풀링을 쓰신다면 풀에 반환)
            }
        }
        _activeTelegraphs.Clear();
    }

    // ══════════════ 격자 기반 공통 이동 로직 ══════════════

    /// <summary>
    /// 플레이어 방향으로 1칸 접근 시도.
    /// X/Y 차이가 더 큰 축을 주축(Primary)으로 먼저 시도하고, 막히면 부축(Secondary)으로 우회.
    /// </summary>
    
    protected void TryStepToward(Vector2Int targetPos)
    {
        var diff = targetPos - GridPosition;

        var dirX = diff.x != 0 ? new Vector2Int(System.Math.Sign(diff.x), 0) : Vector2Int.zero;
        var dirY = diff.y != 0 ? new Vector2Int(0, System.Math.Sign(diff.y)) : Vector2Int.zero;

        // 더 먼 축을 주축으로 설정
        bool xPrimary = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);
        var primary = xPrimary ? dirX : dirY;
        var secondary = xPrimary ? dirY : dirX;

        if (primary != Vector2Int.zero && TryStep(primary)) return;
        if (secondary != Vector2Int.zero && TryStep(secondary)) return;
    }

    protected void TryStepTowardPlayer()
    {
        TryStepToward(Player.GridPosition);
    }

    protected virtual void PerformMovement()
    {
        if (IsDead) return;

        // 이번 턴에 경보가 켜졌다면 플레이어 위치 기억 (세이프룸 안이면 무시)
        // 실시간 무한 동기화가 아닌, 마지막으로 감지된 최초 1회(또는 플레이어가 시야 밖으로 사라진 시점의 위치)로 설정하여 맵 끝까지 쫓아오는 버그를 차단합니다.
        if(GameManager.Instance != null && GameManager.Instance.isAlerted && !SafeRoomTimerGate.PlayerInSafeRoom)
        {
            if (_alarmTarget == null)
            {
                _alarmTarget = Player.GridPosition;
            }
        }

        // 시야 안에 플레이어가 있고 
        if (SquaredDistanceToPlayer() <= data.visionRange*data.visionRange)
        {
            TryStepTowardPlayer();
            _alarmTarget = null;
            return;
        }

        if(_alarmTarget != null)
        {
            Vector2Int targetCell = _alarmTarget.Value;

            // 플레이어와 충분히 가까워졌으면 다시 목표물로 설정
            if(GridPosition != targetCell)
            {
                TryStepToward(targetCell);
                return;
            }
            else
            {
                // 이미 목표 지점이면 경보 해제
                _alarmTarget = null;
            }
        }
        
    }

    /// <summary>
    /// 지정 방향으로 1칸 이동 시도.
    /// 통과 불가 조건: 벽 / 플레이어 점유 / 타 엔티티 점유.
    /// 이동 성공 시 논리 좌표 갱신 + 시야 갱신 + 스프라이트 방향 갱신 + Walk 애니메이션 켬.
    /// </summary>
    /// <param name="dir">이동 방향 벡터</param>
    /// <returns>이동 성공 여부</returns>
    protected virtual bool TryStep(Vector2Int dir)
    {
        var next = GridPosition + dir;

        if (IsSafeRoomCell(next)) return false;                   // 세이프룸 침범 방지

        if (!GridSystem.Instance.IsWalkable(next)) return false;  // 벽
        if (next == Player.GridPosition) return false;            // 플레이어 위치
                                                                  //  if (TurnManager.Instance.GetEntityAt(next) != null) return false; // 타 엔티티
        if (GridSystem.Instance.IsCellOccupied(next)) return false; // 타 엔티티

        if (_animator != null) _animator.SetBool("Walk", true);

        SetGridPosition(next);
        TrapTerrainController.TryApplyTerrainAt(this, next);
        UpdateVisibility();
        UpdateSpriteDirection(next);
        return true;
    }

    // ══════════════ 수치 및 전투 판정 헬퍼 ══════════════

    /// <summary>
    /// attackMin~attackMax 범위에서 랜덤 공격력 롤.
    /// </summary>
    protected virtual int RollAttackDamage()
    {
        return Random.Range(data.attackMin, data.attackMax + 1);
    }

    /// <summary>
    /// 방어력 적용 후 최종 피해 산출. 최소 0.
    /// </summary>
    protected int ApplyDefense(int rawDamage)
    {
        int defense = Random.Range(data.defenseMin, data.defenseMax + 1);
        return Mathf.Max(0, rawDamage - defense);
    }

    /// <summary>
    /// GDD 기준 명중 판정 롤.
    /// 공격 무효 확률은 (대상 회피 - 공격자 정확도)이며, 최대 50%까지만 허용합니다.
    /// </summary>
    /// <param name="targetEvasion">대상의 회피 스탯</param>
    /// <returns>명중이면 true</returns>
    protected bool RollHit(int targetEvasion)
    {
        int attackerAccuracy = data != null ? data.accuracy : 0;
        int missChancePercent = Mathf.Clamp(targetEvasion - attackerAccuracy, 0, 50);

        return Random.Range(0, 100) >= missChancePercent;
    }

    /// <summary>
    /// 몬스터가 플레이어에게 실제 공격을 적용하는 공통 진입점.
    /// 회피는 공격자의 정확도가 필요하기 때문에 플레이어 TakeDamage 내부가 아니라 여기서 먼저 판정합니다.
    /// 명중한 경우에만 데미지와 EnemyData.onHitEffect가 적용됩니다.
    /// </summary>
    /// <param name="rawDamage">방어력 적용 전 원시 피해량</param>
    /// <returns>명중해서 피해/명중 효과 처리를 시도했으면 true, 빗나가거나 대상이 없으면 false</returns>
    protected bool TryHitPlayer(int rawDamage)
    {
        if (Player == null || Player.IsDead || rawDamage <= 0) return false;

        NYHPlayerStats playerStats = Player.GetComponent<NYHPlayerStats>();
        if (playerStats == null)
            playerStats = NYHPlayerStats.Instance;

        int targetEvasion = playerStats != null ? playerStats.Evasion : 0;
        if (!RollHit(targetEvasion))
        {
            GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(Player.transform.position, "Miss", 0, CombatTextType.Miss));
            Debug.Log($"{name} 공격 빗나감: 정확도 {data?.accuracy ?? 0}, 플레이어 회피 {targetEvasion}");
            return false;
        }

        int criticalReferenceMaxDamage = data != null ? data.attackMax : rawDamage;
        Player.Health.TakeDamageWithCombatText(rawDamage, criticalReferenceMaxDamage);
        TryApplyOnHitEffect(Player);
        return true;
    }

    /// <summary>
    /// 이동 1회에 드는 시간 비용 (초).
    /// data.moveSpeed 값. data 없으면 기본값 1.0.
    /// </summary>
    public float GetMoveCost()
    {
        float cost = data != null ? data.moveSpeed : 1.0f;
        if (_statusController == null)
            _statusController = GetComponent<StatusController>();

        if (_statusController != null)
            cost = _statusController.ModifyMoveTimeCost(cost);

        return cost;
    }

    /// <summary>
    /// 공격 1회에 드는 시간 비용 (초).
    /// 1 / attackSpeed. attackSpeed가 0이거나 data 없으면 기본값 1.0.
    /// </summary>
    public float GetAttackCost()
    {
        float cost = (data != null && data.attackSpeed > 0f) ? (1f / data.attackSpeed) : 1.0f;
        if (_statusController == null)
            _statusController = GetComponent<StatusController>();

        return _statusController != null ? _statusController.ModifyAttackCooldown(cost) : cost;
    }

    /// <summary>
    /// 명중 시 data.onHitEffect에 지정된 상태이상을 대상에게 부여.
    /// onHitEffect가 null이거나 대상이 이미 사망했으면 아무것도 안 함.
    /// </summary>
    protected void TryApplyOnHitEffect(IDamageable target)
    {
        if (data.onHitEffect == null) return;
        if (target == null || target.IsDead) return;
        if (target is not Component component) return;

        var effectData = data.onHitEffect;

        var request = new StatusRequest(
            target as IGridEntity,
            (target as IGridEntity)?.GridPosition ?? GridPosition,
            effectData.statusType,
            effectData.durationTurns,
            effectData.power,
            null // trapSource: 적 공격이므로 null
        );

        StatusController controller = component.GetComponent<StatusController>();
        if (controller == null)
            controller = component.gameObject.AddComponent<StatusController>();

        controller.ApplyStatus(request);

        // 상태이상 전역 이벤트 발행 (사운드 / UI 텍스트 구독자용)
        GameEvents.OnStatusEffectApplied?.Invoke(request);
    }

    // ══════════════ 템플릿 메서드 패턴 ══════════════
    // OnTurnUpdate → ExecutePendingAttack → PerformMovement → CheckAttackTelegraph 순으로 진행.
    // 각 단계를 override해서 적마다 다른 행동 구현.

    /// <summary>
    /// TurnManager가 매 플레이어 행동 후 호출하는 진입점.
    ///
    /// 처리 순서:
    ///   1. 플레이어 행동 비용(초)만큼 이동 AP / 공격 AP 누적
    ///   2. AP가 공격 비용 충족 → ExecutePendingAttack (이전 턴에 예약된 공격 발동)
    ///   3. AP가 이동 비용 충족할 때마다 PerformMovement (느린 적도 정확히 1칸만 이동)
    ///   4. AP가 공격 비용 충족 → CheckAttackTelegraph (다음 턴 공격 예고)
    ///   5. 이동 여부에 따라 Walk 애니메이션 파라미터 리셋
    /// </summary>
    protected virtual void Update()
    {
        if (IsDead || Player == null) return;

        // 시간 정지 중(Safe Room 대기 등)에는 행동하지 않음
        if (TimeSystem.Instance == null || !TimeSystem.Instance.IsRunning) return;

        // 플레이어 행동 비용만큼 AP 충전
        _moveActionPoints += Time.deltaTime;
        _attackActionPoints += Time.deltaTime;


        // 1단계: 이동 (이동 비용 충족 시 반복 가능, 단 느린 적은 최대 1회)
        float moveCost = GetMoveCost();

        if (_moveActionPoints >= moveCost)
        {
            PerformMovement();
            _moveActionPoints -= moveCost;
        }

        // 2단계: 다음 공격 예고
        float attackCost = GetAttackCost();
        if (_attackActionPoints >= attackCost)
        {
            CheckAttackTelegraph();
            _attackActionPoints -= attackCost;
        }
    }


    /// <summary>
    /// 자식 클래스가 SmoothMoveVisual 등 커스텀 보간 이동 코루틴을 직접 Walk 파라미터를 제어하는 경우 true 반환.
    /// true이면 부모가 Walk를 강제 리셋하지 않음.
    /// </summary>
    protected virtual bool HasCustomMovementVisual()
    {
        return false;
    }

    /// <summary>
    /// 이전 턴에 예약된 공격(_pendingAttackCell)을 발동.
    /// 플레이어가 여전히 예고 타일에 있으면 데미지 부여.
    /// 완료 후 _pendingAttackCell 초기화 + 텔레그래프 정리.
    /// </summary>
    protected virtual IEnumerator ExecutePendingAttack(Vector2Int targetCell, float delaySeconds)
    {
        _isAttacking = true;
        ShowTelegraph(targetCell);

        yield return new WaitForSeconds(delaySeconds);

        if (!IsDead)
        {
            // 공격 방향으로 스프라이트 전환
            UpdateSpriteDirection(targetCell);

            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }

            yield return new WaitForSeconds(0.25f);
            // 공격 타일에 플레이어가 있으면 데미지 + 상태이상 적용
            if (Player.GridPosition == targetCell)
            {
                int damage = RollAttackDamage();
                TryHitPlayer(damage);
            }
        }

        ClearTelegraphs();
        _attackActionPoints = 0f; // 공격 행동 포인트 초기화 (쿨타임 보정)
        _isAttacking = false;
        _pendingAttackCell = null;
    }




    /// <summary>
    /// 다음 턴 공격 범위 예고 메서드.
    /// 기본 동작: 플레이어와 인접(맨해튼 거리 1)이면 텔레그래프 생성 + _pendingAttackCell 설정.
    /// 원거리 공격 적은 자식 클래스에서 override.
    /// </summary>
    protected virtual void CheckAttackTelegraph()
    {
        if (IsDead || _isAttacking) return;

        if (_animator != null) _animator.SetBool("Walk", false);

        if (IsAdjacentToPlayer())
        {
            _pendingAttackCell = Player.GridPosition;
            UpdateSpriteDirection(_pendingAttackCell.Value);
            StartCoroutine(ExecutePendingAttack(_pendingAttackCell.Value, 0.5f));
            ShowTelegraph(_pendingAttackCell.Value);
        }
    }

    /// <summary>
    /// targetPos 방향에 맞춰 SpriteRenderer.flipX 설정.
    /// 타겟이 왼쪽이면 flipX = true, 오른쪽이면 flipX = false.
    /// </summary>
    protected void UpdateSpriteDirection(Vector2Int targetPos)
    {
        if (SpriteRenderer == null) return;

        if (targetPos.x < GridPosition.x)
        {
            SpriteRenderer.flipX = true;   // 왼쪽 바라봄
        }
        else if (targetPos.x > GridPosition.x)
        {
            SpriteRenderer.flipX = false;  // 오른쪽 바라봄 (기본)
        }
    }

    // ══════════════ 내부 판정 유틸리티 ══════════════

    /// <summary>
    /// 지정 격자가 세이프룸 영역(Safe Room)인지 판정합니다.
    /// </summary>
    protected bool IsSafeRoomCell(Vector2Int cell)
    {
        if (MapGenerator.Instance == null || MapGenerator.Instance.CurrentRooms == null)
            return false;

        for (int i = 0; i < MapGenerator.Instance.CurrentRooms.Count; i++)
        {
            var room = MapGenerator.Instance.CurrentRooms[i];
            if (room.type == RoomType.Safe)
            {
                return room.floorCells.Contains(cell);
            }
        }
        return false;
    }

    /// <summary>
    /// 플레이어와 상하좌우 1칸 인접 여부 판정 (맨해튼 거리 == 1).
    /// </summary>
    protected bool IsAdjacentToPlayer()
    {
        var diff = Player.GridPosition - GridPosition;
        return Mathf.Abs(diff.x) + Mathf.Abs(diff.y) == 1;
    }

    /// <summary>
    /// 플레이어와의 제곱 거리 계산.
    /// sqrt 없이 거리 비교에 사용 (성능 절약).
    /// </summary>
    protected float SquaredDistanceToPlayer()
    {
        // 세이프룸 안 플레이어는 감지 불가 — 모든 적 시야 판정이 이 값을 쓰므로 한 곳에서 차단
        if (SafeRoomTimerGate.PlayerInSafeRoom)
            return float.MaxValue;

        var diff = Player.GridPosition - GridPosition;
        return diff.x * diff.x + diff.y * diff.y;
    }

    /// <summary>
    /// 사망 애니메이션 트리거 안전 실행.
    /// Animator가 없거나 파라미터가 미등록이면 아무것도 안 함.
    /// </summary>
    private void PlayDeathAnimation()
    {
        if (_animator == null) return;

        // Walk 루프 강제 종료
        _animator.SetBool("Walk", false);

        if (string.IsNullOrEmpty(deathTriggerName)) return;
        if (!HasAnimatorParameter(deathTriggerName, AnimatorControllerParameterType.Trigger)) return;

        _animator.speed = 1f;
        _animator.SetTrigger(deathTriggerName);
    }

    /// <summary>
    /// deathDestroyDelay(초) 대기 후 이 게임오브젝트를 Destroy.
    /// PlayDeathAnimation()과 짝을 이루어 Die()에서 시작됨.
    /// </summary>
    private IEnumerator DestroyAfterDeath()
    {
        if (deathDestroyDelay > 0f)
            yield return new WaitForSeconds(deathDestroyDelay);

        Destroy(gameObject);
    }

    /// <summary>
    /// Animator에 특정 이름의 파라미터가 실제로 등록되어 있는지 검증.
    /// 없는 파라미터에 SetTrigger 등을 날리면 Unity가 Warning 뿜으므로 사전 체크.
    /// </summary>
    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null) return false;

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

