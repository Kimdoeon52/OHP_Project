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

    //[Header("보스 씬 타일맵 (GridSystem 없는 씬에서 연결)")]
    //[SerializeField] private Tilemap floorTilemap;

    private Vector2Int gridPos;
    private GridPlayer player;

    // 4방향 이동을 위한 벡터 배열임
    private static Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    protected override void Start()
    {
        base.Start();
        CurrentHp = 100; // 보스 체력 설정
        AddActionCase(BossAction.Pattern1, 50); // 랜덤 이동
        AddActionCase(BossAction.Pattern2, 50); // 플레이어 추적

        gridPos = GridSystem.Instance.WorldToGrid(transform.position); // 초기 위치를 격자 좌표로 변환하여 저장
        player = FindFirstObjectByType<GridPlayer>(); //플레이어 찾기
    }

    //========= 패턴1 : 랜덤 이동 ====================================================
    protected override async UniTask Phase1Pattern1()
    {
        Debug.Log($"<color=red>[보스 랜덤 이동패턴]</color> ");
        for (int i = 0; i < stepsPerPattern; i++)
        {
            await StepRandom(); //랜덤 이동 시도
            await UniTask.Delay(stepDelayMs, cancellationToken: cancelToken.Token);
        }
        isBossAttacking = false;
    }

    // ===================== 패턴2 : 플레이어 추적 =========================================================
    protected override async UniTask Phase1Pattern2()
    {
        Debug.Log($"<color=red>[보스 플레이어 추적 패턴1]</color> ");
        for (int i = 0; i < 12; i++)
        {
            await StepTowardPlayer();
            await UniTask.Delay(stepDelayMs, cancellationToken: cancelToken.Token);
        }
        isBossAttacking = false;
    }

    //============================= 이동 로직 ==============================================

    private async UniTask StepRandom()
    {
        var dirs = (Vector2Int[])Dirs.Clone();
        Shuffle(dirs);

        foreach (var dir in dirs)
        {
            if (TryStep(dir))
            {
                await AnimateMove();
                return;
            }
        }
        // 이동 가능 방향 없으면 대기만
        await UniTask.Delay((int)(moveTime * 1000), cancellationToken: cancelToken.Token);
    }

    private async UniTask StepTowardPlayer()
    {
        if (player == null) { await StepRandom(); return; } //플레이어가 없으면 랜덤 이동

        var diff = player.GridPosition - gridPos; // 플레이어와의 격자 좌표 차이 계산
        var dirX = diff.x != 0 ? new Vector2Int(System.Math.Sign(diff.x), 0) : Vector2Int.zero; // 플레이어와의 격자 좌표 차이에 따라 우선적으로 시도할 방향과 보조적으로 시도할 방향을 결정
        var dirY = diff.y != 0 ? new Vector2Int(0, System.Math.Sign(diff.y)) : Vector2Int.zero;
        bool xFirst = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);

        var primary = xFirst ? dirX : dirY; // 플레이어와의 격자 좌표 차이에 따라 우선적으로 시도할 방향과 보조적으로 시도할 방향을 결정
        var secondary = xFirst ? dirY : dirX; // 플레이어와의 격자 좌표 차이에 따라 우선적으로 시도할 방향과 보조적으로 시도할 방향을 결정

        if (primary   != Vector2Int.zero && TryStep(primary))   { await AnimateMove(); return; }
        if (secondary != Vector2Int.zero && TryStep(secondary)) { await AnimateMove(); return; }

        // 플레이어 방향이 막히면 랜덤으로 우회
        await StepRandom();
    }

    private bool TryStep(Vector2Int dir)
    {
        var next = gridPos + dir; // 다음 위치 계산
        if (!GridSystem.Instance.IsWalkable(next)) return false; // 다음 위치가 걸을 수 없는 곳이면 실패
        if (player != null && next == player.GridPosition) return false; // 다음 위치에 플레이어가 있으면 실패
        gridPos = next;
        return true;
    }

    private async UniTask AnimateMove()
    {
        var target = GridSystem.Instance.GridToWorld(gridPos); // 격자 좌표를 월드 좌표로 변환하여 이동 목표 지점 계산
        bool done = false;
        transform.DOMove(target, moveTime)
            .SetEase(moveEase)
            .OnComplete(() => done = true);
        await UniTask.WaitUntil(() => done, cancellationToken: cancelToken.Token);
    }

    private static void Shuffle(Vector2Int[] arr) 
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }
}
