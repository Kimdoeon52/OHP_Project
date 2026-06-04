using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/*
 * BossA — HP 기반 보스 (눈알 보스). 본체 행동만 담당.
 * BossBase를 상속하며, BossBase의 패턴 메서드를 오버라이드하여 동작을 구성.
 * 
 * 행동 사이클:
 *   1) AttackFloor 위에서 3보 배회 (Wander)
 *   2) BossPatternPer()로 Pattern1 또는 Pattern2 결정
 *   3) 패턴 실행 후 patternDelay 대기 및 반복
 * 
 * 패턴 종류:
 *   - Pattern 1 (점프 찍기): 플레이어 위치로 점프(DOJump)하여 3x3 범위 공격, 3초 쿨타임 후 한번 더 반복.
 *   - Pattern 2 (십자 레이저): 플레이어가 있는 라인을 향해 가로/세로 4방향 레이저 발사.
 */
public class BossA : BossBase, IGridEntity, IMultiCellGridEntity, IDamageable
{
    [Header("HP 페이즈 경계")]
    [SerializeField] private int phase3HpThreshold = 20;

    [Header("배회 설정")]
    [SerializeField] private float wanderMoveDuration = 0.25f; // DOTween 이동 한 칸 소요 시간(초)
    [SerializeField] private int wanderStepIntervalMs = 500;   // 배회 한 칸 이동 간격
    [SerializeField] private int wanderStepsBeforePattern = 3; // 패턴 실행 전 배회(걷기) 횟수

    [Header("공격 예고/이펙트")]
    [SerializeField] private GameObject shockwavePrefab;       // 점프 공격 시 소환할 충격파 효과
    [SerializeField] private float shockwaveLifetimeSec = 1f;

    [Header("카메라 흔들림 설정 (점프 공격)")]
    [SerializeField] private float shakeDuration = 0.4f;       // 흔들림 지속 시간 (초)
    [SerializeField] private float shakeStrength = 0.5f;       // 흔들림 세기

    [Header("레이저 설정 (Pattern 2)")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private int laserTelegraphMs = 800;
    [SerializeField] private int laserActiveMs = 400;
    [SerializeField] private int laserDamage = 1;
    [SerializeField] private float laserVisualThickness = 3.5f; // 레이저 이미지의 시각적 두께 (히트 판정은 2칸 고정)

    [Header("AttackFloor 타일맵 (이동 범위 제한)")]
    [SerializeField] private Tilemap attackFloorTilemap;

    [SerializeField] private Vector2Int bossSize = new Vector2Int(2, 2); // 보스가 차지하는 크기 (2,2)

    // ─── 내부 상태 ───────────────────────────────────────────────────
    private RectInt _mapBounds;
    private bool _hasMapBounds;
    private SpriteRenderer _bossSprite;
    private Vector2Int gridPos;

    #region 플레이어 이동공격 대상 등록 프록시
    public Vector2Int GridPosition => gridPos;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => currentHp <= 0;

    public IEnumerable<Vector2Int> OccupiedCells
    {
        get
        {
            if (IsDead)
                yield break;

            for (int x = 0; x < bossSize.x; x++)
            for (int y = 0; y < bossSize.y; y++)
                yield return gridPos + new Vector2Int(x, y);
        }
    }

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

    //==============================================================================================
    // 1. 초기화 및 지형/범위 판정 헬퍼
    //==============================================================================================
    protected override void OnEnable()
    {
        base.OnEnable(); // patternDelay 변환, 보스전 120초 타이머 시작 및 WaitBossCoolTime 구동
        currentHp = maxHp;
        _bossSprite = GetComponentInChildren<SpriteRenderer>();
        CacheMapBounds();

        if (GridSystem.Instance != null)
            gridPos = GridSystem.Instance.WorldToGrid(transform.position - new Vector3(0.5f, 0.5f, 0f));

        RegisterBossGridTarget();

        // BossAction 가중치 설정 (Pattern1: 50% / Pattern2: 50%)
        actionCases.Clear();
        AddActionCase(BossAction.Pattern1, 50);
        AddActionCase(BossAction.Pattern2, 50);
    }

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

    private bool IsAreaInAttackFloor(Vector2Int bottomLeft, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (!IsInAttackFloor(bottomLeft + new Vector2Int(x, y)))
                    return false;
            }
        }
        return true;
    }

    //==============================================================================================
    // 2. 메인 행동 루프 제어 (BossBase 오버라이드)
    //==============================================================================================
    protected override async UniTask Phase1BossPattern()
    {
        isBossAttacking = true;
        if (!_hasMapBounds) CacheMapBounds();

        // 설정된 횟수만큼 배회 후 패턴 발사
        await WanderStepsAsync(wanderStepsBeforePattern);

        if (cancelToken.IsCancellationRequested || isDying)
        {
            isBossAttacking = false;
            return;
        }

        BossAction bossAction = BossPatternPer();
        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);

        switch (bossAction)
        {
            case BossAction.Pattern1:
                Phase1Pattern1().Forget();
                break;
            case BossAction.Pattern2:
                Phase1Pattern2().Forget();
                break;
            default:
                Phase1Pattern1().Forget();
                break;
        }
    }

    protected override async UniTask Phase2BossPattern()
    {
        isBossAttacking = true;
        if (!_hasMapBounds) CacheMapBounds();

        // 설정된 횟수만큼 배회 후 패턴 발사
        await WanderStepsAsync(wanderStepsBeforePattern);

        if (cancelToken.IsCancellationRequested || isDying)
        {
            isBossAttacking = false;
            return;
        }

        BossAction bossAction = BossPatternPer();
        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);

        switch (bossAction)
        {
            case BossAction.Pattern1:
                Phase2Pattern1().Forget();
                break;
            case BossAction.Pattern2:
                Phase2Pattern2().Forget();
                break;
            default:
                Phase2Pattern1().Forget();
                break;
        }
    }

    //==============================================================================================
    // 3. 배회 및 이동 지원 헬퍼
    //==============================================================================================
    private async UniTask WanderStepsAsync(int steps)
    {
        var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        for (int i = 0; i < steps; i++)
        {
            if (cancelToken.IsCancellationRequested || isDying) break;

            ShuffleDirs(dirs);
            bool moved = false;
            foreach (var d in dirs)
            {
                var next = gridPos + d;
                if (IsAreaInAttackFloor(next, bossSize))
                {
                    var targetWorld = GridSystem.Instance.GridToWorld(next) + new Vector3(0.5f, 0.5f, 0f);
                    await transform.DOMove(targetWorld, wanderMoveDuration)
                        .SetEase(Ease.InOutSine)
                        .AsyncWaitForCompletion();
                    gridPos = next;
                    moved = true;
                    break;
                }
            }
            if (!moved) break;

            await UniTask.Delay(wanderStepIntervalMs, cancellationToken: cancelToken.Token);
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

    private Vector2Int FindValidFootprintNear(Vector2Int target)
    {
        Vector2Int centerTarget = target - new Vector2Int(bossSize.x / 2, bossSize.y / 2);
        if (IsAreaInAttackFloor(centerTarget, bossSize)) return centerTarget;

        var offsets = new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
        };
        foreach (var offset in offsets)
        {
            Vector2Int candidate = target + offset;
            if (IsAreaInAttackFloor(candidate, bossSize))
                return candidate;
        }
        return gridPos;
    }

    //==============================================================================================
    // 4. 패턴 1: 점프 및 3x3 범위 격자 공격
    //==============================================================================================
    protected override async UniTask Phase1Pattern1()
    {
        await ExecuteJumpAttackPattern();
        isBossAttacking = false;
    }

    protected override async UniTask Phase2Pattern1()
    {
        await ExecuteJumpAttackPattern();
        isBossAttacking = false;
    }

    private async UniTask ExecuteJumpAttackPattern()
    {
        for (int i = 0; i < 2; i++)
        {
            if (cancelToken.IsCancellationRequested || isDying) break;
            if (player == null || player.IsDead) break;

            Vector2Int playerCell = player.GridPosition;
            Vector2Int landingGridPos = FindValidFootprintNear(playerCell);

            // 1) 3x3 범위 공격 경고 표시 (플레이어 중심)
            var attackCells = new List<Vector2Int>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2Int c = playerCell + new Vector2Int(x, y);
                    if (GridSystem.Instance.IsInBounds(c))
                        attackCells.Add(c);
                }
            }

            ShowWarning(attackCells);
            try
            {
                // 경고 표시 후 2초 대기
                await UniTask.Delay(2000, cancellationToken: cancelToken.Token);

                if (cancelToken.IsCancellationRequested || isDying) break;

                // 2) 점프 이동
                var targetWorld = GridSystem.Instance.GridToWorld(landingGridPos) + new Vector3(0.5f, 0.5f, 0f);
                if (bossAnimation != null) bossAnimation.SetTrigger("Teleport");

                await transform.DOJump(targetWorld, jumpPower: 2f, numJumps: 1, duration: 0.6f)
                    .SetEase(Ease.OutQuad)
                    .AsyncWaitForCompletion();
                gridPos = landingGridPos;

                if (bossAnimation != null) bossAnimation.SetTrigger("Appear");

                // 카메라 흔들림 트리거
                if (CameraFollow.Instance != null)
                {
                    CameraFollow.Instance.Shake(shakeDuration, shakeStrength);
                }

                // 3) 충격파 및 데미지 판정
                if (shockwavePrefab != null)
                {
                    var go = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
                    if (shockwaveLifetimeSec > 0f) Destroy(go, shockwaveLifetimeSec);
                }

                bool playerHit = false;
                if (player != null && !player.IsDead)
                {
                    foreach (var c in attackCells)
                    {
                        if (player.GridPosition == c)
                        {
                            playerHit = true;
                            break;
                        }
                    }
                }

                if (playerHit)
                {
                    player.TakeDamage(1);
                    Vector2Int? knockbackTarget = FindKnockbackTarget(player.GridPosition, playerCell, landingGridPos);
                    if (knockbackTarget.HasValue)
                    {
                        player.Movement.Knockback(knockbackTarget.Value);
                        Debug.Log($"<color=orange>[보스 점프 공격]</color> 플레이어를 {knockbackTarget.Value}로 밀쳐냈습니다.");
                    }
                    else
                    {
                        Debug.LogWarning("[보스 점프 공격] 플레이어를 밀쳐낼 빈 공간이 없습니다.");
                    }
                }
            }
            finally
            {
                ClearWarning(attackCells);
            }

            // 4) 대기 시간 처리
            if (i == 0)
            {
                // 첫 번째 점프 후에는 3초 쿨타임 대기
                await UniTask.Delay(3000, cancellationToken: cancelToken.Token);
            }
            else
            {
                // 두 번째 점프 후에는 충격파 나오고 1초 뒤 다시 걷기
                await UniTask.Delay(1000, cancellationToken: cancelToken.Token);
            }
        }
    }

    //==============================================================================================
    // 5. 패턴 2: 플레이어 방향 십자 레이저 발사
    //==============================================================================================
    protected override async UniTask Phase1Pattern2()
    {
        await ExecuteCrossLaserPattern();
        isBossAttacking = false;
    }

    protected override async UniTask Phase2Pattern2()
    {
        await ExecuteCrossLaserPattern();
        isBossAttacking = false;
    }

    private async UniTask ExecuteCrossLaserPattern()
    {
        if (player == null || player.IsDead) return;

        var attackCells = GetCrossCells();
        ShowWarning(attackCells);

        try
        {
            if (bossAnimation != null) bossAnimation.SetTrigger("Attack2");

            await UniTask.Delay(laserTelegraphMs, cancellationToken: cancelToken.Token);

            List<GameObject> lasers = new List<GameObject>();
            if (laserPrefab != null && _hasMapBounds)
            {
                // 세로 레이저 (두께 2칸, 시각적 두께 laserVisualThickness)
                Vector3 vStart = GridSystem.Instance.GridToWorld(new Vector2Int(gridPos.x, _mapBounds.yMin));
                Vector3 vEnd = GridSystem.Instance.GridToWorld(new Vector2Int(gridPos.x + 1, _mapBounds.yMax - 1));
                Vector3 vCenter = (vStart + vEnd) * 0.5f;
                GameObject vLaser = Instantiate(laserPrefab, vCenter, Quaternion.Euler(0, 0, 90));
                float vLength = Vector3.Distance(vStart, vEnd);
                vLaser.transform.localScale = new Vector3(vLength, laserVisualThickness, 1f);
                lasers.Add(vLaser);

                // 가로 레이저 (두께 2칸, 시각적 두께 laserVisualThickness)
                Vector3 hStart = GridSystem.Instance.GridToWorld(new Vector2Int(_mapBounds.xMin, gridPos.y));
                Vector3 hEnd = GridSystem.Instance.GridToWorld(new Vector2Int(_mapBounds.xMax - 1, gridPos.y + 1));
                Vector3 hCenter = (hStart + hEnd) * 0.5f;
                GameObject hLaser = Instantiate(laserPrefab, hCenter, Quaternion.identity);
                float hLength = Vector3.Distance(hStart, hEnd);
                hLaser.transform.localScale = new Vector3(hLength, laserVisualThickness, 1f);
                lasers.Add(hLaser);
            }

            DamagePlayerIfInCells(attackCells, laserDamage);

            await UniTask.Delay(laserActiveMs, cancellationToken: cancelToken.Token);

            foreach (var l in lasers) if (l != null) Destroy(l);
        }
        finally
        {
            ClearWarning(attackCells);
            if (bossAnimation != null) bossAnimation.SetTrigger("Attack2Fin");
        }
    }

    private List<Vector2Int> GetCrossCells()
    {
        var cells = new List<Vector2Int>();
        if (!_hasMapBounds) return cells;

        // 세로 두께 2줄
        for (int y = _mapBounds.yMin; y < _mapBounds.yMax; y++)
        {
            cells.Add(new Vector2Int(gridPos.x, y));
            cells.Add(new Vector2Int(gridPos.x + 1, y));
        }

        // 가로 두께 2줄
        for (int x = _mapBounds.xMin; x < _mapBounds.xMax; x++)
        {
            cells.Add(new Vector2Int(x, gridPos.y));
            cells.Add(new Vector2Int(x, gridPos.y + 1));
        }

        return cells;
    }

    private Vector2Int? FindKnockbackTarget(Vector2Int playerPos, Vector2Int attackCenter, Vector2Int bossPos)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        int searchRadius = 4;
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                Vector2Int candidate = playerPos + new Vector2Int(x, y);

                if (GridSystem.Instance == null) continue;
                if (!GridSystem.Instance.IsInBounds(candidate)) continue;
                if (!GridSystem.Instance.IsWalkable(candidate)) continue;
                if (GridSystem.Instance.IsCellOccupied(candidate)) continue;
                if (Mathf.Abs(candidate.x - attackCenter.x) <= 1 && Mathf.Abs(candidate.y - attackCenter.y) <= 1) continue;
                if (IsBossOccupying(candidate, bossPos)) continue;

                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0) return null;

        Vector2 pushDir = (Vector2)(playerPos - attackCenter);
        if (pushDir.sqrMagnitude < 0.01f)
        {
            Vector2 bossCenter = (Vector2)bossPos + new Vector2(0.5f, 0.5f);
            pushDir = (Vector2)playerPos - bossCenter;
            if (pushDir.sqrMagnitude < 0.01f)
            {
                pushDir = Vector2.down;
            }
        }
        pushDir.Normalize();

        candidates.Sort((a, b) =>
        {
            float distA = Vector2Int.Distance(a, playerPos);
            float distB = Vector2Int.Distance(b, playerPos);

            Vector2 dirA = (Vector2)(a - playerPos);
            float dotA = dirA.sqrMagnitude > 0 ? Vector2.Dot(dirA.normalized, pushDir) : 0;

            Vector2 dirB = (Vector2)(b - playerPos);
            float dotB = dirB.sqrMagnitude > 0 ? Vector2.Dot(dirB.normalized, pushDir) : 0;

            float scoreA = distA - dotA * 1.5f;
            float scoreB = distB - dotB * 1.5f;

            return scoreA.CompareTo(scoreB);
        });

        return candidates[0];
    }

    private bool IsBossOccupying(Vector2Int cell, Vector2Int bossPos)
    {
        return cell.x >= bossPos.x && cell.x < bossPos.x + bossSize.x &&
               cell.y >= bossPos.y && cell.y < bossPos.y + bossSize.y;
    }
}