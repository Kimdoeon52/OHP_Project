using Cysharp.Threading.Tasks;
using DG.Tweening;
using Mono.Cecil;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BossB : BossBase
{
    [Header("이동 설정")]
    [SerializeField] private float moveTime = 0.25f; //이동하는데 걸리는 시간
    [SerializeField] private Ease moveEase = Ease.Linear; //스르륵 미끄러지게 끝나는 두트윈임.
    [SerializeField] private int stepsPerPattern = 4; //이동 횟수
    [SerializeField] private int stepDelayMs = 1000; //이동 딜레이

    [Header("보스 공격 이펙트")]
    [SerializeField] private GameObject attack1Effect; //쿵!
    [SerializeField] private GameObject fallingRock; //떨어지는 바위 프리팹
    [SerializeField] private GameObject swordGround; //땅에 칼 박아버리는거

    [Header("패턴2설정")]
    [SerializeField] private int rockMinDis = 1; // 보스로부터 최소 1칸 떨어진 곳
    [SerializeField] private int rockMaxDis = 3; // 보스로부터 최대 3칸 떨어진 곳
    [SerializeField] private int rockDelayMs = 500; //0.5초...?

    //[Header("보스 씬 타일맵 (GridSystem 없는 씬에서 연결)")]
    //[SerializeField] private Tilemap floorTilemap;

    private Vector2Int gridPos;
    private bool isMove = false;
    private Animator anime;
    
    // 4방향 이동을 위한 벡터 배열임
    private static Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    protected override void Start()
    {
        base.Start();
        currentHp = maxHp; // 보스 체력 설정
        AddActionCase(BossAction.Pattern1, 10); // 평타
        AddActionCase(BossAction.Pattern2, 10); // 하늘
        AddActionCase(BossAction.Pattern3, 80); // 이동
        anime = GetComponent<Animator>();
    }

    protected void BossMove() //보스가 움직여요~
    {
        isMove = true;
        //움직이는 로직 구현
        isMove = false;
    }

    private void DamagePlayerIfInCell(Vector2Int attackCell, int damage)
    {
        if (player == null || player.IsDead) return;

        if (player.GridPosition == attackCell)
        {
            player.TakeDamage(damage);
        }
    }
    //========= 패턴1 : 일반 공격 ====================================================
    protected override async UniTask Phase1Pattern1()
    {
        anime.SetTrigger("Attack1");
        //공격 로직 구현
        await UniTask.Delay(800, cancellationToken: cancelToken.Token); //1.2초+0.2초 1.4초가 그마나 공격 이펙트랑 맞는듯?
        GameObject Effect = Instantiate(attack1Effect, transform.position, Quaternion.identity); //공격 이펙트 생성
        Destroy(Effect, 1f); //이펙트 1초 후 제거 예약ㅇ미.
        await UniTask.Delay(200, cancellationToken: cancelToken.Token); //쩌정!!!!
        anime.SetTrigger("Attack1Fin");
        
        await UniTask.Delay(2000, cancellationToken: cancelToken.Token); //공격 애니메이션이 끝날 때까지 대기
        isBossAttacking = false;
    }

    // ===================== 패턴2 : 바위떨구기 =========================================================
    protected override async UniTask Phase1Pattern2()
    {
        anime.SetTrigger("Attack2");
        await UniTask.Delay(2300, cancellationToken: cancelToken.Token); //2.5초가 이펙트랑 맞는듯?
        GameObject Effect = Instantiate(swordGround, transform.position, Quaternion.identity); //공격 이펙트 생성
        //공격 로직 구현
        RockFalling().Forget(); //바위 떨어져유~
        await UniTask.Delay(6000, cancellationToken: cancelToken.Token); //애니메이션이 끝날 때까지 대기
        Destroy(Effect); //이펙트 제거
        anime.SetTrigger("Attack2Fin");
        await UniTask.Delay(2000, cancellationToken: cancelToken.Token); //애니메이션이 끝날 때까지 대기
        isBossAttacking = false;
    }
    private async UniTaskVoid RockFalling()
    {
        for (int i = 0; i < 12; i++) //12번은 떨어져야제~~~
        {
            // 취소 토큰임
            if (cancelToken.IsCancellationRequested) return;

            // 보스 주변 격자 범위 내에서 랜덤 오프셋 계산
            int ranX = Random.Range(rockMinDis, rockMaxDis + 1) * (Random.Range(0, 2) == 0 ? 1 : -1);
            int ranY = Random.Range(rockMinDis, rockMaxDis + 1) * (Random.Range(0, 2) == 0 ? 1 : -1);

            Vector2Int targetPos = gridPos + new Vector2Int(ranX, ranY);

            // GridSystem을 사용하여 격자 좌표를 월드 좌표로 변환
            // 만약 맵 밖이나 벽 안이라면 굳이 생성하지 않고 걸러내기 위해 IsWalkable을 체크해 주면 좋습니다.
            if (GridSystem.Instance.IsWalkable(targetPos))
            {
                Vector3 pos = GridSystem.Instance.GridToWorld(targetPos);

                // 바위 프리팹 생성
                GameObject effect = Instantiate(fallingRock, pos, Quaternion.identity);

                Destroy(effect, 1.5f);
            }
            else
            {
                // 만약 선택된 좌표가 맵 밖이라 생성을 건너뛰었다면, 횟수를 메꾸기 위해 index를 하나 되돌림
                i--;
            }

            // 바위와 바위 사이의 생성 간격 딜레이 (예: 0.5초씩 6번 = 총 3초 동안 떨어짐)
            await UniTask.Delay(rockDelayMs, cancellationToken: cancelToken.Token);
        }
    }
    //===============================임시 이동 패턴========================================
    protected override async UniTask Phase1Pattern3()
    {
        await BossMoveStart(false);
        isBossAttacking = false;
    }
    //============================= 이동 로직 ==============================================

    public async UniTask BossMoveStart(bool chasePlayer)
    {
        isMove = true;
        anime.SetBool("Walk", true);
        // 지정된 횟수(stepsPerPattern)만큼 멈추지 않고 연속 이동 연출
        for (int i = 0; i < 1; i++)
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

    //========================================== 타격 범위 생성 ============================================
    void CreateHitBox(Vector2Int bossPos, int range)
    {
        // 내 중심 좌표에서 range 범위 내의 격자 좌표를 순회
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                Vector2Int pos = bossPos + new Vector2Int(x, y);
                if (GridSystem.Instance.IsWalkable(pos))
                {
                    Debug.Log($"보스 타격 범위 격자: {pos}");
                }
            }
        }
    }

    void bossMoveGridUpdate()
    {
        var bossPos = GridSystem.Instance.WorldToGrid(transform.position);
        CreateHitBox(bossPos, 3);
    }
}
