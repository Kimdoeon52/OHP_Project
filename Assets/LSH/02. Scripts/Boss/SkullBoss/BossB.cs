using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

//IGridEntitiy : TurnManager에 현재 그리드 위치를 알려주기 위한 인터페이스
//IDamageable : 플레이어 공격 시스템이 이 보스를 데미지 받을 수 있는 대상으로 인식하기 위한 인터페이스
public class BossB : BossBase, IGridEntity, IMultiCellGridEntity, IDamageable 
{
    [Header("보스 공격 이펙트")]
    [SerializeField] private GameObject attack1Effect; //쿵! 패턴1 휘두르기 전용

    
    [Header("패턴2 충격파")]
    [SerializeField] private int shockwaveMaxRadius = 4;            // 링 개수 (N)
    [SerializeField] private int shockwaveStepMs = 1000;            // 각 단계 시간 (사용자 스펙: 1초씩)
    [SerializeField] private int shockwaveDamage = 1;               // 링 한 칸당 데미지
    [SerializeField] private float shockwaveEffectLifetimeSec = 1f; // spawn된 attack2Effect 잔존 시간
    [SerializeField] private int shockwaveOverlapIntervalMs = 1000; // 충격파 중첩 발사 간격 (ms)

    [Header("추격 크리스탈 설정")]
    [SerializeField] private GameObject crystalPrefab;              // 3가지 변종 공용 베이스 prefab (Crystal 컴포넌트 부착)
    [SerializeField] private int chasingCrystalIntervalMs = 500;     // 한 발 사이 간격 (사용자 스펙: 0.5초)
    [SerializeField] private int chasingCrystalWarnMs = 400;         // 떨어지기 전 워닝(=조준) 시간 — 간격보다 짧게 잡아야 hit 사이클이 또렷
    [SerializeField] private float chasingCrystalLifetimeSec = 1f; // 낙하 후 잔존 시간 (사용자 스펙: 떨어진 후 소멸)
    [SerializeField] private int chasingCrystalDamage = 1;           // 직격 시 데미지
    [SerializeField] private Color chasingCrystalTint = Color.white; // 기본 베이스 색상
    [SerializeField] private float crystalShakeDuration = 0.15f;    // 크리스탈 낙하 시 카메라 흔들림 시간
    [SerializeField] private float crystalShakeStrength = 0.25f;    // 크리스탈 낙하 시 카메라 흔들림 강도
    [SerializeField] private int crystalHitDelayMs = 150;           // 경고 해제 후 실제 데미지가 들어가는 딜레이(ms)
    
    [Header("Phase 2 연출")]
    [SerializeField] private Color phase2Tint = new Color(1f, 0.45f, 0.45f, 1f); // 보스 스프라이트 붉은 틴트
    [SerializeField] private float phase2AnimSpeed = 2f;  // Phase 2 진입 시 애니메이터 speed

    [Header("Phase 2 강화 패턴 설정")]
    [SerializeField] private int phase2ShockwaveRadius = 6;

    [Header("BossB SFX")]
    [Tooltip("충격파/크리스탈처럼 여러 칸에서 동시에 발생하는 효과음이 과도하게 겹치지 않도록 묶음당 1회만 통과시키는 최소 간격입니다.")]
    [SerializeField, Min(0f)] private float burstSfxCooldownSeconds = 0.05f;



    [Header("패턴 1히트 판정")]
    [SerializeField] private int attack1Damage = 1;   // 패턴1(평타) 데미지
    [SerializeField] private int attack1Range = 3;    // 패턴1 휘두르기 사거리(보스 앞 칸 수)
    [SerializeField] private int attack1Width = 1;    // 패턴1 휘두르기 폭(중심선 양옆 칸 수, 0=일자)

    [Header("보스 크기")]
    [SerializeField] private Vector2Int bossSize = new Vector2Int(1, 1); // 보스가 차지하는 크기 (1,1)

    private Vector2Int gridPos;
    private readonly Dictionary<AudioEventId, float> _nextBurstSfxTimeByEvent = new Dictionary<AudioEventId, float>();
    private SpriteRenderer bossSprite;                // Phase 2 색 변경용

    #region 플레이어 이동공격 대상 등록 프록시
    public override Vector2Int BossSize => bossSize;
    public override Vector2Int GridPosition => gridPos;
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
                yield return gridPos + new Vector2Int(x, y + 1);
        }
    }

    private Vector3 BossPivotOffset => new Vector3((bossSize.x - 1) * 0.5f, (bossSize.y - 1) * 0.5f, 0f);



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

    private void PlayBossBSfx(AudioEventId eventId)
    {
        if (eventId == AudioEventId.None)
            return;

        GameEvents.OnAudioEventRequested?.Invoke(eventId);
    }

    private void PlayBossBBurstSfx(AudioEventId eventId)
    {
        if (eventId == AudioEventId.None)
            return;

        float now = Time.unscaledTime;
        if (_nextBurstSfxTimeByEvent.TryGetValue(eventId, out float nextTime) && now < nextTime)
            return;

        _nextBurstSfxTimeByEvent[eventId] = now + burstSfxCooldownSeconds;
        GameEvents.OnAudioEventRequested?.Invoke(eventId);
    }

    private void RegisterBossGridTarget()
    {
        if (TurnManager.Instance == null)
        {
            Debug.LogWarning("BossB: TurnManager가 없어 플레이어 이동공격 대상으로 등록하지 못했습니다.");
            return;
        }

        TurnManager.Instance.Register(this);
    }

    private void UnregisterBossGridTarget()
    {
        TurnManager.Instance?.Unregister(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnregisterBossGridTarget();
    }
    #endregion

    protected override void OnEnable()
    {
        currentHp = maxHp; // 보스 체력 설정먼저 해야 체력 업데이트가 보임
        base.OnEnable();

        bossSprite = GetComponentInChildren<SpriteRenderer>(); //Phase 2 색 변경용 (자식 포함 검색)
        gridPos = GridSystem.Instance.WorldToGrid(transform.position - BossPivotOffset); //보스 시작 격자 좌표 초기화
        transform.position = GridSystem.Instance.GridToWorld(gridPos) + BossPivotOffset; // 타일의 중앙으로 위치 스냅
        RegisterBossGridTarget(); // 플레이어가 이동공격 대상으로 보스를 찾을 수 있도록 TurnManager에 등록
        Debug.Log($"보스 시작 위치: {gridPos}"); //디버그용
        CacheMapBounds();

        actionCases.Clear();
        AddActionCase(BossAction.Pattern1, 50);
        AddActionCase(BossAction.Pattern2, 50);
    }

    protected override void OnDestroy()
    {
        var specialPattern = GetComponent<SpecialPattern>();
        if (specialPattern != null)
        {
            specialPattern.StopSpecialPattern();
        }
        base.OnDestroy(); // cancelToken cancel + ClearAllWarnings
    }

    //=============================== 죽음 패턴 ==========================================
    protected override async UniTask OnDeath()
    {
        DeathPatternAsync().Forget();
    }

    private async UniTaskVoid DeathPatternAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();

        // 1) 패시브/진행 중 패턴 전부 중단
        if (cancelToken != null && !cancelToken.IsCancellationRequested) cancelToken.Cancel();

        var specialPattern = GetComponent<SpecialPattern>();
        if (specialPattern != null)
        {
            specialPattern.StopSpecialPattern();
        }

        // 취소된 패턴들이 finally(ClearWarning)를 돌릴 한 프레임을 준 뒤 정리 — 워닝 refcount 꼬임 방지
        await UniTask.Yield(token);

        ClearAllWarnings();

        // 보스 사망 애니메이션 실행 (2페이즈 가속도를 원래의 1로 복구)
        if (bossAnimation != null)
        {
            bossAnimation.speed = 1f;
            bossAnimation.SetTrigger("Dead");
        }

        // 2초 대기 후 파티클 생성
        try
        {
            await UniTask.Delay(2000, cancellationToken: token);
        }
        catch (System.OperationCanceledException) { }

        // 제작된 사망 파티클 스폰
        SpawnDeathParticle();

        // 파티클 생성 후 1초 더 대기하고 보스 제거
        try
        {
            await UniTask.Delay(1000, cancellationToken: token);
        }
        catch (System.OperationCanceledException) { }

        Destroy(gameObject); // 파괴
        base.OnDeath().Forget();
    }





    // 수평 방향일 때만 스프라이트를 플레이어 쪽으로 뒤집음 (기본 우향 가정).
    private void FaceDir(Vector2Int dir)
    {
        if (dir.x == 0) return;
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dir.x < 0 ? -1f : 1f);
        transform.localScale = s;
    }

    // 공격 이펙트를 보스 현재 방향에 맞춰 스폰. transform.localScale.x 부호로 플립
    private GameObject SpawnAttackEffectFacingBoss(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return null;
        var go = Instantiate(prefab, pos, Quaternion.identity);
        var s = go.transform.localScale;
        s.x = Mathf.Abs(s.x) * Mathf.Sign(transform.localScale.x);
        go.transform.localScale = s;
        return go;
    }

    // 모든 패턴의 공통 프리픽스. Teleport 사라짐 → 플레이어 옆 칸으로 위치 점프 → Appear 등장.
    private async UniTask<Vector2Int> TeleportToPlayerAsync(int disappearMs = 1000, int appearMs = 500)
    {
        bossAnimation.SetTrigger("Teleport");
        PlayBossBSfx(AudioEventId.BossB_Teleport);
        await UniTask.Delay(GetScaledDelay(disappearMs), cancellationToken: cancelToken.Token);

        var dir = TeleportBesidePlayer();
        if (dir == Vector2Int.zero) dir = DirToPlayer();
        FaceDir(dir);

        bossAnimation.SetTrigger("Appear");
        await UniTask.Delay(GetScaledDelay(appearMs), cancellationToken: cancelToken.Token);
        return dir;
    }

    // 보스 전장(AttackFloor)의 중앙 타일로 순간이동하는 프리픽스.
    private async UniTask TeleportToCenterAsync(int disappearMs = 1000, int appearMs = 500)
    {
        bossAnimation.SetTrigger("Teleport");
        PlayBossBSfx(AudioEventId.BossB_Teleport);
        await UniTask.Delay(GetScaledDelay(disappearMs), cancellationToken: cancelToken.Token);

        Vector2Int centerCell = gridPos;
        if (hasMapBounds)
        {
            int centerX = mapBounds.xMin + mapBounds.width / 2;
            int centerY = mapBounds.yMin + mapBounds.height / 2;
            centerCell = new Vector2Int(centerX, centerY);
        }
        else if (GridSystem.Instance != null)
        {
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
            if (any)
            {
                centerCell = new Vector2Int((minX + maxX) / 2, (minY + maxY) / 2);
            }
        }

        gridPos = centerCell;
        transform.position = GridSystem.Instance.GridToWorld(centerCell) + BossPivotOffset;

        // 도착 후 플레이어를 바라봄
        Vector2Int faceDir = DirToPlayer();
        FaceDir(faceDir);

        bossAnimation.SetTrigger("Appear");
        await UniTask.Delay(GetScaledDelay(appearMs), cancellationToken: cancelToken.Token);
    }

    private List<Vector2Int> GetRingCells(Vector2Int center, int r)
    {
        var cells = new List<Vector2Int>();
        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                var c = center + new Vector2Int(dx, dy);
                if (IsInAttackFloor(c)) cells.Add(c);
            }
        }
        return cells;
    }

    // 보스 → 플레이어의 4방향 주 방향 (가까운 축 우선). 플레이어 없으면 오른쪽.
    private Vector2Int DirToPlayer()
    {
        if (player == null) return Vector2Int.right;

        var diff = player.GridPosition - gridPos;
        if (diff == Vector2Int.zero) return Vector2Int.right;

        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
            return new Vector2Int(System.Math.Sign(diff.x), 0);
        return new Vector2Int(0, System.Math.Sign(diff.y));
    }

    // 플레이어 좌/우 인접 칸으로 순간이동. 보스가 있던 쪽 우선, 막히면 반대쪽.
    private Vector2Int TeleportBesidePlayer()
    {
        if (player == null) return Vector2Int.zero;
        var pPos = player.GridPosition;
        int firstSign = (gridPos.x <= pPos.x) ? -1 : 1; // -1: 플레이어 왼쪽 칸, +1: 오른쪽 칸

        foreach (int sign in new[] { firstSign, -firstSign })
        {
            int xOffset = (sign < 0) ? -bossSize.x : 1;
            var cell = new Vector2Int(pPos.x + xOffset, pPos.y);

            if (IsAreaInAttackFloor(cell, bossSize))
            {
                gridPos = cell;
                transform.position = GridSystem.Instance.GridToWorld(cell) + BossPivotOffset;
                return new Vector2Int(-sign, 0); // 착지 칸에서 플레이어를 향하는 방향
            }
        }
        return Vector2Int.zero; // 양쪽 다 막힘
    }

    // 보스 앞쪽(dir 방향)으로 사거리만큼 뻗고 좌우 width만큼 퍼지는 휘두르기 칸 목록.
    private List<Vector2Int> BuildSwingCells(Vector2Int origin, Vector2Int dir, int range, int width)
    {
        var cells = new List<Vector2Int>();
        var perp = new Vector2Int(-dir.y, dir.x); // dir에 수직인 축 (좌우 퍼짐)

        for (int d = 1; d <= range; d++)
        {
            for (int w = -width; w <= width; w++)
            {
                var pos = origin + dir * d + perp * w;
                if (IsInAttackFloor(pos))
                    cells.Add(pos);
            }
        }
        return cells;
    }

    //=============================== 패턴1 : 주변에 넓게 퍼지는 공격 ====================================
    protected override async UniTask Phase1Pattern1()
    {
        try
        {
            // 1. AttackFloor의 중앙 타일로 순간이동 (1번만 실행)
            await TeleportToCenterAsync(1000, 500);

            // 보스 공격 애니메이션 시작 (캐스팅 포즈 유지)
            bossAnimation.SetTrigger("Attack2");
            await UniTask.Delay(GetScaledDelay(1000), cancellationToken: cancelToken.Token);

            var tasks = new List<UniTask>();

            // 2. 공격(충격파)을 설정된 간격으로 연달아 중첩 실행
            for (int i = 0; i < 2; i++)
            {
                if (cancelToken.IsCancellationRequested) break;

                // 비동기로 실행하여 리스트에 등록 (끝나기 전에 다음 충격파가 안쪽에서 다시 시작됨)
                tasks.Add(ExecuteShockwavePattern(shockwaveMaxRadius, shockwaveStepMs));

                // 마지막이 아니라면 설정된 대기 시간 후 다음 충격파 시작
                if (i < 1)
                {
                    await UniTask.Delay(shockwaveOverlapIntervalMs, cancellationToken: cancelToken.Token);
                }
            }

            // 모든 충격파 공격이 끝날 때까지 대기
            await UniTask.WhenAll(tasks);

            // 마무리 애니메이션 재생
            bossAnimation.SetTrigger("Attack2Fin");
            await UniTask.Delay(GetScaledDelay(1000), cancellationToken: cancelToken.Token);
        }
        finally
        {
            isBossAttacking = false;
        }
    }

    protected override async UniTask Phase2Pattern1()
    {
        try
        {
            // Phase 2는 Phase 1과 동일하게 중앙으로 순간이동
            await TeleportToCenterAsync(1000, 500);

            bossAnimation.SetTrigger("Attack2");
            await UniTask.Delay(GetScaledDelay(1000), cancellationToken: cancelToken.Token);

            var tasks = new List<UniTask>();

            // 2회 중첩 실행 (Phase 2 스펙: 반경 6칸인 phase2ShockwaveRadius 사용)
            for (int i = 0; i < 2; i++)
            {
                if (cancelToken.IsCancellationRequested) break;

                tasks.Add(ExecuteShockwavePattern(phase2ShockwaveRadius, shockwaveStepMs));

                if (i < 1)
                {
                    await UniTask.Delay(shockwaveOverlapIntervalMs, cancellationToken: cancelToken.Token);
                }
            }

            await UniTask.WhenAll(tasks);

            bossAnimation.SetTrigger("Attack2Fin");
            await UniTask.Delay(GetScaledDelay(1000), cancellationToken: cancelToken.Token);
        }
        finally
        {
            isBossAttacking = false;
        }
    }



    private async UniTask ExecuteShockwavePattern(int maxRadius, int stepDelayMs)
    {
        var center = gridPos;

        for (int r = 1; r <= maxRadius; r++)
        {
            if (cancelToken.IsCancellationRequested) break;

            var ringCells = GetRingCells(center, r);
            if (ringCells.Count == 0) continue;

            ShowWarning(ringCells);
            bool warningCleared = false;
            try
            {
                await UniTask.Delay(stepDelayMs, cancellationToken: cancelToken.Token);
                PlayBossBBurstSfx(AudioEventId.BossB_Shockwave);

                if (crystalPrefab != null)
                {
                    PlayBossBBurstSfx(AudioEventId.BossB_CrystalSpawn);
                    // 각 경고표시 칸마다 개별적으로 이펙트 생성 및 스케일 축소 (충격파 이미지 -> 크리스탈 교체)
                    foreach (var cell in ringCells)
                    {
                        Vector3 cellWorldPos = GridSystem.Instance.GridToWorld(cell);
                        var go = Instantiate(crystalPrefab, cellWorldPos, Quaternion.identity);
                        if (go != null)
                        {
                            Phase2Crystal p2c = go.GetComponent<Phase2Crystal>();
                            if (p2c != null)
                            {
                                p2c.Setup(chasingCrystalTint, 0.7f, shockwaveEffectLifetimeSec);
                            }
                            else
                            {
                                Crystal c = go.GetComponent<Crystal>();
                                if (c != null)
                                    c.Setup(chasingCrystalTint, 0.7f, shockwaveEffectLifetimeSec);
                                else
                                    Destroy(go, shockwaveEffectLifetimeSec);
                            }
                        }
                    }
                }

                // 시각적 경고를 먼저 해제
                ClearWarning(ringCells);
                warningCleared = true;

                // 크리스탈 낙하 애니메이션 싱크를 위해 딜레이 추가
                await UniTask.Delay(crystalHitDelayMs, cancellationToken: cancelToken.Token);

                DamagePlayerIfInCells(ringCells, shockwaveDamage);
            }
            finally
            {
                if (!warningCleared)
                {
                    ClearWarning(ringCells);
                }
            }
        }
    }

    //=============================== 패턴2 : 순간이동 후 3x3 or 5x5 공격 ====================================
    protected override async UniTask Phase1Pattern2()
    {
        // 1x1 범위 (radius: 0), 개수 5개 낙하
        await ExecuteSwingPattern(attack1Range, attack1Width, 1.3f, 1000, 500, 0, 1f, 5); 
    }

    protected override async UniTask Phase2Pattern2()
    {
        // 3x3 범위 (radius: 1), 개수 9개 낙하 (Phase 2 사양)
        await ExecuteSwingPattern(attack1Range, attack1Width, 1.3f, 1000, 500, 1, 1f, 9);
    }



    private async UniTask ExecuteSwingPattern(int range, int width, float effectScaleMultiplier, int disappearMs, int appearMs, int crystalRadius, float crystalScale, int chasingCount)
    {
        var dir = await TeleportToPlayerAsync(disappearMs, appearMs);

        var hitCells = BuildSwingCells(gridPos, dir, range, width);
        ShowWarning(hitCells);
        bool warningCleared = false;
        try
        {
            bossAnimation.SetTrigger("Attack1");
            await UniTask.Delay(GetScaledDelay(800), cancellationToken: cancelToken.Token);
            PlayBossBSfx(AudioEventId.BossB_Swing);
            
            GameObject effect = SpawnAttackEffectFacingBoss(attack1Effect, transform.position);
            if (effect != null)
            {
                effect.transform.localScale *= effectScaleMultiplier;
                Destroy(effect, 1f);
            }

            // 시각적 경고를 먼저 해제
            ClearWarning(hitCells);
            warningCleared = true;

            DamagePlayerIfInCells(hitCells, attack1Damage);
        }
        finally
        {
            if (!warningCleared)
            {
                ClearWarning(hitCells);
            }
        }

        await UniTask.Delay(GetScaledDelay(200), cancellationToken: cancelToken.Token);
        bossAnimation.SetTrigger("Attack1Fin");

        await UniTask.Delay(GetScaledDelay(1000), cancellationToken: cancelToken.Token);

        for (int i = 0; i < chasingCount; i++)
        {
            if (cancelToken.IsCancellationRequested) break;

            if (player != null && !player.IsDead)
            {
                DropCrystalAtCell(player.GridPosition, chasingCrystalWarnMs, crystalRadius, crystalScale, chasingCrystalDamage, chasingCrystalTint).Forget();
            }

            await UniTask.Delay(chasingCrystalIntervalMs, cancellationToken: cancelToken.Token);
        }

        await UniTask.Delay(GetScaledDelay(1000), cancellationToken: cancelToken.Token);
        isBossAttacking = false;
    }



    private async UniTaskVoid DropCrystalAtCell(Vector2Int centerCell, int warnMs, int radius, float scale, int damage, Color tint)
    {
        var cells = new List<Vector2Int>();
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int c = centerCell + new Vector2Int(dx, dy);
                if (IsInAttackFloor(c))
                    cells.Add(c);
            }
        }

        ShowWarning(cells);
        bool warningCleared = false;
        try
        {
            await UniTask.Delay(warnMs, cancellationToken: cancelToken.Token);

            if (crystalPrefab != null)
            {
                PlayBossBBurstSfx(AudioEventId.BossB_CrystalSpawn);
                foreach (var cell in cells)
                {
                    var worldPos = GridSystem.Instance.GridToWorld(cell);
                    var go = Instantiate(crystalPrefab, worldPos, Quaternion.identity);
                    
                    Phase2Crystal p2c = go.GetComponent<Phase2Crystal>();
                    if (p2c != null)
                    {
                        // 스케일은 늘리지 않고 1f로 둔 상태로 각 격자마다 배치하여 밟지 못하게 함
                        p2c.SetupWithGridBlock(tint, 1f, chasingCrystalLifetimeSec, cell);
                    }
                    else
                    {
                        Crystal c = go.GetComponent<Crystal>();
                        if (c != null) 
                            c.Setup(tint, 1f, chasingCrystalLifetimeSec);
                        else 
                            Destroy(go, chasingCrystalLifetimeSec);
                    }
                }

                // 크리스탈 낙하 시 카메라 셰이크(흔들림) 효과 추가 (한 번만 발생)
                if (CameraFollow.Instance != null)
                {
                    CameraFollow.Instance.Shake(crystalShakeDuration, crystalShakeStrength);
                }
                PlayBossBBurstSfx(AudioEventId.BossB_CrystalImpact);
            }

            // 시각적 경고를 먼저 해제
            ClearWarning(cells);
            warningCleared = true;

            // 크리스탈 낙하 애니메이션 싱크를 위해 딜레이 추가
            await UniTask.Delay(crystalHitDelayMs, cancellationToken: cancelToken.Token);

            DamagePlayerIfInCells(cells, damage);
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            if (!warningCleared)
            {
                ClearWarning(cells);
            }
        }
    }

    //=============================== Phase 2 진입 ==========================================
    protected override void EnterPhase2()
    {
        base.EnterPhase2();

        // 붉은 틴트 색 변환 제거
        if (bossAnimation != null) bossAnimation.speed = phase2AnimSpeed;

        var specialPattern = GetComponent<SpecialPattern>();
        if (specialPattern != null)
        {
            specialPattern.StartPhase2SpecialPattern();
        }
    }

    private void EnterPhase3()
    {
        bossLevel = 3;
        Debug.Log("<color=orange>[보스 Phase 3 진입]</color>");
        
        var specialPattern = GetComponent<SpecialPattern>();
        if (specialPattern != null)
        {
            specialPattern.StartPhase3SpecialPattern();
        }
    }

    protected override async UniTask PhaseState()
    {
        if (bossLevel == 1 && currentHp <= maxHp * 0.7f)
        {
            Debug.Log($"<color=orange>[보스 Phase 2 진입 조건 충족]</color> 체력 {currentHp} ≤ {maxHp * 0.7f}");
            EnterPhase2();
        }
        else if (bossLevel == 2 && currentHp <= maxHp * 0.3f)
        {
            Debug.Log($"<color=orange>[보스 Phase 3 진입 조건 충족]</color> 체력 {currentHp} ≤ {maxHp * 0.3f}");
            EnterPhase3();
        }

        switch (bossLevel)
        {
            case 1:
                await Phase1BossPattern();
                break;
            case 2:
                await Phase2BossPattern();
                break;
            case 3:
                await Phase3BossPattern();
                break;
        }
    }

    private async UniTask Phase3BossPattern()
    {
        isBossAttacking = true;
        BossAction bossAction = BossPatternPer();
        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);
        RequestBossPatternSfx(bossAction);
        switch (bossAction)
        {
            case BossAction.Pattern1:
                Phase2Pattern1().Forget();
                break;
            case BossAction.Pattern2:
                Phase2Pattern2().Forget();
                break;
            case BossAction.Pattern3:
                Phase2Pattern1().Forget();
                break;
        }
    }

    protected override void RequestBossPatternSfx(BossAction bossAction)
    {
        AudioEventId eventId = bossAction switch
        {
            BossAction.Pattern1 => AudioEventId.None,
            BossAction.Pattern2 => AudioEventId.None,
            BossAction.Pattern3 => AudioEventId.None,
            _ => AudioEventId.None
        };

        if (eventId != AudioEventId.None)
        {
            GameEvents.OnAudioEventRequested?.Invoke(eventId);
        }
    }

    private int GetScaledDelay(int originalDelayMs)
    {
        if (bossAnimation != null)
        {
            float speed = bossAnimation.speed;
            if (speed > 0f)
            {
                return Mathf.RoundToInt(originalDelayMs / speed);
            }
        }
        return originalDelayMs;
    }
}
