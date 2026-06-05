using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SpecialPattern : MonoBehaviour
{
    [Header("패시브 오브젝트 프리팹")]
    [SerializeField] private GameObject crystalPrefab;
    [SerializeField] private GameObject miniSkullPrefab;

    [Header("크리스탈 비 설정")]
    [SerializeField] private int rainTickMs = 300;
    [SerializeField] private int rainCellsPerTick = 3;
    [SerializeField] private int crystalWarnMs = 1000;
    [SerializeField] private int crystalDamage = 1;
    [SerializeField] private Color crystalTint = Color.red;
    [SerializeField] private float crystalScale = 1f;
    [SerializeField] private float crystalLifetimeSec = 1.5f;

    [Header("미니해골 소환 설정")]
    [SerializeField] private int miniSkullCount = 2;
    [SerializeField] private int miniSkullLifetimeMs = 10000;
    [SerializeField] private int skullSearchRadius = 4;

    private BossBase boss;
    private CancellationTokenSource cts;
    private List<Vector2Int> walkableCellsCache;
    private readonly List<GameObject> activeMiniSkulls = new List<GameObject>();
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
        MiniSkullPassiveLoop().Forget();
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

        DestroyAllMiniSkulls();
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
        try
        {
            await UniTask.Delay(crystalWarnMs, cancellationToken: cts.Token);

            if (crystalPrefab != null)
            {
                Vector3 pos = GridSystem.Instance.GridToWorld(targetPos);
                GameObject go = Instantiate(crystalPrefab, pos, Quaternion.identity);
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

            if (boss.Player != null && !boss.Player.IsDead && boss.Player.GridPosition == targetPos)
            {
                boss.Player.TakeDamage(crystalDamage);
            }
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            if (boss != null)
            {
                boss.ClearWarning(targetPos);
            }
        }
    }

    private async UniTask MiniSkullPassiveLoop()
    {
        try
        {
            while (cts != null && !cts.IsCancellationRequested)
            {
                if (miniSkullPrefab != null)
                {
                    var spawnCells = PickRandomNearbyWalkable(miniSkullCount, skullSearchRadius);
                    foreach (var cell in spawnCells)
                    {
                        var world = GridSystem.Instance.GridToWorld(cell);
                        var skull = Instantiate(miniSkullPrefab, world, Quaternion.identity);
                        activeMiniSkulls.Add(skull);
                    }
                }
                await UniTask.Delay(miniSkullLifetimeMs, cancellationToken: cts.Token);

                activeMiniSkulls.RemoveAll(s => s == null);
            }
        }
        catch (System.OperationCanceledException) { }
    }

    private List<Vector2Int> PickRandomNearbyWalkable(int count, int searchRadius)
    {
        var candidates = new List<Vector2Int>();
        if (boss == null) return candidates;

        Vector2Int center = boss.GridPosition;

        for (int dx = -searchRadius; dx <= searchRadius; dx++)
        {
            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                var c = center + new Vector2Int(dx, dy);
                
                // 보스 위치 제외
                if (c.x >= center.x && c.x < center.x + boss.BossSize.x &&
                    c.y >= center.y && c.y < center.y + boss.BossSize.y)
                    continue;

                if (GridSystem.Instance.IsWalkable(c)) 
                    candidates.Add(c);
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

    private void DestroyAllMiniSkulls()
    {
        foreach (var skull in activeMiniSkulls)
        {
            if (skull != null) Destroy(skull);
        }
        activeMiniSkulls.Clear();
    }
}
