using UnityEngine;

public class GridSystem : MonoBehaviour
{
    public static GridSystem Instance { get; private set; }

    [Header("타일맵 레이어 (MapGenerator 와 동일한 참조)")]
    [SerializeField] private TerrainTilemaps tilemaps = new TerrainTilemaps();

    public TerrainTilemaps Tilemaps => tilemaps;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("GridSystem이 씬에 두 개 이상 있습니다.");
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 격자 좌표 → 월드 좌표 (셀 중심점)
    /// </summary>
    public Vector3 GridToWorld(Vector2Int pos)
    {
        return tilemaps.floor.GetCellCenterWorld(new Vector3Int(pos.x, pos.y, 0));
    }

    /// <summary>
    /// 월드 좌표 → 격자 좌표
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3Int cell = tilemaps.floor.WorldToCell(worldPos);
        return new Vector2Int(cell.x, cell.y);
    }

    /// <summary>
    /// 해당 격자 셀이 이동 가능한지 (바닥 타일 있고, 벽/낭떨어지 타일 없음)
    /// </summary>
    public bool IsWalkable(Vector2Int pos)
    {
        var cell = new Vector3Int(pos.x, pos.y, 0);

        if (tilemaps.chasm != null && tilemaps.chasm.HasTile(cell)) return false;

        bool hasFloor = tilemaps.floor != null && tilemaps.floor.HasTile(cell);
        if (!hasFloor) return false;

        return true;
    }

    /// <summary>
    /// 한 셀의 지형 타입 — 포션/AI/시야 등이 분기 조건으로 사용.
    /// </summary>
    public TerrainType GetTerrain(Vector2Int pos)
    {
        return tilemaps.Get(pos);
    }

    /// <summary>
    /// 맵 범위 안인지 (바닥 또는 벽 타일이 존재하는 셀)
    /// </summary>
    public bool IsInBounds(Vector2Int pos)
    {
        var cell = new Vector3Int(pos.x, pos.y, 0);
        if (tilemaps.floor != null && tilemaps.floor.HasTile(cell)) return true;
        return tilemaps.wall != null && tilemaps.wall.HasTile(cell);
    }

    /// <summary>
    /// 맵에 존재하는 모든 통행 가능 셀 반환 (VisionSystem 초기화 등에 사용)
    /// </summary>
    public System.Collections.Generic.IEnumerable<Vector2Int> GetAllWalkableCells()
    {
        if (tilemaps.floor == null) yield break;
        foreach (var pos in tilemaps.floor.cellBounds.allPositionsWithin)
        {
            if (tilemaps.floor.HasTile(pos))
            {
                var grid = new Vector2Int(pos.x, pos.y);
                if (IsWalkable(grid))
                    yield return grid;
            }
        }
    }

    /// <summary>
    /// 맵에 존재하는 모든 셀 반환 (바닥 + 벽 포함, 안개 초기화 등에 사용)
    /// </summary>
    public System.Collections.Generic.IEnumerable<Vector2Int> GetAllCells()
    {
        var visited = new System.Collections.Generic.HashSet<Vector2Int>();

        if (tilemaps.floor != null)
        {
            foreach (var pos in tilemaps.floor.cellBounds.allPositionsWithin)
            {
                if (tilemaps.floor.HasTile(pos))
                    visited.Add(new Vector2Int(pos.x, pos.y));
            }
        }

        if (tilemaps.wall != null)
        {
            foreach (var pos in tilemaps.wall.cellBounds.allPositionsWithin)
            {
                if (tilemaps.wall.HasTile(pos))
                    visited.Add(new Vector2Int(pos.x, pos.y));
            }
        }

        return visited;
    }
}
