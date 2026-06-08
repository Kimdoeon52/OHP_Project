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
    [SerializeField] private float activeDuration = 5f;               // 타일이 유지되는 시간 (초)
    [SerializeField] private float cooldownDuration = 2f;             // 타일 제거 후 다음 패턴까지 쉬는 시간 (초)

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
        SpecialPatternLoop().Forget();
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

    private async UniTaskVoid SpecialPatternLoop()
    {
        var token = cts.Token;
        try
        {
            while (isRunning && cts != null && !cts.IsCancellationRequested)
            {
                // 1. 임의로 패턴 1 또는 패턴 2 선택
                int patternType = Random.Range(1, 3); // 1 또는 2
                
                // 2. 타일 그리기 및 플레이어 밀치기
                if (patternType == 1)
                {
                    ExecuteEdgePattern();
                }
                else
                {
                    ExecuteCenterPattern();
                }

                // 3. 5초 경과 대기 (activeDuration)
                await UniTask.Delay((int)(activeDuration * 1000), cancellationToken: token);

                // 4. 타일 제거 (다시 걸을 수 있음)
                ClearPaintedTiles();

                // 5. 쿨다운 대기 후 반복 (cooldownDuration)
                await UniTask.Delay((int)(cooldownDuration * 1000), cancellationToken: token);
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

    private void ExecuteCenterPattern()
    {
        if (boss == null || !boss.HasMapBounds || GridSystem.Instance == null || obstacleTile == null) return;

        RectInt bounds = boss.MapBounds;
        var wallTilemap = GridSystem.Instance.Tilemaps.wall;
        if (wallTilemap == null) return;

        ClearPaintedTiles();

        Vector2Int center = new Vector2Int(bounds.xMin + bounds.width / 2, bounds.yMin + bounds.height / 2);

        // 중앙 5x5 영역 (반경 2)
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                Vector2Int cell = center + new Vector2Int(dx, dy);
                
                if (GridSystem.Instance.IsInBounds(cell))
                {
                    var hasFloor = GridSystem.Instance.Tilemaps.floor != null && GridSystem.Instance.Tilemaps.floor.HasTile(new Vector3Int(cell.x, cell.y, 0));
                    var hasHazard = GridSystem.Instance.Tilemaps.hazard != null && GridSystem.Instance.Tilemaps.hazard.HasTile(new Vector3Int(cell.x, cell.y, 0));

                    if (hasFloor || hasHazard)
                    {
                        wallTilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), obstacleTile);
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

            // 만약 현재 탐색 좌표가 걸어갈 수 있고, 몬스터가 없고, 새로 칠한 벽 타일 위치가 아니라면
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
