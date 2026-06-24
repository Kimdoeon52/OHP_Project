using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// Phase 2 패턴2로 소환되는 작은 해골.
// 8방향으로 배회하다 lifetimeSec 후 자동 소멸. 플레이어와 같은 타일에 있으면 damageCooldownSec 간격으로 데미지.
public class MiniSkull : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private int moveIntervalMs = 400;     // 한 칸 이동마다 대기 시간
    [SerializeField] private float moveTime = 0.35f;        // DOMove 보간 시간 (moveIntervalMs와 비슷하게)
    [SerializeField] private Ease moveEase = Ease.Linear;

    [Header("생존 / 데미지")]
    [SerializeField] private float lifetimeSec = 10f;       // 자동 소멸까지 시간
    [SerializeField] private int contactDamage = 1;         // 같은 타일에 있을 때 1회 데미지
    [SerializeField] private float damageCooldownSec = 1f;  // 같은 미니해골에서 다음 데미지까지 쿨

    // 8방향 (상하좌우 + 대각선)
    private static readonly Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        Vector2Int.left + Vector2Int.up,  Vector2Int.left + Vector2Int.down,
        Vector2Int.right + Vector2Int.up, Vector2Int.right + Vector2Int.down,
    };

    private Vector2Int gridPos;
    private Vector2Int currentDir = Vector2Int.zero;  // 현재 이동 방향 (막힐 때까지 유지)
    private GridPlayer player;
    private CancellationTokenSource cts;
    private float lastDamageTime = -999f;

    private void OnEnable()
    {
        cts = new CancellationTokenSource();
        player = FindFirstObjectByType<GridPlayer>();

        // 스폰 위치를 가장 가까운 격자 셀로 스냅 (소환자가 GridToWorld를 이미 했더라도 안전)
        gridPos = GridSystem.Instance.WorldToGrid(transform.position);
        transform.position = GridSystem.Instance.GridToWorld(gridPos);

        WanderLoop().Forget();
        SelfDestructAfter(lifetimeSec).Forget();
    }

    private void OnDestroy()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private void Update()
    {
        // 같은 타일에 플레이어가 있으면 쿨 안에서 1회 데미지. 미니해골이 멈춰있을 때도, 플레이어가 들어왔을 때도 모두 잡힘.
        if (player == null || player.IsDead) return;
        if (player.GridPosition != gridPos) return;
        if (Time.time - lastDamageTime < damageCooldownSec) return;

        player.TakeDamage(BossBase.ScaleBossDamage(contactDamage));
        lastDamageTime = Time.time;
    }

    private async UniTaskVoid WanderLoop()
    {
        try
        {
            while (!cts.IsCancellationRequested)
            {
                if (!TryStepRandom())
                {
                    // 8방향 다 막혔으면 잠깐 쉬었다가 재시도 (벽 코너에 갇힌 경우 대비)
                    await UniTask.Delay(moveIntervalMs, cancellationToken: cts.Token);
                    continue;
                }
                await UniTask.Delay(moveIntervalMs, cancellationToken: cts.Token);
            }
        }
        catch (System.OperationCanceledException) { /* 파괴 시 정상 취소 */ }
    }

    // 현재 방향(currentDir)으로 한 칸 전진. 막혔거나 방향이 없으면 새 방향을 골라서 시도.
    // 8방향 다 막혔으면 false 반환 (호출자가 한 틱 쉬고 재시도).
    private bool TryStepRandom()
    {
        // 1) 기존 방향이 살아있고 계속 갈 수 있으면 그대로 직진.
        if (currentDir != Vector2Int.zero && TryMove(currentDir)) return true;

        // 2) 막혔거나 첫 이동: 8방향 중 walkable한 후보들 모아서 (가능하면 직전 방향 빼고) 무작위 선택.
        var candidates = new List<Vector2Int>();
        foreach (var d in Dirs)
        {
            if (d == currentDir) continue; // 같은 방향은 위에서 이미 막힌 게 확인됨
            if (GridSystem.Instance.IsWalkable(gridPos + d)) candidates.Add(d);
        }

        if (candidates.Count == 0) return false;

        var pick = candidates[Random.Range(0, candidates.Count)];
        currentDir = pick;
        return TryMove(pick);
    }

    // dir 방향으로 한 칸 이동 성공 시 true. gridPos/currentDir/트윈 모두 갱신.
    private bool TryMove(Vector2Int dir)
    {
        var next = gridPos + dir;
        if (!GridSystem.Instance.IsWalkable(next)) return false;

        gridPos = next;
        currentDir = dir;
        var world = GridSystem.Instance.GridToWorld(next);
        transform.DOMove(world, moveTime).SetEase(moveEase);
        return true;
    }

    private async UniTaskVoid SelfDestructAfter(float sec)
    {
        try
        {
            await UniTask.Delay((int)(sec * 1000), cancellationToken: cts.Token);
            if (this != null) Destroy(gameObject);
        }
        catch (System.OperationCanceledException) { }
    }
}
