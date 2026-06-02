using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/*
 * BossA — HP 기반 보스 (눈알 보스). 본체 행동만 담당.
 * 격자 레이저 패턴은 별도 스크립트(LazerPattern)에서 독립적으로 실행.
 *
 * 본체 행동 사이클 (반복, HP > phase3HpThreshold 동안):
 *   1) 활성 모드 — activeDurationSec(=10초) 현재 위치에서 대기
 *   2) 사라짐 → 맵 중앙으로 텔레포트 (충격파 효과)
 *   3) 배회 모드 — wanderDurationSec(=10초) 랜덤 인접 셀 이동
 *
 * Phase 3 (HP ≤ phase3HpThreshold):
 *   사이클 정지. 한 번 중앙으로 이동한 뒤 영구 정지.
 *   (회전 레이저는 LazerPattern이 별도로 처리)
 */
public class BossA : BossBase, IGridEntity, IMultiCellGridEntity, IDamageable
{
    [Header("메인 사이클")]
    [SerializeField] private float activeDurationSec = 10f;    // 활성 모드 지속 시간
    [SerializeField] private float wanderDurationSec = 10f;    // 배회 모드 지속 시간
    [SerializeField] private int wanderStepIntervalMs = 500;   // 배회 한 칸 이동 간격
    [SerializeField] private int breakDisappearMs = 800;       // 사라짐 노출 시간
    [SerializeField] private int fallAppearMs = 400;           // 중앙 등장 후 잠깐 대기

    [Header("HP 페이즈 경계")]
    [SerializeField] private int phase3HpThreshold = 20;       // HP ≤ 이 값 → Phase 3 (중앙 고정, 사이클 정지)

    [Header("충격파 (선택)")]
    [SerializeField] private GameObject shockwavePrefab;       // 중앙 낙하 시 spawn할 효과 prefab
    [SerializeField] private float shockwaveLifetimeSec = 1f;

    [Header("이동 연출")]
    [SerializeField] private float wanderMoveDuration = 0.25f; // DOTween 이동 한 칸 소요 시간(초)

    [Header("AttackFloor 타일맵 (이동·중앙 범위 제한)")]
    [SerializeField] private Tilemap attackFloorTilemap;

    [SerializeField] private Vector2Int bossSize = new Vector2Int(2, 2); // 보스가 차지하는 크기 (2,2)

    // ─── 내부 상태 ───────────────────────────────────────────────────
    private RectInt _mapBounds;
    private bool _hasMapBounds;
    private SpriteRenderer _bossSprite;
    private bool _phase3Locked;
    private bool _mainLoopStarted;
    private Vector2Int gridPos;

    #region 플레이어 이동공격 대상 등록 프록시
    // 이 region은 NYH 플레이어의 "이동하려는 칸에 적이 있으면 이동 대신 공격" 기능과 연결하기 위한 구간입니다.
    // BossB는 원래 자기 내부 gridPos만 알고 있고, TurnManager에는 자신을 등록하지 않았습니다.
    // 그래서 플레이어 쪽에서 TurnManager.GetEntityAt(target)을 호출해도 보스를 찾을 수 없었습니다.
    //
    // 해결 방식:
    // 1. BossB가 IGridEntity를 구현해서 "내가 어느 격자 칸에 있는지" 외부에 알려줍니다.
    // 2. BossB가 IDamageable을 구현해서 플레이어 공격 코드가 TakeDamage를 호출할 수 있게 합니다.
    // 3. Start에서 TurnManager에 등록하고, OnDisable에서 해제합니다.
    //
    // 주의:
    // 현재 구현은 BossB가 한 칸짜리 보스라는 가정입니다.
    // 보스가 2x2 이상을 차지하게 되면 GridPosition 하나만으로는 부족하므로,
    // 보스가 점유하는 모든 칸을 반환하거나 별도 충돌 판정 목록을 만들어야 합니다.

    public Vector2Int GridPosition => gridPos;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => currentHp <= 0;

    // IMultiCellGridEntity 인터페이스 요구사항입니다. 보스가 실제로 차지하는 모든 칸을 반환합니다.
    public IEnumerable<Vector2Int> OccupiedCells
    {
        get
        {
            if (IsDead  || gridPos.x < 0 || gridPos.y < 0)
                yield break;

            for (int x = 0; x < bossSize.x; x++)
            for (int y = 0; y < bossSize.y; y++)
                yield return gridPos + new Vector2Int(x, y);
        }
    }

    // IDamageable 인터페이스 요구사항입니다.
    // 플레이어 공격 시스템은 TakeDamage 이름으로 피해를 주므로,
    // BossB 내부의 기존 보스 피해 처리 함수인 BossTakeDamage로 연결합니다.
    public void TakeDamage(int amount)
    {
        BossTakeDamage(amount);
    }

    // IDamageable 인터페이스 요구사항입니다.
    // BossBase에는 BossTakeDamage가 이미 있으므로, 피해 처리는 TakeDamage에서 BossTakeDamage로 연결합니다.
    public virtual void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }

    // IGridEntity 인터페이스 요구사항입니다.
    // BossB는 BossBase의 Update/UniTask 패턴으로 독립 행동하므로, 턴 매니저가 호출해도 여기서는 아무것도 하지 않습니다.
    public void OnTurnUpdate()
    {
    }

    //Boss를 TurnManager의 엔티티 목록에 등록하는 함수.
    // 플레이어는 이동하려는 칸의 대상을 TurnManager.GetEntityAt()으로 찾기 때문에,
    //이 등록이 빠지면 보스가 실제로 그 칸에 있어도 이동공격 대상으로 인식되지 않습니다.
    private void RegisterBossGridTarget()
    {
        if (TurnManager.Instance == null)
        {
            Debug.LogWarning("BossB: TurnManager가 없어 플레이어 이동공격 대상으로 등록하지 못했습니다.");
            return;
        }

        TurnManager.Instance.Register(this);
    }

    // BossB를 TurnManager의 그리드 엔티티 목록에서 제거합니다.
    // 보스가 죽거나 비활성화된 뒤에도 목록에 남아 있으면,
    // 플레이어가 없는 대상을 공격하려 하거나 파괴된 참조가 남을 수 있어서 정리합니다.
    // ?. 연산자는 TurnManager가 이미 사라진 상황에서는 아무것도 하지 않도록 막아줍니다.
    private void UnregisterBossGridTarget()
    {
        TurnManager.Instance?.Unregister(this);
    }

    // Unity 생명주기 함수입니다.
    // BossB 오브젝트가 비활성화되거나 파괴될 때 자동으로 호출됩니다.
    // 이때 TurnManager 목록에서 BossB를 제거해서,
    // 사라진 보스가 플레이어 이동공격 대상으로 계속 남지 않게 합니다.
    private void OnDisable()
    {
        UnregisterBossGridTarget();
    }
    #endregion
    //=============================== 초기화 ==========================================
    protected override void OnEnable()
    {
        base.OnEnable(); // patternDelay 변환, 보스전 120초 타이머 시작
        currentHp = maxHp;
        _bossSprite = GetComponentInChildren<SpriteRenderer>();
        CacheMapBounds();

        if (GridSystem.Instance != null)
            gridPos = GridSystem.Instance.WorldToGrid(transform.position);

        RegisterBossGridTarget();

        // base의 PhaseState 디스패치를 막고 자체 메인 루프로 동작.
        isBossAttacking = true;
        if (!_mainLoopStarted)
        {
            _mainLoopStarted = true;
            MainLoop().Forget();
        }
    }

    // BossBase의 시간 기반 Phase2 전환을 막는다 (BossA는 HP 기반 + 자체 사이클).
    protected override void EnterPhase2() { /* no-op */ }

    // base PhaseState가 호출되지 않도록 무력화 (메인 루프에서 직접 사이클 운영).
    protected override UniTask PhaseState() => UniTask.CompletedTask;

    private void CacheMapBounds()
    {
        if (attackFloorTilemap != null)
        {
            attackFloorTilemap.CompressBounds();
            var b = attackFloorTilemap.cellBounds;
            _mapBounds = new RectInt(b.xMin, b.yMin, b.size.x, b.size.y);
            _hasMapBounds = true;
            return;
        }

        // attackFloorTilemap 미설정 시 기존 방식(전체 walkable) 폴백
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

    private bool IsInAttackFloor(Vector2Int cell)
    {
        if (attackFloorTilemap != null)
            return attackFloorTilemap.GetTile(new Vector3Int(cell.x, cell.y, 0)) != null;
        return GridSystem.Instance != null && GridSystem.Instance.IsWalkable(cell);
    }

    //=============================== 메인 행동 루프 ==========================================
    private async UniTask MainLoop()
    {
        try
        {
            while (!cancelToken.IsCancellationRequested && !isDying)
            {
                // Phase 3 진입 1회: 중앙 이동 후 영구 정지 (회전 레이저는 LazerPattern이 별도 담당)
                if (currentHp <= phase3HpThreshold && !_phase3Locked)
                {
                    _phase3Locked = true;
                    await BreakAndFallAsync();
                }

                if (_phase3Locked)
                {
                    // 사이클 정지, 가만히 대기
                    await UniTask.Delay(1000, cancellationToken: cancelToken.Token);
                    continue;
                }

                // 일반 사이클: 활성(현재 자리에서 대기) → 사라짐/중앙낙하 → 배회 → 반복
                await UniTask.Delay((int)(activeDurationSec * 1000), cancellationToken: cancelToken.Token);
                if (cancelToken.IsCancellationRequested || isDying) break;

                await BreakAndFallAsync();
                if (cancelToken.IsCancellationRequested || isDying) break;

                await WanderModeAsync();
            }
        }
        catch (System.OperationCanceledException) { }
    }

    //=============================== 사라짐 / 중앙 낙하 / 배회 ==========================================
    private async UniTask BreakAndFallAsync()
    {
        // 1) 페이드 아웃 후 사라짐
        if (_bossSprite != null)
        {
            await _bossSprite.DOFade(0f, breakDisappearMs / 1000f * 0.5f)
                .SetEase(Ease.InQuad)
                .AsyncWaitForCompletion();
            _bossSprite.enabled = false;
            _bossSprite.color = new UnityEngine.Color(_bossSprite.color.r, _bossSprite.color.g, _bossSprite.color.b, 1f);
        }
        else
        {
            await UniTask.Delay(breakDisappearMs, cancellationToken: cancelToken.Token);
        }

        // 2) 맵 중앙으로 텔레포트 후 gridPos 갱신
        var centerWorld = GetMapCenterWorld();
        transform.position = centerWorld;
        if (GridSystem.Instance != null)
            gridPos = GridSystem.Instance.WorldToGrid(centerWorld);

        // 3) 충격파 effect (있으면) + 페이드 인 등장
        if (shockwavePrefab != null)
        {
            var go = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
            if (shockwaveLifetimeSec > 0f) Destroy(go, shockwaveLifetimeSec);
        }
        if (_bossSprite != null)
        {
            _bossSprite.color = new UnityEngine.Color(_bossSprite.color.r, _bossSprite.color.g, _bossSprite.color.b, 0f);
            _bossSprite.enabled = true;
            await _bossSprite.DOFade(1f, fallAppearMs / 1000f * 0.6f)
                .SetEase(Ease.OutQuad)
                .AsyncWaitForCompletion();
        }
        else
        {
            await UniTask.Delay(fallAppearMs, cancellationToken: cancelToken.Token);
        }
    }

    private async UniTask WanderModeAsync()
    {
        var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        float elapsed = 0f;

        while (elapsed < wanderDurationSec)
        {
            if (cancelToken.IsCancellationRequested || isDying) break;

            ShuffleDirs(dirs);
            var bossCell = GridSystem.Instance.WorldToGrid(transform.position);
            foreach (var d in dirs)
            {
                var next = bossCell + d;
                if (IsInAttackFloor(next))
                {
                    var targetWorld = GridSystem.Instance.GridToWorld(next);
                    transform.DOMove(targetWorld, wanderMoveDuration).SetEase(Ease.InOutSine);
                    gridPos = next;
                    break;
                }
            }

            await UniTask.Delay(wanderStepIntervalMs, cancellationToken: cancelToken.Token);
            elapsed += wanderStepIntervalMs / 1000f;
        }
    }

    private static void ShuffleDirs(Vector2Int[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }

    private Vector3 GetMapCenterWorld()
    {
        if (_hasMapBounds && GridSystem.Instance != null)
        {
            int cx = (_mapBounds.xMin + _mapBounds.xMax) / 2;
            int cy = (_mapBounds.yMin + _mapBounds.yMax) / 2;
            return GridSystem.Instance.GridToWorld(new Vector2Int(cx, cy));
        }
        return transform.position;
    }
}
