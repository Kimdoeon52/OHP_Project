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
    [SerializeField] private GameObject attack2Effect; //충격파 패턴 전용. 링 r번째에 scale이 r배로 커지며 spawn
    [SerializeField] private GameObject swordGround;   //일단 보존만 콰과과과 하는거임.
    
    [Header("패턴2 충격파")]
    [SerializeField] private int shockwaveMaxRadius = 4;            // 링 개수 (N)
    [SerializeField] private int shockwaveStepMs = 1000;            // 각 단계 시간 (사용자 스펙: 1초씩)
    [SerializeField] private int shockwaveDamage = 1;               // 링 한 칸당 데미지
    [SerializeField] private float shockwaveEffectLifetimeSec = 1f; // spawn된 attack2Effect 잔존 시간

    [Header("패턴3 추격 크리스탈 (0.5초 × 20회)")]
    [SerializeField] private GameObject crystalPrefab;              // 3가지 변종 공용 베이스 prefab (Crystal 컴포넌트 부착)
    [SerializeField] private int chasingCrystalCount = 20;           // 떨굴 총 횟수 (사용자 스펙: 20회)
    [SerializeField] private int chasingCrystalIntervalMs = 500;     // 한 발 사이 간격 (사용자 스펙: 0.5초)
    [SerializeField] private int chasingCrystalWarnMs = 400;         // 떨어지기 전 워닝(=조준) 시간 — 간격보다 짧게 잡아야 hit 사이클이 또렷
    [SerializeField] private float chasingCrystalLifetimeSec = 0.5f; // 낙하 후 잔존 시간 (사용자 스펙: 떨어진 후 소멸)
    [SerializeField] private int chasingCrystalDamage = 1;           // 직격 시 데미지
    [SerializeField] private Color chasingCrystalTint = Color.white; // 기본 베이스 색상

    [Header("Phase 2 연출")]
    [SerializeField] private Color phase2Tint = new Color(1f, 0.45f, 0.45f, 1f); // 보스 스프라이트 붉은 틴트
    [SerializeField] private float phase2AnimSpeed = 2f;  // Phase 2 진입 시 애니메이터 speed

    [Header("Phase 2 강화 패턴 설정")]
    [SerializeField] private int phase2ShockwaveRadius = 6;
    [SerializeField] private int phase2Attack1Range = 5;
    [SerializeField] private int phase2Attack1Width = 2;
    [SerializeField] private float phase2Attack1EffectScale = 1.6f;
    [SerializeField] private int phase2CrystalRadius = 1; // 1 -> 3x3 area
    [SerializeField] private float phase2CrystalScale = 3f;
    [SerializeField] private int phase2CrystalCount = 5;
    [SerializeField] private int phase2CrystalIntervalMs = 1000;

    [Header("죽음 패턴")]
    [SerializeField] private float deathEscapeSec = 5f;      // 즉사 전 도망 시간
    [SerializeField] private Color deathCrystalTint = Color.red; // 죽음 크리스탈 색 (붉은색)
    [SerializeField] private float deathCrystalScale = 2f;  // 죽음 크리스탈 크기 배율 (2배)
    [SerializeField] private int deathInstantKillDamage = 99999; // 안전지대 밖 즉사 데미지

    [Header("패턴 1히트 판정")]
    [SerializeField] private int attack1Damage = 1;   // 패턴1(평타) 데미지
    [SerializeField] private int attack1Range = 3;    // 패턴1 휘두르기 사거리(보스 앞 칸 수)
    [SerializeField] private int attack1Width = 1;    // 패턴1 휘두르기 폭(중심선 양옆 칸 수, 0=일자)

    [Header("보스 크기")]
    [SerializeField] private Vector2Int bossSize = new Vector2Int(1, 1); // 보스가 차지하는 크기 (1,1)

    private Vector2Int gridPos;
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

    private bool IsBossOccupying(Vector2Int cell)
    {
        return cell.x >= gridPos.x && cell.x < gridPos.x + bossSize.x &&
               cell.y >= gridPos.y && cell.y < gridPos.y + bossSize.y;
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
            Debug.LogWarning("BossB: TurnManager가 없어 플레이어 이동공격 대상으로 등록하지 못했습니다.");
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

    protected override void OnEnable()
    {
        currentHp = maxHp; // 보스 체력 설정먼저 해야 체력 업데이트가 보임
        base.OnEnable();
        bossSprite = GetComponentInChildren<SpriteRenderer>(); //Phase 2 색 변경용 (자식 포함 검색)
        gridPos = GridSystem.Instance.WorldToGrid(transform.position - BossPivotOffset); //보스 시작 격자 좌표 초기화
        RegisterBossGridTarget(); // 플레이어가 이동공격 대상으로 보스를 찾을 수 있도록 TurnManager에 등록
        Debug.Log($"보스 시작 위치: {gridPos}"); //디버그용
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

        // 2) 맵 중앙 구하기 (fallback은 보스 현재 위치)
        var safeCenter = gridPos;
        if (hasMapBounds)
        {
            safeCenter = new Vector2Int(mapBounds.xMin + mapBounds.width / 2, mapBounds.yMin + mapBounds.height / 2);
        }

        // 3) 맵 중앙에 푸른 크리스탈 떨어뜨림 (시각 랜드마크)
        if (crystalPrefab != null)
        {
            var crystalWorld = GridSystem.Instance.GridToWorld(safeCenter) + new Vector3(0.5f, 0.5f, 0f);
            var go = Instantiate(crystalPrefab, crystalWorld, Quaternion.identity);
            var c = go.GetComponent<Crystal>();
            if (c != null) 
                c.Setup(Color.cyan, deathCrystalScale, deathEscapeSec + 5f); // 푸른색 크리스탈 설정
        }

        // 4) 안전지대: 중앙 3x3 영역
        var safeCells = new HashSet<Vector2Int>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                safeCells.Add(safeCenter + new Vector2Int(dx, dy));
            }
        }

        // 5) 안전지대 제외한 모든 AttackFloor 셀에 위험 표시
        var dangerCells = new List<Vector2Int>();
        if (hasMapBounds)
        {
            for (int x = mapBounds.xMin; x < mapBounds.xMax; x++)
            {
                for (int y = mapBounds.yMin; y < mapBounds.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (IsInAttackFloor(cell) && !safeCells.Contains(cell))
                    {
                        dangerCells.Add(cell);
                    }
                }
            }
        }
        ShowWarning(dangerCells);

        try
        {
            // 6) 도망 시간 (5초 대기)
            await UniTask.Delay((int)(deathEscapeSec * 1000), cancellationToken: token);

            // 7) 즉사 폭발 발생 (패턴1 Effect 사용)
            if (attack2Effect != null)
            {
                foreach (var cell in dangerCells)
                {
                    Vector3 cellWorldPos = GridSystem.Instance.GridToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);
                    var effectGo = SpawnAttackEffectFacingBoss(attack2Effect, cellWorldPos);
                    if (effectGo != null)
                    {
                        effectGo.transform.localScale = Vector3.one * 0.7f;
                        Destroy(effectGo, 1.5f);
                    }
                }
            }

            // 8) 즉사 판정: 안전지대 밖이면 즉사
            if (player != null && !player.IsDead && !safeCells.Contains(player.GridPosition))
            {
                player.TakeDamage(deathInstantKillDamage);
            }
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            // 경고 타일 정리
            ClearWarning(dangerCells);

            // 보스 사망 애니메이션 실행
            if (bossAnimation != null)
            {
                bossAnimation.SetTrigger("Dead");
            }

            // 3초 대기 후 소멸
            try
            {
                await UniTask.Delay(3000, cancellationToken: token);
            }
            catch (System.OperationCanceledException) { }

            Destroy(gameObject); // 파괴
        }
    }

    private void DamagePlayerIfInCell(Vector2Int attackCell, int damage)
    {
        if (player == null || player.IsDead) return;

        if (player.GridPosition == attackCell)
        {
            player.TakeDamage(damage);
        }
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
        await UniTask.Delay(disappearMs, cancellationToken: cancelToken.Token);

        var dir = TeleportBesidePlayer();
        if (dir == Vector2Int.zero) dir = DirToPlayer();
        FaceDir(dir);

        bossAnimation.SetTrigger("Appear");
        await UniTask.Delay(appearMs, cancellationToken: cancelToken.Token);
        return dir;
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

    //==================================== 패턴 메인 흐름 제어 (거리 계산) ====================================
    protected override async UniTask Phase1BossPattern()
    {
        isBossAttacking = true;

        int distance = 0;
        if (player != null && !player.IsDead)
        {
            distance = Mathf.Max(Mathf.Abs(player.GridPosition.x - gridPos.x), Mathf.Abs(player.GridPosition.y - gridPos.y));
        }

        BossAction bossAction;
        if (distance < 3)
            bossAction = BossAction.Pattern1;
        else if (distance < 8)
            bossAction = BossAction.Pattern3;
        else
            bossAction = BossAction.Pattern2;

        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);

        switch (bossAction)
        {
            case BossAction.Pattern1:
                Phase1Pattern1().Forget();
                break;
            case BossAction.Pattern2:
                Phase1Pattern2().Forget();
                break;
            case BossAction.Pattern3:
                Phase1Pattern3().Forget();
                break;
        }
    }

    protected override async UniTask Phase2BossPattern()
    {
        isBossAttacking = true;

        int distance = 0;
        if (player != null && !player.IsDead)
        {
            distance = Mathf.Max(Mathf.Abs(player.GridPosition.x - gridPos.x), Mathf.Abs(player.GridPosition.y - gridPos.y));
        }

        BossAction bossAction;
        if (distance < 3)
            bossAction = BossAction.Pattern1;
        else if (distance < 6)
            bossAction = BossAction.Pattern2;
        else
            bossAction = BossAction.Pattern3;

        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);

        switch (bossAction)
        {
            case BossAction.Pattern1:
                Phase2Pattern1().Forget();
                break;
            case BossAction.Pattern2:
                Phase2Pattern2().Forget();
                break;
            case BossAction.Pattern3:
                Phase2Pattern3().Forget();
                break;
        }
    }

    //=============================== 패턴1 : 주변에 넓게 퍼지는 공격 ====================================
    protected override async UniTask Phase1Pattern1()
    {
        await ExecuteShockwavePattern(shockwaveMaxRadius);
    }

    protected override async UniTask Phase2Pattern1()
    {
        await ExecuteShockwavePattern(phase2ShockwaveRadius);
    }

    private async UniTask ExecuteShockwavePattern(int maxRadius)
    {
        bossAnimation.SetTrigger("Attack2");
        await UniTask.Delay(1000, cancellationToken: cancelToken.Token);

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
                await UniTask.Delay(shockwaveStepMs, cancellationToken: cancelToken.Token);

                if (attack2Effect != null)
                {
                    // 각 경고표시 칸마다 개별적으로 이펙트 생성 및 스케일 축소
                    foreach (var cell in ringCells)
                    {
                        Vector3 cellWorldPos = GridSystem.Instance.GridToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);
                        var go = SpawnAttackEffectFacingBoss(attack2Effect, cellWorldPos);
                        if (go != null)
                        {
                            go.transform.localScale = Vector3.one * 0.7f; // 격자 1칸에 맞춰 작게 조절
                            Destroy(go, shockwaveEffectLifetimeSec);
                        }
                    }
                }

                // 시각적 경고를 먼저 해제
                ClearWarning(ringCells);
                warningCleared = true;

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

        bossAnimation.SetTrigger("Attack2Fin");
        await UniTask.Delay(1000, cancellationToken: cancelToken.Token);
        isBossAttacking = false;
    }

    //=============================== 패턴2 : 순간이동 후 3x3 or 5x5 공격 ====================================
    protected override async UniTask Phase1Pattern2()
    {
        await ExecuteSwingPattern(attack1Range, attack1Width, 1.3f, 1000, 500); // 1.0f -> 1.3f로 확대
    }

    protected override async UniTask Phase2Pattern2()
    {
        await ExecuteSwingPattern(phase2Attack1Range, phase2Attack1Width, phase2Attack1EffectScale * 1.2f, 500, 250); // 배율 1.2배 더 확대
    }

    private async UniTask ExecuteSwingPattern(int range, int width, float effectScaleMultiplier, int disappearMs, int appearMs)
    {
        var dir = await TeleportToPlayerAsync(disappearMs, appearMs);

        var hitCells = BuildSwingCells(gridPos, dir, range, width);
        ShowWarning(hitCells);
        bool warningCleared = false;
        try
        {
            bossAnimation.SetTrigger("Attack1");
            await UniTask.Delay(disappearMs == 500 ? 400 : 800, cancellationToken: cancelToken.Token);
            
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

        await UniTask.Delay(disappearMs == 500 ? 100 : 200, cancellationToken: cancelToken.Token);
        bossAnimation.SetTrigger("Attack1Fin");

        await UniTask.Delay(1000, cancellationToken: cancelToken.Token);
        isBossAttacking = false;
    }

    //=============================== 패턴3 : 플레이어 위치에 크리스탈 생성 ====================================
    protected override async UniTask Phase1Pattern3()
    {
        await ExecuteCrystalPattern(chasingCrystalCount, chasingCrystalIntervalMs, chasingCrystalWarnMs, 0, 1f, chasingCrystalDamage, chasingCrystalTint);
    }

    protected override async UniTask Phase2Pattern3()
    {
        await ExecuteCrystalPattern(phase2CrystalCount, phase2CrystalIntervalMs, chasingCrystalWarnMs, phase2CrystalRadius, phase2CrystalScale, chasingCrystalDamage, chasingCrystalTint);
    }

    private async UniTask ExecuteCrystalPattern(int count, int intervalMs, int warnMs, int radius, float scale, int damage, Color tint)
    {
        bossAnimation.SetTrigger("Attack2");
        await UniTask.Delay(1000, cancellationToken: cancelToken.Token);
        GameObject effect = SpawnAttackEffectFacingBoss(swordGround, transform.position);

        for (int i = 0; i < count; i++)
        {
            if (cancelToken.IsCancellationRequested) break;

            if (player != null && !player.IsDead)
            {
                DropCrystalAtCell(player.GridPosition, warnMs, radius, scale, damage, tint).Forget();
            }

            await UniTask.Delay(intervalMs, cancellationToken: cancelToken.Token);
        }

        bossAnimation.SetTrigger("Attack2Fin");
        if (effect != null) Destroy(effect);
        await UniTask.Delay(1000, cancellationToken: cancelToken.Token);
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
                var worldPos = GridSystem.Instance.GridToWorld(centerCell);
                var go = Instantiate(crystalPrefab, worldPos, Quaternion.identity);
                
                Phase2Crystal p2c = go.GetComponent<Phase2Crystal>();
                if (p2c != null)
                {
                    p2c.SetupWithGridBlock(tint, scale, chasingCrystalLifetimeSec, centerCell);
                }
                else
                {
                    Crystal c = go.GetComponent<Crystal>();
                    if (c != null) 
                        c.Setup(tint, scale, chasingCrystalLifetimeSec);
                    else 
                        Destroy(go, chasingCrystalLifetimeSec);
                }

                // 크리스탈 낙하 시 카메라 셰이크(흔들림) 효과 추가
                if (CameraFollow.Instance != null)
                {
                    CameraFollow.Instance.Shake(0.15f, 0.25f);
                }
            }

            // 시각적 경고를 먼저 해제
            ClearWarning(cells);
            warningCleared = true;

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

        if (bossSprite != null) bossSprite.color = phase2Tint;
        if (bossAnimation != null) bossAnimation.speed = phase2AnimSpeed;

        var specialPattern = GetComponent<SpecialPattern>();
        if (specialPattern != null)
        {
            specialPattern.StartSpecialPattern();
        }
    }
}
