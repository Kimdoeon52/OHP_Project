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

    [Header("스페셜 패턴 타일 설정")]
    [SerializeField] private UnityEngine.Tilemaps.TileBase obstacleTile; // 새로 그릴 벽 타일 에셋

    private BossBase boss;
    private CancellationTokenSource cts;
    private List<Vector2Int> walkableCellsCache;
    private bool isRunning = false;
    private List<Vector2Int> paintedCells = new List<Vector2Int>();

    private void Awake()
    {
        boss = GetComponent<BossBase>();
    }

    public void StartSpecialPattern()
    {
        if (isRunning) return;
        isRunning = true;

        cts = new CancellationTokenSource();

        // 1. 패턴1: 가장자리에 타일을 즉시 그리고 플레이어를 밀침 (상시 유지)
        ExecuteEdgePattern();

        // 2. 패턴2: 남아있는 바닥(이동 가능 셀)을 캐싱하여 크리스탈 비 준비
        CacheWalkableCells();

        // 3. 패턴2: 크리스탈 비 상시 루프 시작
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
        if (boss == null || !boss.HasMapBounds || GridSystem.Instance == null || obstacleTile == null) return;

        RectInt bounds = boss.MapBounds;
        var wallTilemap = GridSystem.Instance.Tilemaps.wall;
        if (wallTilemap == null) return;

        ClearPaintedTiles();

        // 맵 안전성 체크: 최소 크기가 되지 않으면 크기를 동적 조절
        int xEdge = Mathf.Max(1, Mathf.Min(3, bounds.width / 2 - 1));
        int yEdge = Mathf.Max(1, Mathf.Min(3, bounds.height / 2 - 1));

        // 가장자리 xEdge, yEdge 칸만큼 새로운 타일을 그려 좁힘
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                // 가장자리 영역 조건
                if (x < bounds.xMin + xEdge || x >= bounds.xMax - xEdge || 
                    y < bounds.yMin + yEdge || y >= bounds.yMax - yEdge)
                {
                    // 바닥 또는 함정 타일이 있어 원래 다닐 수 있는 길일 때만 벽을 그림
                    var hasFloor = GridSystem.Instance.Tilemaps.floor != null && GridSystem.Instance.Tilemaps.floor.HasTile(new Vector3Int(x, y, 0));
                    var hasHazard = GridSystem.Instance.Tilemaps.hazard != null && GridSystem.Instance.Tilemaps.hazard.HasTile(new Vector3Int(x, y, 0));

                    if (hasFloor || hasHazard)
                    {
                        wallTilemap.SetTile(new Vector3Int(x, y, 0), obstacleTile);
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

    private void ClearPaintedTiles()
    {
        if (GridSystem.Instance == null) return;
        var wallTilemap = GridSystem.Instance.Tilemaps.wall;
        if (wallTilemap == null) return;

        foreach (var cell in paintedCells)
        {
            wallTilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), null);
        }

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

    private Vector2Int? FindKnockbackTargetNear(Vector2Int playerPos)
    {
        if (GridSystem.Instance == null) return null;

        var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        ShuffleDirections(directions);

        foreach (var dir in directions)
        {
            var candidate = playerPos + dir;
            // 칠해진 벽 영역이 아니고, 원래 이동 가능한 범위인지 검사
            if (GridSystem.Instance.IsInBounds(candidate) && 
                GridSystem.Instance.IsWalkable(candidate) &&
                !paintedCells.Contains(candidate))
            {
                if (TurnManager.Instance != null && TurnManager.Instance.GetEntityAt(candidate) == null)
                {
                    return candidate;
                }
            }
        }

        var diagonalDirs = new[] {
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };
        ShuffleDirections(diagonalDirs);

        foreach (var dir in diagonalDirs)
        {
            var candidate = playerPos + dir;
            if (GridSystem.Instance.IsInBounds(candidate) && 
                GridSystem.Instance.IsWalkable(candidate) &&
                !paintedCells.Contains(candidate))
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
