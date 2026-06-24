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
    [SerializeField] private int crystalHitDelayMs = 150; // 경고 해제 후 실제 데미지가 들어가는 딜레이(ms)

    [Header("가장자리 크리스탈 설정")]
    [SerializeField] private GameObject edgeCrystalPrefab; // 가장자리 벽 전용 크리스탈 프리팹 (비워둘 시 폴백용 crystalPrefab 사용)
    [SerializeField] private Color edgeCrystalColor = Color.blue;
    [SerializeField] private float edgeCrystalScale = 1.2f;

    private BossBase boss;
    private CancellationTokenSource cts;
    private List<Vector2Int> walkableCellsCache;
    private bool isRunning = false;
    private List<Vector2Int> paintedCells = new List<Vector2Int>();
    private List<GameObject> spawnedEdgeCrystals = new List<GameObject>();

    private void Awake()
    {
        boss = GetComponent<BossBase>();
    }

    public void StartSpecialPattern()
    {
        StartPhase3SpecialPattern();
    }

    public void StartPhase2SpecialPattern()
    {
        if (isRunning) return;
        isRunning = true;

        cts = new CancellationTokenSource();

        CacheWalkableCells();
        CrystalRainLoop().Forget();
    }

    public void StartPhase3SpecialPattern()
    {
        if (!isRunning)
        {
            isRunning = true;
            cts = new CancellationTokenSource();
            ExecuteEdgePattern();
            CacheWalkableCells();
            CrystalRainLoop().Forget();
            return;
        }

        ExecuteEdgePattern();
        CacheWalkableCells();
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

        ClearPaintedTiles();
    }

    private void OnDestroy()
    {
        StopSpecialPattern();
    }

    private async UniTaskVoid CrystalRainLoop()
    {
        var token = cts.Token;
        try
        {
            while (isRunning && cts != null && !token.IsCancellationRequested)
            {
                if (walkableCellsCache != null && walkableCellsCache.Count > 0)
                {
                    for (int i = 0; i < rainCellsPerTick; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        var cell = walkableCellsCache[Random.Range(0, walkableCellsCache.Count)];
                        FallSingleCrystalAsync(cell).Forget();
                    }
                }
                await UniTask.Delay(rainTickMs, cancellationToken: token);
            }
        }
        catch (System.OperationCanceledException) { }
    }

    private void ExecuteEdgePattern()
    {
        GameObject prefabToUse = edgeCrystalPrefab != null ? edgeCrystalPrefab : crystalPrefab;
        if (boss == null || !boss.HasMapBounds || GridSystem.Instance == null || prefabToUse == null) return;

        RectInt bounds = boss.MapBounds;

        ClearPaintedTiles();

        // 맵 가장자리에서 고정 3칸 두께로 벽 생성
        int xEdge = 3;
        int yEdge = 3;

        // 가장자리 xEdge, yEdge 칸만큼 크리스탈을 배치하여 좁힘
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                // 가장자리 영역 조건
                if (x < bounds.xMin + xEdge || x >= bounds.xMax - xEdge || 
                    y < bounds.yMin + yEdge || y >= bounds.yMax - yEdge)
                {
                    // 바닥 또는 함정 타일이 있어 원래 다닐 수 있는 길일 때만 크리스탈을 소환
                    var hasFloor = GridSystem.Instance.Tilemaps.floor != null && GridSystem.Instance.Tilemaps.floor.HasTile(new Vector3Int(x, y, 0));
                    var hasHazard = GridSystem.Instance.Tilemaps.hazard != null && GridSystem.Instance.Tilemaps.hazard.HasTile(new Vector3Int(x, y, 0));

                    if (hasFloor || hasHazard)
                    {
                        Vector3 worldPos = GridSystem.Instance.GridToWorld(cell);
                        GameObject go = Instantiate(prefabToUse, worldPos, Quaternion.identity);
                        
                        // 프리팹 컴포넌트에 의존하지 않고 SpecialPattern이 직접 이동 불가를 확실하게 세팅
                        GridSystem.Instance.BlockCell(cell);

                        Phase2Crystal p2c = go.GetComponent<Phase2Crystal>();
                        if (p2c != null)
                        {
                            p2c.Setup(edgeCrystalColor, edgeCrystalScale, 9999f);
                        }
                        else
                        {
                            Crystal c = go.GetComponent<Crystal>();
                            if (c != null)
                            {
                                c.Setup(edgeCrystalColor, edgeCrystalScale, 9999f);
                            }
                        }

                        spawnedEdgeCrystals.Add(go);
                        paintedCells.Add(cell);
                    }
                }
            }
        }

        // 플레이어가 갇힌 경우 밀어냄
        PushPlayerIfInsideBlockedArea();

        // 비전 업데이트
        if (boss.Player != null && VisionSystem.Instance != null)
        {
            VisionSystem.Instance.UpdateVision(boss.Player.GridPosition);
        }
    }

    private void CacheWalkableCells()
    {
        walkableCellsCache = new List<Vector2Int>();
        if (GridSystem.Instance == null) return;

        foreach (var cell in GridSystem.Instance.GetAllWalkableCells())
        {
            // 칠해진 벽 구역을 제외하고 캐싱하여 해당 격자 위에만 비가 오게 함
            if (!paintedCells.Contains(cell))
            {
                walkableCellsCache.Add(cell);
            }
        }
    }

    private async UniTaskVoid FallSingleCrystalAsync(Vector2Int targetPos)
    {
        if (boss == null || cts == null || cts.IsCancellationRequested) return;

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
                    p2c.Setup(crystalTint, crystalScale, crystalLifetimeSec);
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

            // 크리스탈 낙하 애니메이션 싱크를 위해 딜레이 추가
            await UniTask.Delay(crystalHitDelayMs, cancellationToken: cts.Token);

            if (boss.Player != null && !boss.Player.IsDead && boss.Player.GridPosition == targetPos)
            {
                boss.Player.TakeDamage(BossBase.ScaleBossDamage(crystalDamage));
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

    private void ClearPaintedTiles()
    {
        if (GridSystem.Instance != null)
        {
            foreach (var cell in paintedCells)
            {
                GridSystem.Instance.UnblockCell(cell);
            }
        }

        foreach (var go in spawnedEdgeCrystals)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }
        spawnedEdgeCrystals.Clear();
        paintedCells.Clear();

        if (boss != null && boss.Player != null && VisionSystem.Instance != null)
        {
            VisionSystem.Instance.UpdateVision(boss.Player.GridPosition);
        }
    }

    private void PushPlayerIfInsideBlockedArea()
    {
        if (boss == null || boss.Player == null || boss.Player.IsDead || boss.Player.Movement == null) return;

        Vector2Int playerPos = boss.Player.GridPosition;

        if (paintedCells.Contains(playerPos))
        {
            Vector2Int? targetPos = FindNearestWalkableCell(playerPos);
            if (targetPos.HasValue)
            {
                boss.Player.Movement.Knockback(targetPos.Value);
                Debug.Log($"[SpecialPattern] 플레이어가 설치 구역에 갇힘 -> {targetPos.Value} 격자로 밀쳐냄");
            }
        }
    }

    private Vector2Int? FindNearestWalkableCell(Vector2Int startPos)
    {
        if (GridSystem.Instance == null) return null;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(startPos);
        visited.Add(startPos);

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();

            if (GridSystem.Instance.IsWalkable(curr) && 
                (TurnManager.Instance == null || TurnManager.Instance.GetEntityAt(curr) == null) &&
                !paintedCells.Contains(curr))
            {
                return curr;
            }

            Vector2Int[] neighbors = {
                curr + Vector2Int.up,
                curr + Vector2Int.down,
                curr + Vector2Int.left,
                curr + Vector2Int.right
            };

            foreach (var n in neighbors)
            {
                if (!visited.Contains(n) && GridSystem.Instance.IsInBounds(n))
                {
                    visited.Add(n);
                    queue.Enqueue(n);
                }
            }
        }

        return null;
    }
}
