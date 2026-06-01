using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;
using DG.Tweening;

/*
 * BossA — 시간 기반 생존형 보스 (눈알 보스).
 * 제한 시간 120초 동안 살아남기. 페이즈는 HP가 아니라 TimeSystem.TimeRemaining으로 전환.
 *   120~80초 : 일반 패턴 (플레이어 방향 직선 레이저 / 굵은 직선 레이저)
 *   80 ~20초 : 분노 패턴 (가로5 / 세로5 / 가로3+세로3 랜덤 레이저)
 *   20 ~ 0초 : 죽음 패턴 (중앙으로 이동 + 10초간 4방향 회전 붉은 레이저, 닿으면 즉사)
 *   패턴은 patternDelay(=2초) 간격으로 실행.
 */
public class BossA : BossBase, IGridEntity, IDamageable
{
    [Header("레이저 프리팹")]
    [SerializeField] private GameObject lazerAttackPrefab; //레이저임
    [SerializeField] private GameObject smallEyePrefab;     //작은 눈알 프리팹

    [Header("레이저 타이밍 (patternDelay와 분리)")]
    [SerializeField] private int laserTelegraphMs = 800; // 경고 알려주는 시간
    [SerializeField] private int laserActiveMs = 400;    // 실제 레이저 발사 시간

    [Header("레이저 판정")]
    [SerializeField] private int laserDamage = 1;         // 일반 레이저 피해
    [SerializeField] private float thickLaserScaleY = 3f; // 굵은 레이저 크기
    [SerializeField] private int thickLaserWidth = 1;     // 굵은 레이저 폭 (중심선 양옆 칸 수)

    [Header("페이즈 시간 경계 (초)")]
    [SerializeField] private float phase1Until = 80f; // 이 시간보다 많으면 Phase1
    [SerializeField] private float phase2Until = 20f; // 이 시간보다 많으면 Phase2, 이하면 최후의 패턴

    [Header("죽음 패턴 (회전 레이저)")]
    [SerializeField] private float deathLaserDurationSec = 10f; // 회전 지속 시간
    [SerializeField] private float deathLaserRotateSpeed = 60f; // 초당 회전 각도
    [SerializeField] private float deathLaserLength = 30f;      // 레이저 길이
    [SerializeField] private float deathLaserHalfWidth = 0.5f;  // 판정 두께
    [SerializeField] private Color deathLaserColor = Color.red;
    [SerializeField] private int deathInstantKillDamage = 99999;

    [Header("사망 설정")]
    [SerializeField] private int deathAnimationDelayMs = 2000; // 사망 애니메이션 재생 대기 시간 (ms)

    [Header("DOTween 페이드 설정")]
    [SerializeField] private float fadeDuration = 0.5f; // 페이드 연출 시간 (초)

    [Header("보스 공격 범위 타일맵")]
    [SerializeField] private Tilemap attackFloorTilemap; // 보스 공격이 가능한 영역을 정의하는 타일맵

    private RectInt _bossRoomBounds;
    private bool _hasBossRoomBounds;
    private RectInt _mapBounds;        // 전체 맵(통행 가능 셀) 경계 — 가로/세로 레이저 기준
    private bool _hasMapBounds;
    private RectInt _attackFloorBounds; // AttackFloor 타일맵의 바운드 (공격 범위 제한용)
    private bool _hasAttackFloorBounds;
    private bool _deathPhaseStarted;

    private bool _isVanished;          // 보스가 사라진 상태(무적)인지 여부
    private bool _isPattern3Active;    // 패턴 3이 가동 중인지 여부
    private bool _pattern3Cleared;     // 패턴 3이 5초 내에 체력 0으로 파회되었는지 여부
    private int _pattern3DamageTaken;  // 패턴 3 동안 받은 데미지 누적
    private CancellationTokenSource _pattern3CancelSource; // 패턴 3 시간 대기 조기 취소용 토큰
    private Vector2Int gridPos;

    protected override void OnEnable()
    {
        base.OnEnable(); // patternDelay 변환 + 보스전 120초 타이머 시작

        currentHp = maxHp;
        bossLevel = 1;

        _isVanished = false;
        _deathPhaseStarted = false;
        _isPattern3Active = false;

        CacheBossRoom();
        CacheMapBounds();
        CacheAttackFloorBounds();

        // 보스는 존재하지만 눈에 안보임. 특수한 경우에만 모습을 드러냄.
        SetBossVisibility(false);

        gridPos = new Vector2Int(-9999, -9999); // 보스가 사라진 상태(invisible)이므로 격자 밖으로 임시 이동시켜 충돌을 방지
        RegisterBossGridTarget(); // 플레이어가 이동공격 대상으로 보스를 찾을 수 있도록 TurnManager에 등록
        Debug.Log($"보스 시작 위치: {gridPos}"); //디버그용

        // Phase 1 패턴 루프 가동
        Phase1PatternLoopAsync().Forget();
    }

    protected override UniTask PhaseState()
    {
        // BossA는 고유의 독자적인 비동기 패턴 루프를 사용하므로 base.Update에서 호출하는 PhaseState는 비활성화합니다.
        return UniTask.CompletedTask;
    }

    protected override void Update()
    {
        base.Update(); // base.Update에서 디버그 단축키(K, P)를 감지합니다.

        if (isDying) return;

        // 사망조건: 정해진 시간이 끝나면 사망
        if (TimeSystem.Instance != null && TimeSystem.Instance.TimeRemaining <= 0f)
        {
            isDying = true;
            OnDeath();
        }
    }

    #region 플레이어 이동공격 대상 등록 프록시
    // 이 region은 플레이어의 "이동하려는 칸에 적이 있으면 이동 대신 공격" 기능과 연결하기 위한 구간입니다.
    // BossB처럼 IGridEntity를 구현해서 "내가 어느 격자 칸에 있는지" 외부에 알려줍니다.
    // IDamageable을 구현해서 플레이어 공격 코드가 TakeDamage를 호출할 수 있게 합니다.

    public Vector2Int GridPosition => gridPos;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => currentHp <= 0;

    public void TakeDamage(int amount)
    {
        BossTakeDamage(amount);
    }

    public virtual void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }

    public void OnTurnUpdate()
    {
    }

    private void RegisterBossGridTarget()
    {
        if (TurnManager.Instance == null)
        {
            Debug.LogWarning("BossA: TurnManager가 없어 플레이어 이동공격 대상으로 등록하지 못했습니다.");
            return;
        }

        TurnManager.Instance.Register(this);
    }

    private void UnregisterBossGridTarget()
    {
        TurnManager.Instance?.Unregister(this);
    }

    private void OnDisable()
    {
        UnregisterBossGridTarget();
    }
    #endregion

    public override void BossTakeDamage(int amount)
    {
        if (amount <= 0 || isDying) return;

        // 보스가 사라진 상태일 때는 대미지를 받지 않음
        if (_isVanished)
        {
            return;
        }

        currentHp -= amount;

        // 패턴 3 진행 중인 경우 데미지 누적 및 패턴 파회 체크
        if (_isPattern3Active)
        {
            _pattern3DamageTaken += amount;
            if (_pattern3DamageTaken >= 10)
            {
                _pattern3Cleared = true;
                if (_pattern3CancelSource != null)
                {
                    _pattern3CancelSource.Cancel();
                }
                Debug.Log("<color=green>[보스 패턴 파회]</color> 5초 내에 보스에게 10 이상의 데미지를 주어 패턴을 파회했습니다!");
            }
        }

        if (hpBar != null) hpBar.UpdateBar();

        if (currentHp <= 20)
        {
            currentHp = Mathf.Max(0, currentHp);
            isDying = true;

            // 패턴 3이 가동 중이었으면 취소
            if (_isPattern3Active && _pattern3CancelSource != null)
            {
                _pattern3CancelSource.Cancel();
            }

            TriggerPhase3();
        }
        else if (bossLevel == 1 && currentHp <= maxHp * 0.5f)
        {
            TriggerPhase2();
        }
    }

    protected override void OnDeath()
    {
        // 사망 애니메이션 재생
        if (bossAnimation != null)
        {
            bossAnimation.SetTrigger("Death");
        }

        Debug.Log("<color=red>[보스 사망]</color> 정해진 시간이 끝나 보스가 사망하였습니다.");
        DestroyBossAfterDelay().Forget();
    }

    private async UniTask DestroyBossAfterDelay()
    {
        await UniTask.Delay(deathAnimationDelayMs, cancellationToken: this.GetCancellationTokenOnDestroy());
        Destroy(gameObject);
    }

    private void TriggerPhase2()
    {
        bossLevel = 2;
        isBossAttacking = true; // Phase 1 루프 정지

        // 패턴 3 진행 중인 경우 취소
        if (_isPattern3Active && _pattern3CancelSource != null)
        {
            _pattern3CancelSource.Cancel();
        }

        SetBossVisibility(false); // 보스 숨김
        gridPos = new Vector2Int(-9999, -9999); // 격자 위치에서 임시 제거

        // Phase 2 전용 사격 패턴 루프 및 백그라운드 10초 주기 패턴3 루프 시동
        Phase2PatternLoopAsync().Forget();
        Phase2BackgroundLoopAsync().Forget();

        Debug.Log($"<color=orange>[보스 Phase 2 진입]</color> 체력이 50% 이하로 감소하여 폭주 상태 가동.");
    }

    private void TriggerPhase3()
    {
        bossLevel = 3;
        isBossAttacking = true; // 루프 정지 고정
        TriggerPhase3Async().Forget();
    }

    private async UniTaskVoid TriggerPhase3Async()
    {
        Vector3 centerWorld = GetRoomCenterWorld();
        Vector2Int centerCell = Vector2Int.zero;
        if (GridSystem.Instance != null)
        {
            centerCell = GridSystem.Instance.WorldToGrid(centerWorld);
        }

        // 보스가 중앙에 스폰되기 전에 1초간 경고 표시
        ShowWarning(centerCell);
        await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());
        ClearWarning(centerCell);

        // 보스가 중앙에서 모습을 드러내고 4방향 즉사 회전 레이저 가동
        transform.position = centerWorld;
        gridPos = centerCell;
        SetBossVisibility(true, instant: true);
        if (hpBar != null && hpBar.allHpBar != null)
        {
            hpBar.allHpBar.SetActive(false); // Phase 3에서는 체력바 비활성화
        }

        DeathPhaseAsync().Forget();
        Phase3PatternLoopAsync().Forget(); // Phase 3에서도 Phase 2 패턴 1(무작위 가로/세로 레이저)을 계속 진행

        Debug.Log($"<color=red>[보스 Phase 3 진입]</color> 체력이 모두 소진되어 최후의 십자 즉사 레이저 기믹 가동.");
    }

    private async UniTask Phase3PatternLoopAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();
        try
        {
            while (bossLevel == 3)
            {
                // Phase 2 패턴 1: 가로 세로 랜덤 각각 1~2개 동시 레이져 발사 중복위치x
                var tasks = new List<UniTask>();
                tasks.Add(FireRandomHorizontalLasers(Random.Range(1, 3)));
                tasks.Add(FireRandomVerticalLasers(Random.Range(1, 3)));
                await UniTask.WhenAll(tasks);

                await UniTask.Delay(2000, cancellationToken: token);
            }
        }
        catch (System.OperationCanceledException) { }
    }

    //============================ 페이즈1 패턴들 ========================================

    // 120초~80초 패턴 1,2의 딜레이는 각 1초 패턴3은 패턴 파회가 되면 바로 다음 패턴 실행
    private async UniTask Phase1PatternLoopAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();
        int patternIndex = 1;

        try
        {
            while (bossLevel == 1 && !isDying)
            {
                if (patternIndex == 1)
                {
                    // Phase1 패턴1: 가로 랜덤 x축 1~2개 동시 레이져 발사 중복위치x
                    await FireRandomHorizontalLasers(Random.Range(1, 3));
                    await UniTask.Delay(1000, cancellationToken: token);
                    patternIndex = 2;
                }
                else if (patternIndex == 2)
                {
                    // Phase1 패턴2: 세로 랜덤 y축 1~2개 동시 레이져 발사 중복위치x
                    await FireRandomVerticalLasers(Random.Range(1, 3));
                    await UniTask.Delay(1000, cancellationToken: token);
                    patternIndex = 3;
                }
                else if (patternIndex == 3)
                {
                    // phase1 패턴3: 보스의 체력바가 나타나며 보스가 좌측 or 우측에서 등장. 모든 칸에 5초간 경고 표시 후 레이져 발사.
                    await RunPattern3Async();
                    patternIndex = 1;
                }
            }
        }
        catch (System.OperationCanceledException) { }
    }

    // phase1 패턴3
    private async UniTask RunPattern3Async()
    {
        if (isDying) return;

        _isPattern3Active = true;
        _pattern3Cleared = false;
        _pattern3CancelSource = new CancellationTokenSource();

        RectInt bounds = GetActiveBounds();
        int roomCenterY = (bounds.yMin + bounds.yMax) / 2;

        // 보스가 좌측 or 우측에서 등장 (AttackFloor 영역 내 타일 검색)
        int leftmostX = -1;
        int rightmostX = -1;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            Vector2Int testCell = new Vector2Int(x, roomCenterY);
            if (IsInAttackFloor(testCell))
            {
                if (leftmostX == -1) leftmostX = x;
                rightmostX = x;
            }
        }

        Vector2Int spawnCell;
        if (leftmostX != -1 && rightmostX != -1)
        {
            if (Random.Range(0, 2) == 0)
            {
                spawnCell = new Vector2Int(leftmostX, roomCenterY);
            }
            else
            {
                spawnCell = new Vector2Int(rightmostX, roomCenterY);
            }
        }
        else
        {
            // 폴백: 해당 행에 AttackFloor 타일이 없는 경우 바운드 기준 끝부분으로 스폰
            if (Random.Range(0, 2) == 0)
            {
                spawnCell = new Vector2Int(bounds.xMin, roomCenterY);
            }
            else
            {
                spawnCell = new Vector2Int(bounds.xMax - 1, roomCenterY);
            }
        }

        transform.position = GridSystem.Instance.GridToWorld(spawnCell);
        gridPos = spawnCell;

        // 보스 모습 활성화 및 체력바 표시 (패턴3은 즉시 등장해야 하므로 instant=true)
        SetBossVisibility(true, instant: true);
        _pattern3DamageTaken = 0;
        if (hpBar != null) hpBar.UpdateBar();

        // AttackFloor 범위 내 칸에만 경고 표시
        List<Vector2Int> allCells = new List<Vector2Int>();
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var cell = new Vector2Int(x, y);
                if (IsInAttackFloor(cell))
                    allCells.Add(cell);
            }
        }
        ShowWarning(allCells);

        bool expired = false;
        try
        {
            // 모든 칸에 5초간 경고 표시 후 레이져 발사. 플레이어는 이때 보스를 타격할 수 있음.
            await UniTask.Delay(5000, cancellationToken: _pattern3CancelSource.Token);
            expired = true;
        }
        catch (System.OperationCanceledException)
        {
            // 5초가 되기전에 보스의 체력을 0으로 만든다면 패턴 파회 (Token 취소로 조기 탈출)
        }

        // 경고 지우기
        ClearWarning(allCells);

        // 패턴 3 파회 실패 시 방 전체 즉사/고대미지 레이저 격발
        if (expired && !_pattern3Cleared)
        {
            FireRoomWideLaserVisual(bounds).Forget();
            if (player != null && !player.IsDead)
            {
                player.TakeDamage(3);
            }
        }

        // 패턴 3 종료 후 보스 다시 사라짐 및 상태 초기화 (즉시 사라짐)
        SetBossVisibility(false, instant: true);
        gridPos = new Vector2Int(-9999, -9999); // 격자 위치에서 임시 제거
        _isPattern3Active = false;

        if (_pattern3CancelSource != null)
        {
            _pattern3CancelSource.Dispose();
            _pattern3CancelSource = null;
        }
    }

    private async UniTask FireRoomWideLaserVisual(RectInt bounds)
    {
        Vector3 center = GetRoomCenterWorld();
        GameObject lazer = Instantiate(lazerAttackPrefab, center, Quaternion.identity);
        lazer.transform.localScale = new Vector3(bounds.width * 1.5f, bounds.height * 1.5f, 1f);
        await UniTask.Delay(laserActiveMs, cancellationToken: this.GetCancellationTokenOnDestroy());
        if (lazer != null) Destroy(lazer);
    }

    //========================= 보스가 방 위치나 플레이어 위치 등등 가져오는 함수들임 =======================================
    private void CacheBossRoom() //보스방 좌표 가져옴
    {
        if (MapGenerator.Instance?.CurrentRooms == null) return;
        var bossRoom = MapGenerator.Instance.CurrentRooms.Find(r => r.type == RoomType.Boss);
        if (bossRoom == null) return;
        _bossRoomBounds = bossRoom.bounds;
        _hasBossRoomBounds = true;
    }

    private void CacheMapBounds()
    {
        if (GridSystem.Instance == null) return;
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        bool any = false;
        foreach (var c in GridSystem.Instance.GetAllWalkableCells())
        {
            any = true;
            if (c.x < minX) minX = c.x;
            if (c.y < minY) minY = c.y;
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }
        if (!any) return;
        _mapBounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        _hasMapBounds = true;
    }

    private Vector3 GetRoomCenterWorld() //보스 월드 좌표 가져옴
    {
        RectInt bounds = GetActiveBounds();
        if (GridSystem.Instance != null)
        {
            int cx = (bounds.xMin + bounds.xMax) / 2;
            int cy = (bounds.yMin + bounds.yMax) / 2;
            return GridSystem.Instance.GridToWorld(new Vector2Int(cx, cy));
        }
        return transform.position;
    }

    private Vector2Int DirToPlayer4(Vector2Int from)
    {
        if (player == null)
        {
            player = FindFirstObjectByType<GridPlayer>();
        }
        if (player == null) return Vector2Int.right;
        Vector2Int diff = player.GridPosition - from;
        if (diff == Vector2Int.zero) return Vector2Int.right;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            return diff.x > 0 ? Vector2Int.right : Vector2Int.left;
        }
        else
        {
            return diff.y > 0 ? Vector2Int.up : Vector2Int.down;
        }
    }

    private Vector2Int WalkToWall(Vector2Int origin, Vector2Int dir)
    {
        var current = origin;
        int guard = 100;
        while (guard-- > 0 && GridSystem.Instance.IsWalkable(current + dir))
            current += dir;
        return current;
    }

    private RectInt GetActiveBounds()
    {
        if (!_hasAttackFloorBounds) CacheAttackFloorBounds();
        if (!_hasBossRoomBounds) CacheBossRoom();
        if (!_hasMapBounds) CacheMapBounds();

        // AttackFloor 타일맵이 있으면 그 바운드를 최우선 사용
        if (_hasAttackFloorBounds) return _attackFloorBounds;
        if (_hasBossRoomBounds) return _bossRoomBounds;
        if (_hasMapBounds) return _mapBounds;

        return new RectInt(0, 0, 10, 10);
    }

    /// <summary>
    /// AttackFloor 타일맵의 바운드를 global grid 좌표계로 캐싱한다.
    /// </summary>
    private void CacheAttackFloorBounds()
    {
        if (attackFloorTilemap == null) return;
        if (!_hasBossRoomBounds) CacheBossRoom();
        if (!_hasMapBounds) CacheMapBounds();

        RectInt scanBounds;
        if (_hasBossRoomBounds)
        {
            scanBounds = _bossRoomBounds;
        }
        else if (_hasMapBounds)
        {
            scanBounds = _mapBounds;
        }
        else
        {
            return;
        }

        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        bool any = false;

        for (int x = scanBounds.xMin; x < scanBounds.xMax; x++)
        {
            for (int y = scanBounds.yMin; y < scanBounds.yMax; y++)
            {
                var cell = new Vector2Int(x, y);
                if (IsInAttackFloor(cell))
                {
                    any = true;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (any)
        {
            _attackFloorBounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            _hasAttackFloorBounds = true;
            Debug.Log($"[BossA] Cached global AttackFloor bounds: {_attackFloorBounds}");
        }
    }

    /// <summary>
    /// 해당 셀이 AttackFloor 타일맵 위인지 확인한다.
    /// attackFloorTilemap이 미할당이면 항상 true (제한 없음).
    /// </summary>
    private bool IsInAttackFloor(Vector2Int cell)
    {
        if (attackFloorTilemap == null) return true;
        if (GridSystem.Instance == null) return false;
        Vector3 worldPos = GridSystem.Instance.GridToWorld(cell);
        Vector3Int localCell = attackFloorTilemap.WorldToCell(worldPos);
        return attackFloorTilemap.HasTile(localCell);
    }

    // 시각 보이기/안보이기 상태 조절 헬퍼
    // instant=true 면 DOTween 페이드 없이 즉시 전환 (패턴3 등 즉시 등장/퇴장이 필요한 경우)
    private void SetBossVisibility(bool visible, bool instant = false)
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        var colliders = GetComponentsInChildren<Collider2D>();

        // 기존 페이드 트윈 초기화
        foreach (var r in renderers)
        {
            if (r != null) r.DOKill();
        }

        if (visible)
        {
            if (instant)
            {
                // 즉시 등장: 알파 1, 콜라이더 즉시 활성화, 타격 가능
                foreach (var r in renderers)
                {
                    if (r != null)
                    {
                        r.enabled = true;
                        Color color = r.color;
                        color.a = 1f;
                        r.color = color;
                    }
                }
                _isVanished = false;
                foreach (var col in colliders)
                {
                    if (col != null) col.enabled = true;
                }
                if (hpBar != null && hpBar.allHpBar != null) hpBar.allHpBar.SetActive(true);
            }
            else
            {
                // 페이드인: 렌더러를 켜고 알파 0에서 시작
                foreach (var r in renderers)
                {
                    if (r != null)
                    {
                        r.enabled = true;
                        Color color = r.color;
                        color.a = 0f;
                        r.color = color;
                    }
                }

                // DOTween으로 페이드인
                for (int i = 0; i < renderers.Length; i++)
                {
                    var r = renderers[i];
                    if (r == null) continue;

                    var tween = r.DOFade(1f, fadeDuration);

                    // 첫 번째 렌더러의 페이드 완료 시점에 충돌체 활성화 및 타격 허용
                    if (i == 0)
                    {
                        tween.OnComplete(() =>
                        {
                            _isVanished = false;
                            foreach (var col in colliders)
                            {
                                if (col != null) col.enabled = true;
                            }
                            if (hpBar != null && hpBar.allHpBar != null) hpBar.allHpBar.SetActive(true);
                        });
                    }
                }

                // 렌더러가 하나도 없는 비정상 상태 대비 안전 장치
                if (renderers.Length == 0)
                {
                    _isVanished = false;
                    foreach (var col in colliders)
                    {
                        if (col != null) col.enabled = true;
                    }
                    if (hpBar != null && hpBar.allHpBar != null) hpBar.allHpBar.SetActive(true);
                }
            }
        }
        else
        {
            // 사라질 때: 즉시 타격 불가 및 콜라이더 비활성화
            _isVanished = true;
            foreach (var col in colliders)
            {
                if (col != null) col.enabled = false;
            }
            if (hpBar != null && hpBar.allHpBar != null) hpBar.allHpBar.SetActive(false);

            if (instant)
            {
                // 즉시 사라짐: 알파 0, 렌더러 비활성화
                foreach (var r in renderers)
                {
                    if (r != null)
                    {
                        Color color = r.color;
                        color.a = 0f;
                        r.color = color;
                        r.enabled = false;
                    }
                }
            }
            else
            {
                // 페이드아웃 후 렌더러 비활성화
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    var targetRenderer = r;
                    targetRenderer.DOFade(0f, fadeDuration).OnComplete(() =>
                    {
                        if (targetRenderer != null) targetRenderer.enabled = false;
                    });
                }
            }
        }
    }

    //============================ 페이즈2 패턴들 ========================================

    // 80초~20초 기본지속: 10초마다 Phase1 패턴3을 실행.
    private async UniTask Phase2BackgroundLoopAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();
        try
        {
            await UniTask.Delay(10000, cancellationToken: token);
            while (bossLevel == 2 && !isDying)
            {
                await RunPattern3Async();
                await UniTask.Delay(10000, cancellationToken: token);
            }
        }
        catch (System.OperationCanceledException) { }
    }

    // 80초~20초 동안 가벼운 패턴들을 가동하는 메인 루프
    private async UniTask Phase2PatternLoopAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();
        try
        {
            while (bossLevel == 2 && !isDying)
            {
                // 패턴 3 진행 중에는 사격 멈추기
                while (_isPattern3Active)
                {
                    await UniTask.Yield(token);
                }

                int patternType = Random.Range(0, 3);
                if (patternType == 0)
                {
                    // Phase2 패턴1: 가로 세로 랜덤 각각 1~2개 동시 레이져 발사 중복위치x
                    var tasks = new List<UniTask>();
                    tasks.Add(FireRandomHorizontalLasers(Random.Range(1, 3)));
                    tasks.Add(FireRandomVerticalLasers(Random.Range(1, 3)));
                    await UniTask.WhenAll(tasks);
                }
                else if (patternType == 1)
                {
                    // Phase2 패턴2: x축으로 끝에서 부터 끝까지 순차적으로 빠르게 레이져 발사. 0.3초
                    await FireSweepXAsync();
                }
                else
                {
                    // Phase2 패턴3: y축으로 끝에서 부터 끝까지 순차적으로 빠르게 레이져 발사. 0.3초 (주석 복구 - 실제 구현은 SweepY)
                    await FireSweepYAsync();
                }

                await UniTask.Delay(2000, cancellationToken: token);
            }
        }
        catch (System.OperationCanceledException) { }
    }

    // Phase2 패턴2 순차 사격
    private async UniTask FireSweepXAsync()
    {
        RectInt bounds = GetActiveBounds();
        float totalSweepTime = 1.5f;
        int cols = bounds.width;
        if (cols <= 0) return;
        int delayMs = Mathf.RoundToInt((totalSweepTime * 1000f) / cols);

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            // 패턴 3이 끼어들면 스윕 중단
            if (_isPattern3Active) break;

            Vector2Int startCell = new Vector2Int(x, bounds.yMin);
            Vector2Int endCell = new Vector2Int(x, bounds.yMax - 1);
            FireFastLaserBetween(startCell, endCell, Vector2Int.up).Forget();
            await UniTask.Delay(delayMs, cancellationToken: cancelToken.Token);
        }
    }

    // Phase2 패턴3 순차 사격
    private async UniTask FireSweepYAsync()
    {
        RectInt bounds = GetActiveBounds();
        float totalSweepTime = 1.5f;
        int rows = bounds.height;
        if (rows <= 0) return;
        int delayMs = Mathf.RoundToInt((totalSweepTime * 1000f) / rows);

        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            if (_isPattern3Active) break;

            Vector2Int startCell = new Vector2Int(bounds.xMin, y);
            Vector2Int endCell = new Vector2Int(bounds.xMax - 1, y);
            FireFastLaserBetween(startCell, endCell, Vector2Int.right).Forget();
            await UniTask.Delay(delayMs, cancellationToken: cancelToken.Token);
        }
    }

    private async UniTask FireFastLaserBetween(Vector2Int startCell, Vector2Int endCell, Vector2Int axis)
    {
        var cells = CellsAlongLine(startCell, endCell, axis, 0);
        ShowWarning(cells);
        bool cleared = false;
        GameObject lazer = null;
        GameObject eye = null;
        try
        {
            await UniTask.Delay(900, cancellationToken: cancelToken.Token);
            ClearWarning(cells);
            cleared = true;

            Vector3 startPt = GridSystem.Instance.GridToWorld(startCell);
            Vector3 endPt = GridSystem.Instance.GridToWorld(endCell);
            Quaternion q = Quaternion.Euler(0, 0, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg);
            float length = Vector3.Distance(startPt, endPt);
            Vector3 center = (startPt + endPt) * 0.5f;
            lazer = Instantiate(lazerAttackPrefab, center, q);
            lazer.transform.localScale = new Vector3(length, 1f, 1f);

            if (smallEyePrefab != null)
            {
                eye = Instantiate(smallEyePrefab, startPt, Quaternion.identity);
            }

            DamagePlayerIfInCells(cells, laserDamage);

            await UniTask.Delay(450, cancellationToken: cancelToken.Token);
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            if (!cleared)
            {
                ClearWarning(cells);
            }
            if (lazer != null) Destroy(lazer);
            if (eye != null) Destroy(eye);
        }
    }

    private async UniTask FireRandomHorizontalLasers(int count)
    {
        RectInt bounds = GetActiveBounds();
        var tasks = new List<UniTask>();
        var selectedY = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            int y = GetRandomUniqueY(bounds, selectedY);
            selectedY.Add(y);

            Vector2Int startCell = new Vector2Int(bounds.xMin, y);
            Vector2Int endCell = new Vector2Int(bounds.xMax - 1, y);
            tasks.Add(FireLaserBetween(startCell, endCell, Vector2Int.right, 0, 1f, laserDamage, cancelToken.Token));
        }
        await UniTask.WhenAll(tasks);
    }

    private async UniTask FireRandomVerticalLasers(int count)
    {
        RectInt bounds = GetActiveBounds();
        var tasks = new List<UniTask>();
        var selectedX = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            int x = GetRandomUniqueX(bounds, selectedX);
            selectedX.Add(x);

            Vector2Int startCell = new Vector2Int(x, bounds.yMin);
            Vector2Int endCell = new Vector2Int(x, bounds.yMax - 1);
            tasks.Add(FireLaserBetween(startCell, endCell, Vector2Int.up, 0, 1f, laserDamage, cancelToken.Token));
        }
        await UniTask.WhenAll(tasks);
    }

    private int GetRandomUniqueY(RectInt bounds, HashSet<int> existing)
    {
        int yMin = bounds.yMin + 1;
        int yMax = bounds.yMax - 1;
        if (yMax <= yMin) return bounds.yMin;

        int guard = 100;
        int y = Random.Range(yMin, yMax);
        while (existing.Contains(y) && guard-- > 0)
        {
            y = Random.Range(yMin, yMax);
        }
        return y;
    }

    private int GetRandomUniqueX(RectInt bounds, HashSet<int> existing)
    {
        int xMin = bounds.xMin + 1;
        int xMax = bounds.xMax - 1;
        if (xMax <= xMin) return bounds.xMin;

        int guard = 100;
        int x = Random.Range(xMin, xMax);
        while (existing.Contains(x) && guard-- > 0)
        {
            x = Random.Range(xMin, xMax);
        }
        return x;
    }

    //================================ 최후의 패턴 ======================================

    // 20초~0초 기본지속: 보스가 중앙에서 모습을 드러내고 4방향으로 닿으면 즉사하는 붉은 레이져를 발사후 천천히 회전.
    private async UniTask DeathPhaseAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();

        Vector3 center = GetRoomCenterWorld();
        transform.position = center;
        if (bossAnimation != null) bossAnimation.SetBool("Phase2", true); // 연출용 트리거 (필요 시)

        // 1) 레이저 발사 전 1초 동안 해당 방향(십자 방향)으로 경고 표시
        RectInt bounds = GetActiveBounds();
        Vector2Int centerCell = Vector2Int.zero;
        if (GridSystem.Instance != null)
        {
            centerCell = GridSystem.Instance.WorldToGrid(center);
        }

        HashSet<Vector2Int> warningCells = new HashSet<Vector2Int>();
        // 가로 방향 경고선
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            var cell = new Vector2Int(x, centerCell.y);
            if (IsInAttackFloor(cell)) warningCells.Add(cell);
        }
        // 세로 방향 경고선
        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            var cell = new Vector2Int(centerCell.x, y);
            if (IsInAttackFloor(cell)) warningCells.Add(cell);
        }

        ShowWarning(warningCells);
        bool cleared = false;
        try
        {
            await UniTask.Delay(1000, cancellationToken: token);
            ClearWarning(warningCells);
            cleared = true;
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            if (!cleared)
            {
                ClearWarning(warningCells);
            }
        }

        // 2) 실제 레이저 생성 및 회전
        var beams = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            beams[i] = Instantiate(lazerAttackPrefab, center, Quaternion.identity);
            var s = beams[i].transform.localScale;
            beams[i].transform.localScale = new Vector3(deathLaserLength, s.y, s.z);
            var sr = beams[i].GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = deathLaserColor; // 붉은 틴트 (SpriteRenderer 있을 때만)
        }

        float elapsed = 0f;
        float baseAngle = 0f;

        try
        {
            while (elapsed < deathLaserDurationSec)
            {
                baseAngle += deathLaserRotateSpeed * Time.deltaTime;

                bool hit = false;
                Vector3 playerPos = (player != null && !player.IsDead)
                    ? GridSystem.Instance.GridToWorld(player.GridPosition)
                    : Vector3.positiveInfinity;

                for (int i = 0; i < 4; i++)
                {
                    float deg = baseAngle + i * 90f;
                    float a = deg * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);

                    // 1) 보스 이동 대신 빔 정렬
                    beams[i].transform.SetPositionAndRotation(
                        center + dir * (deathLaserLength * 0.5f),
                        Quaternion.Euler(0, 0, deg));

                    // 판정: 플레이어가 이 레이저 선 위에 있으면 즉사
                    if (!hit && IsOnRay(playerPos, center, dir, deathLaserLength, deathLaserHalfWidth))
                        hit = true;
                }

                if (hit && player != null && !player.IsDead)
                    player.TakeDamage(deathInstantKillDamage);

                elapsed += Time.deltaTime;
                await UniTask.Yield(token);
            }
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            for (int i = 0; i < 4; i++)
                if (beams[i] != null) Destroy(beams[i]);
        }

        // 최후의 즉사 회전 레이저 패턴을 버틴 후 보스 소멸
        if (bossAnimation != null)
        {
            bossAnimation.SetTrigger("Death");
        }
        Debug.Log("<color=red>[보스 사망]</color> 체력이 모두 소진되고 10초간의 최후의 패턴이 끝나 보스가 사망하였습니다.");
        await DestroyBossAfterDelay();
    }

    private bool IsOnRay(Vector3 point, Vector3 origin, Vector3 dir, float length, float halfWidth)
    {
        Vector3 n = dir.normalized;
        Vector3 toPoint = point - origin;
        float along = Vector3.Dot(toPoint, n);
        if (along < 0f || along > length) return false;
        Vector3 proj = origin + n * along;
        return Vector3.Distance(point, proj) <= halfWidth;
    }

    //=============================레이져 물리 판정=============================================

    // (Unused Helper - 호환성 유지용)
    private async UniTask FirePlayerDirectedLaser(int width, float visualScaleY, int damage)
    {
        var bossCell = GridSystem.Instance.WorldToGrid(transform.position);
        var dir = DirToPlayer4(bossCell);
        var endCell = WalkToWall(bossCell, dir);
        var startCell = WalkToWall(bossCell, -dir);
        await FireLaserBetween(startCell, endCell, dir, width, visualScaleY, damage, cancelToken.Token);
    }

    private async UniTask FireLaserBetween(Vector2Int startCell, Vector2Int endCell, Vector2Int axis,
                                           int width, float visualScaleY, int damage, CancellationToken token)
    {
        var cells = CellsAlongLine(startCell, endCell, axis, width);

        ShowWarning(cells); // 타일맵 경고 (회피 창)
        bool cleared = false;
        GameObject lazer = null;
        GameObject eye = null;
        try
        {
            await UniTask.Delay(laserTelegraphMs, cancellationToken: token);
            ClearWarning(cells); // 경고 종료 → 레이저 발사
            cleared = true;

            // 1) 보스 이동 대신 레이저 시각 spawn
            Vector3 startPt = GridSystem.Instance.GridToWorld(startCell);
            Vector3 endPt = GridSystem.Instance.GridToWorld(endCell);
            Quaternion q = Quaternion.Euler(0, 0, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg);
            float length = Vector3.Distance(startPt, endPt);
            Vector3 center = (startPt + endPt) * 0.5f;
            lazer = Instantiate(lazerAttackPrefab, center, q);
            var s = lazer.transform.localScale;
            lazer.transform.localScale = new Vector3(length, s.y * visualScaleY, s.z);

            if (smallEyePrefab != null)
            {
                eye = Instantiate(smallEyePrefab, startPt, Quaternion.identity);
            }

            // 격자 데미지 판정
            DamagePlayerIfInCells(cells, damage);

            await UniTask.Delay(laserActiveMs, cancellationToken: token);
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            if (!cleared)
            {
                ClearWarning(cells);
            }
            if (lazer != null) Destroy(lazer);
            if (eye != null) Destroy(eye);
        }
    }

    private List<Vector2Int> CellsAlongLine(Vector2Int startCell, Vector2Int endCell, Vector2Int axis, int width)
    {
        var cells = new List<Vector2Int>();
        var perp = new Vector2Int(-axis.y, axis.x);
        var c = startCell;
        int guard = 500;
        while (guard-- > 0)
        {
            for (int w = -width; w <= width; w++)
            {
                var cell = c + perp * w;
                // AttackFloor 타일맵에 타일이 있는 셀만 포함
                if (IsInAttackFloor(cell))
                    cells.Add(cell);
            }
            if (c == endCell) break;
            c += axis;
        }
        return cells;
    }
}
