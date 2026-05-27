using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

//IGridEntitiy : TurnManager에 현재 그리드 위치를 알려주기 위한 인터페이스
//IDamageable : 플레이어 공격 시스템이 이 보스를 데미지 받을 수 있는 대상으로 인식하기 위한 인터페이스
public class BossB : BossBase, IGridEntity, IDamageable 
{
    [Header("보스 공격 이펙트")]
    [SerializeField] private GameObject attack1Effect; //쿵! 패턴1 휘두르기 전용
    [SerializeField] private GameObject attack2Effect; //충격파 패턴 전용. 링 r번째에 scale이 r배로 커지며 spawn
    [SerializeField] private GameObject fallingRock;   //Phase2 RockRain에서만 사용 (추후 크리스탈로 교체 예정)
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

    [Header("Phase 2 패시브 바위 비 (HP ≤ phase2HpThreshold)")]
    [SerializeField] private int rainTickMs = 300;        // 한 틱 간격(ms). 작을수록 빽빽함
    [SerializeField] private int rainCellsPerTick = 3;    // 한 틱에 동시에 떨어지는 칸 수

    [Header("Phase 2 연출 / 패턴")]
    [SerializeField] private Color phase2Tint = new Color(1f, 0.45f, 0.45f, 1f); // 보스 스프라이트 붉은 틴트
    [SerializeField] private float phase2AnimSpeed = 2f;  // Phase 2 진입 시 애니메이터 speed
    [SerializeField] private GameObject miniSkullPrefab;  // Phase2Pattern2에서 소환할 미니해골 프리팹
    [SerializeField] private int miniSkullCount = 2;      // 한 번에 소환할 미니해골 수
    [SerializeField] private int miniSkullLifetimeMs = 10000; // 패턴2 대기 시간 (미니해골 lifetime과 맞출 것)

    [Header("패턴 1히트 판정")]
    [SerializeField] private int attack1Damage = 1;   // 패턴1(평타) 데미지
    [SerializeField] private int attack1Range = 3;    // 패턴1 휘두르기 사거리(보스 앞 칸 수)
    [SerializeField] private int attack1Width = 1;    // 패턴1 휘두르기 폭(중심선 양옆 칸 수, 0=일자)
    [SerializeField] private int rockDamage = 1;      // 패턴2(바위) 칸당 데미지
    [SerializeField] private int rockWarnMs = 1000;    // 바위 낙하 전 경고 노출 시간

    //[Header("보스 씬 타일맵 (GridSystem 없는 씬에서 연결)")]
    //[SerializeField] private Tilemap floorTilemap;

    private Vector2Int gridPos;
    private Animator anime;
    private bool rainActive = false;                  // Phase 2 바위 비가 이미 돌고 있는지 (중복 시작 방지)
    private List<Vector2Int> walkableCellsCache;      // GetAllWalkableCells 결과 캐시 (매 틱 재조회 방지)
    private SpriteRenderer bossSprite;                // Phase 2 색 변경용

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

    protected override void OnEnable()
    {
        currentHp = maxHp; // 보스 체력 설정먼저 해야 체력 업데이트가 보임
        base.OnEnable();
        AddActionCase(BossAction.Pattern1, 50); // 평타
        AddActionCase(BossAction.Pattern2, 30); // 돌
        AddActionCase(BossAction.Pattern3, 20); // 없음
        anime = GetComponent<Animator>();
        bossSprite = GetComponentInChildren<SpriteRenderer>(); //Phase 2 색 변경용 (자식 포함 검색)
        gridPos = GridSystem.Instance.WorldToGrid(transform.position); //보스 시작 격자 좌표 초기화
        RegisterBossGridTarget(); // 플레이어가 이동공격 대상으로 보스를 찾을 수 있도록 TurnManager에 등록
        Debug.Log($"보스 시작 위치: {gridPos}"); //디버그용
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

    // 공격 이펙트를 보스 현재 방향에 맞춰 스폰. transform.localScale.x 부호로 플리쁘
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
    // disappearMs / appearMs를 절반으로 주면 Phase 2 가속 텔레포트로 사용 가능.
    // 반환값: 보스가 바라보는 방향 (휘두르기 등 dir이 필요한 패턴에서 사용).
    private async UniTask<Vector2Int> TeleportToPlayerAsync(int disappearMs = 1000, int appearMs = 500)
    {
        anime.SetTrigger("Teleport");
        await UniTask.Delay(disappearMs, cancellationToken: cancelToken.Token);

        var dir = TeleportBesidePlayer();
        if (dir == Vector2Int.zero) dir = DirToPlayer();
        FaceDir(dir);

        anime.SetTrigger("Appear");
        await UniTask.Delay(appearMs, cancellationToken: cancelToken.Token);
        return dir;
    }

    private List<Vector2Int> GetRingCells(Vector2Int center, int r)
    {
        var cells = new List<Vector2Int>();
        for (int dx = -r; dx <= r; dx++) // r=1 → 8셀, r=2 → 16셀, r=3 → 24셀 무ㅓ 이런식임
        {
            for (int dy = -r; dy <= r; dy++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                var c = center + new Vector2Int(dx, dy);
                if (GridSystem.Instance.IsWalkable(c)) cells.Add(c);
            }
        }
        return cells;
    }

    //========= 패턴1 : 플레이어 옆 순간이동 → 휘두르기 ====================================================
    protected override async UniTask Phase1Pattern1()
    {
        // 1~3) 텔레포트 프리픽스 (사라짐 → 위치 점프 → 등장). 모든 패턴 공용.
        var dir = await TeleportToPlayerAsync();

        // 4) 휘두르기 칸 계산(데이터) → 경고 표시(시각). 셀은 여기서 고정 — 윈드업 중 재계산 X.
        var hitCells = BuildSwingCells(gridPos, dir, attack1Range, attack1Width);
        ShowWarning(hitCells);
        try
        {
            // 5) 휘두르기 윈드업. 이 800ms가 플레이어가 빠져나가 피하는 유일한 창.
            anime.SetTrigger("Attack1");
            await UniTask.Delay(800, cancellationToken: cancelToken.Token);
            GameObject Effect = SpawnAttackEffectFacingBoss(attack1Effect, transform.position);
            if (Effect != null) Destroy(Effect, 1f);

            // 6) 임팩트 순간: 경고 칸 안에 플레이어가 있으면 데미지.
            DamagePlayerIfInCells(hitCells, attack1Damage);
        }
        finally
        {
            // 취소되더라도 워닝은 반드시 해제 (안 그러면 워닝 타일 영구 잔존)
            ClearWarning(hitCells);
        }

        await UniTask.Delay(200, cancellationToken: cancelToken.Token);
        anime.SetTrigger("Attack1Fin");

        await UniTask.Delay(1000, cancellationToken: cancelToken.Token);
        isBossAttacking = false;
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
    // 성공 시 휘두를 방향(플레이어 쪽) 반환, 양쪽 다 막히면 Vector2Int.zero.
    private Vector2Int TeleportBesidePlayer()
    {
        if (player == null) return Vector2Int.zero;
        var pPos = player.GridPosition;
        int firstSign = (gridPos.x <= pPos.x) ? -1 : 1; // -1: 플레이어 왼쪽 칸, +1: 오른쪽 칸

        foreach (int sign in new[] { firstSign, -firstSign })
        {
            var cell = pPos + new Vector2Int(sign, 0);
            if (GridSystem.Instance.IsWalkable(cell))
            {
                gridPos = cell;
                transform.position = GridSystem.Instance.GridToWorld(cell);
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
                if (GridSystem.Instance.IsWalkable(pos))
                    cells.Add(pos);
            }
        }
        return cells;
    }

    // ===================== 패턴2 : 텔레포트 → Attack2 → 단계별 확장 충격파 =========================
    // 사용자 스펙: Attack2 트리거 후 1초 → 경고 → 1초 → attack2Effect spawn(+데미지) → 더 넓은 경고 → ... N번 반복.
    // 각 단계는 shockwaveStepMs(=1초). attack2Effect는 링 r마다 새로 spawn되며 scale이 r배로 커짐.
    protected override async UniTask Phase1Pattern2()
    {
        // 1) 텔레포트 프리픽스 (공용)
        await TeleportToPlayerAsync();

        // 2) Attack2 트리거 → 1초 대기 (첫 워닝 전 anim 시간 확보)
        anime.SetTrigger("Attack2");
        await UniTask.Delay(2000, cancellationToken: cancelToken.Token);

        var center = gridPos;

        // 3) 링 r=1..N : ShowWarning → 1초 → attack2Effect(scale ×r) + 데미지 → ClearWarning → 다음 링.
        for (int r = 1; r <= shockwaveMaxRadius; r++)
        {
            if (cancelToken.IsCancellationRequested) break;

            var ringCells = GetRingCells(center, r);
            if (ringCells.Count == 0) continue;

            ShowWarning(ringCells);
            try
            {
                await UniTask.Delay(shockwaveStepMs, cancellationToken: cancelToken.Token);

                // attack2Effect spawn — scale은 링 r에 비례해 커짐. 보스 방향에 맞춰 플립.
                if (attack2Effect != null)
                {
                    var go = SpawnAttackEffectFacingBoss(attack2Effect, transform.position);
                    var go2 = SpawnAttackEffectFacingBoss(attack1Effect, transform.position);
                    if (go != null)
                    {
                        go.transform.localScale *= r;
                        go2.transform.localScale *= r/2;
                        Destroy(go, shockwaveEffectLifetimeSec);
                        Destroy(go2, shockwaveEffectLifetimeSec);
                        go.transform.transform.position += new Vector3(0, -0.5f, 0); // 살짝 밑에서
                    }
                }

                DamagePlayerIfInCells(ringCells, shockwaveDamage);
            }
            finally
            {
                ClearWarning(ringCells);
            }
        }

        // 4) 마무리
        anime.SetTrigger("Attack2Fin");
        await UniTask.Delay(1000, cancellationToken: cancelToken.Token);
        isBossAttacking = false;
    }

    private async UniTaskVoid FallSingleRockAsync(Vector2Int targetPos)
    {
        ShowWarning(targetPos);
        Vector3 pos = GridSystem.Instance.GridToWorld(targetPos);
        GameObject effect = Instantiate(fallingRock, pos, Quaternion.identity);
        try
        {
            await UniTask.Delay(rockWarnMs, cancellationToken: cancelToken.Token);
            DamagePlayerIfInCell(targetPos, rockDamage);
        }
        catch (System.OperationCanceledException)
        {
            // 취소: 데미지 판정만 건너뛰고 워닝/이펙트는 아래 finally에서 정리.
        }
        finally
        {
            // 워닝과 이펙트는 무조건 정리 (취소 시에도 잔존 방지)
            ClearWarning(targetPos);
            if (effect != null) Destroy(effect, 1.5f);
        }
    }

    //===========================================페이즈2 패턴 1 ============================================================
    // 2배속 순간이동 + 내려치기. EnterPhase2에서 anime.speed = 2로 미리 가속됐으므로 여기 딜레이도 모두 절반.
    protected override async UniTask Phase2Pattern1()
    {
        for (int i = 0; i < 10; i++)
        {
            if (cancelToken.IsCancellationRequested) break;

            // 1~3) 텔레포트 프리픽스 (2배속). disappear/appear ms를 절반으로 넘김.
            var dir = await TeleportToPlayerAsync(500, 250);

            // 4) 휘두르기 칸 계산 → 경고 표시.
            var hitCells = BuildSwingCells(gridPos, dir, attack1Range, attack1Width);
            ShowWarning(hitCells);
            try
            {
                // 5) 휘두르기 윈드업 (400ms). 플레이어 회피 창도 절반이라 더 빡빡함.
                anime.SetTrigger("Attack1");
                await UniTask.Delay(400, cancellationToken: cancelToken.Token);
                GameObject Effect = SpawnAttackEffectFacingBoss(attack1Effect, transform.position);
                if (Effect != null) Destroy(Effect, 1f);

                // 6) 임팩트 판정.
                DamagePlayerIfInCells(hitCells, attack1Damage);
            }
            finally
            {
                ClearWarning(hitCells);
            }

            await UniTask.Delay(100, cancellationToken: cancelToken.Token);
            anime.SetTrigger("Attack1Fin");

            await UniTask.Delay(1000, cancellationToken: cancelToken.Token); //공격 마무리 (2배속 1초)
        }
        isBossAttacking = false;
    }

    //=========================================== 페이즈2 패턴 2 : MiniSkull 소환 ============================================================
    // 보스 주변 walkable 셀에 miniSkullCount만큼 미니해골 소환. miniSkullLifetimeMs 동안 배회 후 자연 소멸(미니해골 자체 lifetime).
    protected override async UniTask Phase2Pattern2()
    {
        if (miniSkullPrefab == null)
        {
            Debug.LogWarning("BossB.Phase2Pattern2: miniSkullPrefab이 비어 있어 패턴 스킵.");
            isBossAttacking = false;
            return;
        }

        // 보스 주변 walkable 셀 중 무작위로 N개 선택 (중복 없이).
        var spawnCells = PickRandomNearbyWalkable(miniSkullCount, searchRadius: 4);
        foreach (var cell in spawnCells)
        {
            var world = GridSystem.Instance.GridToWorld(cell);
            Instantiate(miniSkullPrefab, world, Quaternion.identity);
        }

        // 미니해골이 배회하는 동안 보스는 다음 패턴 못 함 (isBossAttacking 유지).
        await UniTask.Delay(miniSkullLifetimeMs, cancellationToken: cancelToken.Token);
        isBossAttacking = false;
    }

    // 보스 현재 위치 기준 searchRadius 안의 walkable 셀에서 최대 count개 무작위 선택 (보스 칸 제외, 중복 제외).
    private List<Vector2Int> PickRandomNearbyWalkable(int count, int searchRadius)
    {
        var candidates = new List<Vector2Int>();
        for (int dx = -searchRadius; dx <= searchRadius; dx++)
        {
            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var c = gridPos + new Vector2Int(dx, dy);
                if (GridSystem.Instance.IsWalkable(c)) candidates.Add(c);
            }
        }

        var picked = new List<Vector2Int>();
        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            int idx = Random.Range(0, candidates.Count);
            picked.Add(candidates[idx]);
            candidates.RemoveAt(idx);
        }
        return picked;
    }
    protected override async UniTask Phase2Pattern3()
    {
        isBossAttacking = false;
    }
    //=============================== 패턴3 : 텔레포트 → Attack2 → 플레이어 위치 추격 크리스탈 비 =================
    // 보스가 텔레포트 → Attack2 트리거 → chasingCrystalIntervalMs 간격으로 chasingCrystalCount회 크리스탈 투하.
    // 각 발은 fire-and-forget으로, 발사 순간 플레이어의 현재 위치를 스냅샷해서 그 칸으로 떨어짐 ("따라오는" 효과).
    // 모든 발사가 끝나면 Attack2Fin 트리거.
    protected override async UniTask Phase1Pattern3()
    {
        await TeleportToPlayerAsync();

        anime.SetTrigger("Attack2");
        await UniTask.Delay(1000, cancellationToken: cancelToken.Token);
        GameObject Effect = SpawnAttackEffectFacingBoss(swordGround, transform.position);
        for (int i = 0; i < chasingCrystalCount; i++)
        {
            if (cancelToken.IsCancellationRequested) break;

            // 매 발 발사 시점의 플레이어 위치를 스냅샷. 이후 플레이어가 움직이면 다음 발이 새 위치를 추격.
            if (player != null && !player.IsDead)
            {
                DropChasingCrystal(player.GridPosition).Forget();
            }

            await UniTask.Delay(chasingCrystalIntervalMs, cancellationToken: cancelToken.Token);
        }

        anime.SetTrigger("Attack2Fin");
        Destroy(Effect);
        await UniTask.Delay(1000, cancellationToken: cancelToken.Token);
        isBossAttacking = false;
    }

    // 단일 크리스탈 낙하 (fire-and-forget). 호출 시 cell을 고정해 워닝 → 크리스탈 spawn → 데미지 → 자연 소멸.
    private async UniTaskVoid DropChasingCrystal(Vector2Int cell)
    {
        ShowWarning(cell);
        try
        {
            await UniTask.Delay(chasingCrystalWarnMs, cancellationToken: cancelToken.Token);

            if (crystalPrefab != null)
            {
                var worldPos = GridSystem.Instance.GridToWorld(cell);
                var go = Instantiate(crystalPrefab, worldPos, Quaternion.identity);
                var c = go.GetComponent<Crystal>();
                if (c != null) c.Setup(chasingCrystalTint, 1f, chasingCrystalLifetimeSec);
                else Destroy(go, chasingCrystalLifetimeSec);
            }

            DamagePlayerIfInCell(cell, chasingCrystalDamage);
        }
        catch (System.OperationCanceledException) { /* 보스 사망 등 취소 */ }
        finally
        {
            ClearWarning(cell);
        }
    }

    //=============================== Phase 2 진입 / 바위 비 ==========================================
    // 베이스의 EnterPhase2(bossLevel=2, patternDelay 감소) 후에 맵 전체에 지속 바위 비 시작.
    // 추가로 Phase 2 시각 연출: 보스 스프라이트 붉은 틴트 + 애니메이터 speed 가속.
    protected override void EnterPhase2()
    {
        base.EnterPhase2();

        // 보스 외형 변화
        if (bossSprite != null) bossSprite.color = phase2Tint;
        if (anime != null) anime.speed = phase2AnimSpeed;

        if (rainActive) return;
        rainActive = true;
        walkableCellsCache = new List<Vector2Int>(GridSystem.Instance.GetAllWalkableCells());
        RockRain().Forget();
    }

    // Phase 2가 시작된 시점부터 보스가 살아있는 동안(=cancelToken 살아있는 동안) 무한 반복.
    // 매 틱마다 walkableCellsCache에서 rainCellsPerTick개를 골라 FallSingleRockAsync로 비동기 투하.
    // 기존 패턴2 RockFalling과 독립적으로 돌기 때문에 보스가 다른 패턴을 쓰는 중에도 비가 계속 내림.
    private async UniTaskVoid RockRain()
    {
        try
        {
            while (!cancelToken.IsCancellationRequested)
            {
                if (walkableCellsCache != null && walkableCellsCache.Count > 0)
                {
                    for (int i = 0; i < rainCellsPerTick; i++)
                    {
                        var cell = walkableCellsCache[Random.Range(0, walkableCellsCache.Count)];
                        FallSingleRockAsync(cell).Forget();
                    }
                }
                await UniTask.Delay(rainTickMs, cancellationToken: cancelToken.Token);
            }
        }
        catch (System.OperationCanceledException) { /* 보스 파괴 시 정상 취소 */ }
    }
}
