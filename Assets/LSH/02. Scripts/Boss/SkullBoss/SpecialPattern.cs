using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SpecialPattern : MonoBehaviour
{
    [Header("패시브 오브젝트 프리팹")]
    [SerializeField] private GameObject crystalPrefab;

    [Header("크리스탈 비 설정")]
    [SerializeField] private int rainTickMs = 300;
    [SerializeField] private int rainCellsPerTick = 3;
    [SerializeField] private int crystalWarnMs = 1000;
    [SerializeField] private int crystalDamage = 1;
    [SerializeField] private Color crystalTint = Color.red;
    [SerializeField] private float crystalScale = 1f;
    [SerializeField] private float crystalLifetimeSec = 1.5f;

    private BossBase boss;
    private CancellationTokenSource cts;
    private List<Vector2Int> walkableCellsCache;
    private bool isRunning = false;

    private void Awake()
    {
        boss = GetComponent<BossBase>();
    }

    public void StartSpecialPattern()
    {
        if (isRunning) return;
        isRunning = true;

        cts = new CancellationTokenSource();
        
        CacheWalkableCells();

        CrystalRainLoop().Forget();
    }

    public void StopSpecialPattern()
    {
        if (!isRunning) return;
        isRunning = false;

        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }

    private void OnDestroy()
    {
        StopSpecialPattern();
    }

    private void CacheWalkableCells()
    {
        walkableCellsCache = new List<Vector2Int>();
        if (GridSystem.Instance == null) return;

        foreach (var cell in GridSystem.Instance.GetAllWalkableCells())
        {
            walkableCellsCache.Add(cell);
        }
    }

    private async UniTask CrystalRainLoop()
    {
        try
        {
            while (cts != null && !cts.IsCancellationRequested)
            {
                if (walkableCellsCache != null && walkableCellsCache.Count > 0)
                {
                    for (int i = 0; i < rainCellsPerTick; i++)
                    {
                        var cell = walkableCellsCache[Random.Range(0, walkableCellsCache.Count)];
                        FallSingleCrystalAsync(cell).Forget();
                    }
                }
                await UniTask.Delay(rainTickMs, cancellationToken: cts.Token);
            }
        }
        catch (System.OperationCanceledException) { }
    }

    private async UniTask FallSingleCrystalAsync(Vector2Int targetPos)
    {
        if (boss == null) return;

        boss.ShowWarning(targetPos);
        bool warningCleared = false;
        try
        {
            await UniTask.Delay(crystalWarnMs, cancellationToken: cts.Token);

            if (crystalPrefab != null)
            {
                Vector3 pos = GridSystem.Instance.GridToWorld(targetPos);
                GameObject go = Instantiate(crystalPrefab, pos, Quaternion.identity);
                
                Phase2Crystal p2c = go.GetComponent<Phase2Crystal>();
                if (p2c != null)
                {
                    p2c.SetupWithGridBlock(crystalTint, crystalScale, crystalLifetimeSec, targetPos);
                }
                else
                {
                    Crystal c = go.GetComponent<Crystal>();
                    if (c != null)
                    {
                        c.Setup(crystalTint, crystalScale, crystalLifetimeSec);
                    }
                    else
                    {
                        Destroy(go, crystalLifetimeSec);
                    }
                }
            }

            // 즉각적인 타격을 위해 시각적 경고를 먼저 해제
            if (boss != null)
            {
                boss.ClearWarning(targetPos);
                warningCleared = true;
            }

            if (boss.Player != null && !boss.Player.IsDead && boss.Player.GridPosition == targetPos)
            {
                boss.Player.TakeDamage(crystalDamage);

                // 끼임 방지: 크리스탈에 맞은 플레이어는 근처 이동 가능한 타일로 튕겨냄
                Vector2Int? knockbackTarget = FindKnockbackTargetNear(targetPos);
                if (knockbackTarget.HasValue && boss.Player.Movement != null)
                {
                    boss.Player.Movement.Knockback(knockbackTarget.Value);
                    Debug.Log($"[SpecialPattern] 플레이어가 크리스탈에 맞음 -> {knockbackTarget.Value}로 튕겨냄 (끼임 방지)");
                }
            }
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            if (boss != null && !warningCleared)
            {
                boss.ClearWarning(targetPos);
            }
        }
    }

    private Vector2Int? FindKnockbackTargetNear(Vector2Int playerPos)
    {
        if (GridSystem.Instance == null) return null;

        // 4방향 인접 격자 우선 탐색
        var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        ShuffleDirections(directions);

        foreach (var dir in directions)
        {
            var candidate = playerPos + dir;
            if (GridSystem.Instance.IsInBounds(candidate) && 
                GridSystem.Instance.IsWalkable(candidate))
            {
                // 다른 몬스터나 보스가 점유 중이 아닌지 체크
                if (TurnManager.Instance != null && TurnManager.Instance.GetEntityAt(candidate) == null)
                {
                    return candidate;
                }
            }
        }

        // 대각선 4방향 2차 탐색
        var diagonalDirs = new[] {
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };
        ShuffleDirections(diagonalDirs);

        foreach (var dir in diagonalDirs)
        {
            var candidate = playerPos + dir;
            if (GridSystem.Instance.IsInBounds(candidate) && 
                GridSystem.Instance.IsWalkable(candidate) )
            {
                if (TurnManager.Instance != null && TurnManager.Instance.GetEntityAt(candidate) == null)
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private void ShuffleDirections(Vector2Int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }
}
