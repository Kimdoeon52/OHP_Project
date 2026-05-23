using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

//IGridEntitiy : TurnManager에 현재 그리드 위치를 알려주기 위한 인터페이스
//IDamageable : 플레이어 공격 시스템이 이 보스를 데미지 받을 수 있는 대상으로 인식하기 위한 인터페이스
public class BossB : BossBase, IGridEntity, IDamageable 
{
    [Header("이동 설정")]
    [SerializeField] private float moveTime = 0.25f; //이동하는데 걸리는 시간
    [SerializeField] private Ease moveEase = Ease.Linear; //스르륵 미끄러지게 끝나는 두트윈임.
    [SerializeField] private int stepsPerPattern = 4; //이동 횟수
    //[SerializeField] private int stepDelayMs = 1000; //이동 딜레이

    [Header("보스 공격 이펙트")]
    [SerializeField] private GameObject attack1Effect; //쿵!
    [SerializeField] private GameObject fallingRock; //떨어지는 바위 프리팹
    [SerializeField] private GameObject swordGround; //땅에 칼 박아버리는거

    [Header("패턴2설정")]
    [SerializeField] private int rockMinDis = 1; // 보스로부터 최소 1칸 떨어진 곳
    [SerializeField] private int rockMaxDis = 5; // 보스로부터 최대 3칸 떨어진 곳
    [SerializeField] private int rockDelayMs = 500; //0.5초...?

    [Header("패턴 1히트 판정")]
    [SerializeField] private int attack1Damage = 1;   // 패턴1(평타) 데미지
    [SerializeField] private int attack1Range = 3;    // 패턴1 휘두르기 사거리(보스 앞 칸 수)
    [SerializeField] private int attack1Width = 1;    // 패턴1 휘두르기 폭(중심선 양옆 칸 수, 0=일자)
    [SerializeField] private int rockDamage = 1;      // 패턴2(바위) 칸당 데미지
    [SerializeField] private int rockWarnMs = 1000;    // 바위 낙하 전 경고 노출 시간

    //[Header("보스 씬 타일맵 (GridSystem 없는 씬에서 연결)")]
    //[SerializeField] private Tilemap floorTilemap;

    private Vector2Int gridPos;
    private bool isMove = false;
    private Animator anime;

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

    // 4방향 이동을 위한 벡터 배열임
    private static Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    protected override void Start()
    {
        currentHp = maxHp; // 보스 체력 설정먼저 해야 체력 업데이트가 보임
        base.Start();
        AddActionCase(BossAction.Pattern1, 50); // 평타
        AddActionCase(BossAction.Pattern2, 20); // 돌
        AddActionCase(BossAction.Pattern3, 30); // 이동
        anime = GetComponent<Animator>();
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

    //========= 패턴1 : 플레이어 옆 순간이동 → 휘두르기 ====================================================
    protected override async UniTask Phase1Pattern1()
    {
        // 1) 사라지는 연출. Teleport 애니메이션이 끝나 보스가 보이지 않게 된 시점까지 대기.
        anime.SetTrigger("Teleport");
        await UniTask.Delay(1000, cancellationToken: cancelToken.Token); //텔레포트(사라짐) 애니메이션 1초

        // 2) 사라진 동안(보이지 않을 때) 플레이어 옆 칸으로 실제 위치 점프. 양쪽 막히면 제자리에서 플레이어 방향으로 폴백.
        var dir = TeleportBesidePlayer();
        if (dir == Vector2Int.zero) dir = DirToPlayer();//두둥
        FaceDir(dir);//플레이어 방향보기

        // 3) 새 위치에서 등장 연출.
        anime.SetTrigger("Appear");//등장!
        await UniTask.Delay(500, cancellationToken: cancelToken.Token); //등장 애니메이션 0.5초

        // 4) 휘두르기 칸 계산(데이터) → 경고 표시(시각). 셀은 여기서 고정 — 윈드업 중 재계산 X.
        var hitCells = BuildSwingCells(gridPos, dir, attack1Range, attack1Width);
        ShowWarning(hitCells);

        // 5) 휘두르기 윈드업. 이 800ms가 플레이어가 빠져나가 피하는 유일한 창.
        anime.SetTrigger("Attack1");
        await UniTask.Delay(800, cancellationToken: cancelToken.Token); //1.2초+0.2초 1.4초가 그마나 공격 이펙트랑 맞는듯?
        GameObject Effect = Instantiate(attack1Effect, transform.position, Quaternion.identity); //공격 이펙트 생성
        Destroy(Effect, 1f); //이펙트 1초 후 제거 예약ㅇ미.

        // 6) 임팩트 순간: 경고 칸 안에 플레이어가 있으면 데미지, 경고 제거.
        DamagePlayerIfInCells(hitCells, attack1Damage);
        ClearWarning(hitCells);

        await UniTask.Delay(200, cancellationToken: cancelToken.Token); //쩌정!!!!
        anime.SetTrigger("Attack1Fin");
        
        await UniTask.Delay(2000, cancellationToken: cancelToken.Token); //공격 애니메이션이 끝날 때까지 대기
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

    // ===================== 패턴2 : 바위떨구기 =========================================================
    protected override async UniTask Phase1Pattern2()
    {
        anime.SetTrigger("Attack2");
        await UniTask.Delay(2300, cancellationToken: cancelToken.Token);

        GameObject Effect = Instantiate(swordGround, transform.position, Quaternion.identity);

        // 보스 위치를 고정된 좌표로 저장 (안전장치임 호오오옥시 몰라서)
        Vector2Int patternStartPos = this.gridPos;

        RockFalling(patternStartPos).Forget();

        await UniTask.Delay(8000, cancellationToken: cancelToken.Token);
        Destroy(Effect);

        anime.SetTrigger("Attack2Fin");
        await UniTask.Delay(2000, cancellationToken: cancelToken.Token);
        isBossAttacking = false;
    }

    private async UniTask RockFalling(Vector2Int centerPos)
    {
        for (int i = 0; i < 20; i++)
        {
            if (cancelToken.IsCancellationRequested) return;

            Vector2Int targetPos = Vector2Int.zero;
            bool isValidPosition = false;
            int safetyCounter = 0; // 혹시 모를 무한루프 방지용 안전 장치

            while (!isValidPosition && safetyCounter < 10)
            {
                safetyCounter++;
                int ranX = Random.Range(rockMinDis, rockMaxDis + 1) * (Random.Range(0, 2) == 0 ? 1 : -1);
                int ranY = Random.Range(rockMinDis, rockMaxDis + 1) * (Random.Range(0, 2) == 0 ? 1 : -1);

                targetPos = centerPos + new Vector2Int(ranX, ranY);

                if (GridSystem.Instance.IsWalkable(targetPos))
                {
                    isValidPosition = true;
                }
            }

            // 유효한 좌표를 찾은 경우에만 바위 투하 및 딜레이 작동
            if (isValidPosition)
            {
                FallSingleRockAsync(targetPos).Forget();

                // 바위와 바위 사이의 생성 간격 딜레이 (정상적인 좌표 생성 시에만 대기)
                await UniTask.Delay(rockDelayMs, cancellationToken: cancelToken.Token);
            }
            else
            {
                // 10번 시도했는데도 자리를 못 찾았다면 이번 루프는 그냥 패스 (우르르 나오는 현상 방지)
                i--;
                await UniTask.Yield(cancellationToken: cancelToken.Token); // 한 프레임 쉬고 다음 시도
            }
        }
    }

    private async UniTaskVoid FallSingleRockAsync(Vector2Int targetPos)
    {
        try
        {
            // 1. 매개변수로 안전하게 전달받은 targetPos 고정 사용
            ShowWarning(targetPos);
            Vector3 pos = GridSystem.Instance.GridToWorld(targetPos);
            GameObject effect = Instantiate(fallingRock, pos, Quaternion.identity);

            // 2. 경고 시간 대기
            await UniTask.Delay(rockWarnMs, cancellationToken: cancelToken.Token);

            // 3. 판정 및 경고 제거 (여전히 동일한 targetPos 유지)
            DamagePlayerIfInCell(targetPos, rockDamage);
            ClearWarning(targetPos);

            // 4. 이펙트 제거
            Destroy(effect, 1.5f);
        }
        catch (System.OperationCanceledException)
        {
            // 취소 처리
        }
    }

    //===========================================페이즈2 패턴 1 ============================================================
    protected override async UniTask Phase2Pattern1()
    {
        for (int i = 0; i < 10; i++)
        {
            if (cancelToken.IsCancellationRequested) break;
            // 1) 사라지는 연출. Teleport 애니메이션이 끝나 보스가 보이지 않게 된 시점까지 대기.
            anime.SetTrigger("Teleport");
            await UniTask.Delay(1000, cancellationToken: cancelToken.Token); //텔레포트(사라짐) 애니메이션 1초

            // 2) 사라진 동안(보이지 않을 때) 플레이어 옆 칸으로 실제 위치 점프. 양쪽 막히면 제자리에서 플레이어 방향으로 폴백.
            var dir = TeleportBesidePlayer();
            if (dir == Vector2Int.zero) dir = DirToPlayer();//두둥
            FaceDir(dir);//플레이어 방향보기

            // 3) 새 위치에서 등장 연출.
            anime.SetTrigger("Appear");//등장!
            await UniTask.Delay(500, cancellationToken: cancelToken.Token); //등장 애니메이션 0.5초

            // 4) 휘두르기 칸 계산(데이터) → 경고 표시(시각). 셀은 여기서 고정 — 윈드업 중 재계산 X.
            var hitCells = BuildSwingCells(gridPos, dir, attack1Range, attack1Width);
            ShowWarning(hitCells);

            // 5) 휘두르기 윈드업. 이 800ms가 플레이어가 빠져나가 피하는 유일한 창.
            anime.SetTrigger("Attack1");
            await UniTask.Delay(800, cancellationToken: cancelToken.Token); //0.8초가 그마나 공격 이펙트랑 맞는듯?
            GameObject Effect = Instantiate(attack1Effect, transform.position, Quaternion.identity); //공격 이펙트 생성
            Destroy(Effect, 1f); //이펙트 1초 후 제거 예약ㅇ미.

            // 6) 임팩트 순간: 경고 칸 안에 플레이어가 있으면 데미지, 경고 제거.
            DamagePlayerIfInCells(hitCells, attack1Damage);
            ClearWarning(hitCells);

            await UniTask.Delay(200, cancellationToken: cancelToken.Token); //쩌정!!!!
            anime.SetTrigger("Attack1Fin");

            await UniTask.Delay(2000, cancellationToken: cancelToken.Token); //공격 애니메이션이 끝날 때까지 대기
        }
        isBossAttacking = false;
    }

    protected override async UniTask Phase2Pattern2()
    {
        isBossAttacking = false;
    }
    protected override async UniTask Phase2Pattern3()
    {
        isBossAttacking = false;
    }
    //=============================== 플레이어 추적 이동 패턴 ========================================
    protected override async UniTask Phase1Pattern3()
    {
        await BossMoveStart(true);
        isBossAttacking = false;
    }
    //============================= 이동 로직 ==============================================

    public async UniTask BossMoveStart(bool chasePlayer)
    {
        isMove = true;
        anime.SetBool("Walk", true);
        // 지정된 횟수(stepsPerPattern)만큼 멈추지 않고 연속 이동 연출
        for (int i = 0; i < 10; i++)
        {
            if (cancelToken.IsCancellationRequested) break;

            bool moveGood = false;

            if (chasePlayer)
            {
                moveGood = await NextMoveToPlayer();
            }
            else
            {
                moveGood = await RanMove();
            }

            // 만약 사방이 막혀서 이동에 실패했다면 루프 탈출
            if (!moveGood) break;

            // 칸 사이의 딜레이를 0에 가깝게 주거나 moveTime과 맞춰야 끊기지 않습니다.
            // 연속으로 스르륵 움직이게 하려면 stepDelayMs를 0으로 주거나 아주 짧게(50ms) 설정.
            if (stepDelayMs > 0 && i < stepsPerPattern - 1)
            {
                await UniTask.Delay(stepDelayMs, cancellationToken: cancelToken.Token);
            }
            bossMoveGridUpdate();
        }
        anime.SetBool("Walk", false);
        isMove = false;
    }

    private async UniTask<bool> RanMove()
    {
        var dirArray = (Vector2Int[])Dirs.Clone();
        Array_Shuffle(dirArray);

        foreach (var dir in dirArray)
        {
            if (CheckAndUpateGrid(dir))
            {
                await MoveAnime();
                return true;
            }
        }
        return false;
    }

    private async UniTask<bool> NextMoveToPlayer()
    {
        if (player == null) return await RanMove();

        var girdDis = player.GridPosition - gridPos;
        var realDir = Vector2Int.zero;
        var orDir = Vector2Int.zero;

        // 더 멀리 떨어진 축을 주 방향으로 설정
        if (Mathf.Abs(girdDis.x) >= Mathf.Abs(girdDis.y))
        {
            if (girdDis.x != 0) realDir = new Vector2Int(System.Math.Sign(girdDis.x), 0);
            if (girdDis.y != 0) orDir = new Vector2Int(0, System.Math.Sign(girdDis.y));
        }
        else
        {
            if (girdDis.y != 0) realDir = new Vector2Int(0, System.Math.Sign(girdDis.y));
            if (girdDis.x != 0) orDir = new Vector2Int(System.Math.Sign(girdDis.x), 0);
        }

        // 1순위: 플레이어와 더 멀어진 축 방향으로 전진
        if (realDir != Vector2Int.zero && CheckAndUpateGrid(realDir)) { await MoveAnime(); return true; }
        // 2순위: 막혔다면 차선책 축 방향으로 전진
        if (orDir != Vector2Int.zero && CheckAndUpateGrid(orDir)) { await MoveAnime(); return true; }

        // 다 막혔으면 우회하기 위해 랜덤 이동
        return await RanMove();
    }

    private bool CheckAndUpateGrid(Vector2Int dir)
    {
        var nextPos = gridPos + dir;

        if (!GridSystem.Instance.IsWalkable(nextPos)) return false;
        if (player != null && nextPos == player.GridPosition) return false;

        FaceDir(dir); // 이동 방향으로 스프라이트 반전 (좌우 이동만, 상하는 기존 방향 유지)
        gridPos = nextPos;
        return true;
    }

    private async UniTask MoveAnime()
    {
        var targetPos = GridSystem.Instance.GridToWorld(gridPos);
        bool moveCom = false;
        
        transform.DOMove(targetPos, moveTime)
            .SetEase(moveEase)
            .OnComplete(() => moveCom = true);

        await UniTask.WaitUntil(() => moveCom, cancellationToken: cancelToken.Token);
    }

    private static void Array_Shuffle(Vector2Int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    void bossMoveGridUpdate()
    {
        gridPos = GridSystem.Instance.WorldToGrid(transform.position);
    }
}
