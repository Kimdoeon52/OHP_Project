using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BossB : BossBase
{
    [Header("이동 설정")]
    [SerializeField] private float moveTime = 0.25f; //이동하는데 걸리는 시간
    [SerializeField] private Ease moveEase = Ease.OutBack; //Ease.OutBack는 살짝 튕기는 느낌으로 이동이 끝나는 두트윈임.
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
    private GridPlayer player;
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
        AddActionCase(BossAction.Pattern1, 50); // 랜덤 이동
        AddActionCase(BossAction.Pattern2, 50); // 플레이어 추적
        AddActionCase(BossAction.Pattern3, 0); // 패턴3 없음
        anime = GetComponent<Animator>();
    }

    protected void BossMove() //보스가 움직여요~
    {
        isMove = true;
        //움직이는 로직 구현
        isMove = false;
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
    ////============================= 이동 로직 ==============================================

    //private async UniTask StepRandom()
    //{
    //    var dirs = (Vector2Int[])Dirs.Clone();
    //    Shuffle(dirs);

    //    foreach (var dir in dirs)
    //    {
    //        if (TryStep(dir))
    //        {
    //            await AnimateMove();
    //            return;
    //        }
    //    }
    //    // 이동 가능 방향 없으면 대기만
    //    await UniTask.Delay((int)(moveTime * 1000), cancellationToken: cancelToken.Token);
    //}

    //private async UniTask StepTowardPlayer()
    //{
    //    if (player == null) { await StepRandom(); return; } //플레이어가 없으면 랜덤 이동

    //    var diff = player.GridPosition - gridPos; // 플레이어와의 격자 좌표 차이 계산
    //    var dirX = diff.x != 0 ? new Vector2Int(System.Math.Sign(diff.x), 0) : Vector2Int.zero; // 플레이어와의 격자 좌표 차이에 따라 우선적으로 시도할 방향과 보조적으로 시도할 방향을 결정
    //    var dirY = diff.y != 0 ? new Vector2Int(0, System.Math.Sign(diff.y)) : Vector2Int.zero;
    //    bool xFirst = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);

    //    var primary = xFirst ? dirX : dirY; // 플레이어와의 격자 좌표 차이에 따라 우선적으로 시도할 방향과 보조적으로 시도할 방향을 결정
    //    var secondary = xFirst ? dirY : dirX; // 플레이어와의 격자 좌표 차이에 따라 우선적으로 시도할 방향과 보조적으로 시도할 방향을 결정

    //    if (primary   != Vector2Int.zero && TryStep(primary))   { await AnimateMove(); return; }
    //    if (secondary != Vector2Int.zero && TryStep(secondary)) { await AnimateMove(); return; }

    //    // 플레이어 방향이 막히면 랜덤으로 우회
    //    await StepRandom();
    //}

    //private bool TryStep(Vector2Int dir)
    //{
    //    var next = gridPos + dir; // 다음 위치 계산
    //    if (!GridSystem.Instance.IsWalkable(next)) return false; // 다음 위치가 걸을 수 없는 곳이면 실패
    //    if (player != null && next == player.GridPosition) return false; // 다음 위치에 플레이어가 있으면 실패
    //    gridPos = next;
    //    return true;
    //}

    //private async UniTask AnimateMove()
    //{
    //    var target = GridSystem.Instance.GridToWorld(gridPos); // 격자 좌표를 월드 좌표로 변환하여 이동 목표 지점 계산
    //    bool done = false;
    //    transform.DOMove(target, moveTime)
    //        .SetEase(moveEase)
    //        .OnComplete(() => done = true);
    //    await UniTask.WaitUntil(() => done, cancellationToken: cancelToken.Token);
    //}

    //private static void Shuffle(Vector2Int[] arr) 
    //{
    //    for (int i = arr.Length - 1; i > 0; i--)
    //    {
    //        int j = Random.Range(0, i + 1);
    //        (arr[i], arr[j]) = (arr[j], arr[i]);
    //    }
    //}
}
