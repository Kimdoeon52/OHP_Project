using UnityEngine;
using System.Collections.Generic;

public class GridSystem : Singleton<GridSystem>
{

    [Header("타일맵 레이어 (MapGenerator 와 동일한 참조)")]
    [SerializeField] private TerrainTilemaps tilemaps = new TerrainTilemaps();

    public TerrainTilemaps Tilemaps => tilemaps;

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 격자 좌표 -> 월드 좌표 (셀 중심점)
    /// </summary>
    public Vector3 GridToWorld(Vector2Int pos)
    {
        return tilemaps.floor.GetCellCenterWorld(new Vector3Int(pos.x, pos.y, 0));
    }

    /// <summary>
    /// 월드 좌표 -> 격자 좌표
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3Int cell = tilemaps.floor.WorldToCell(worldPos);
        return new Vector2Int(cell.x, cell.y);
    }

    private readonly HashSet<Vector2Int> _blockedCells = new HashSet<Vector2Int>();

    public void BlockCell(Vector2Int pos)
    {
        _blockedCells.Add(pos);
    }

    public void UnblockCell(Vector2Int pos)
    {
        _blockedCells.Remove(pos);
    }

    public bool IsCellBlocked(Vector2Int pos)
    {
        return _blockedCells.Contains(pos);
    }

    /// <summary>
    /// 해당 격자 셀이 이동 가능한지
    /// </summary>
    public bool IsWalkable(Vector2Int pos)
    {
        // 0. 임시 차단된 셀은 통행 불가
        if (IsCellBlocked(pos)) return false;

        var cell = new Vector3Int(pos.x, pos.y, 0);

        if (tilemaps.wall != null && tilemaps.wall.HasTile(cell)) return false;

        bool hasFloor = tilemaps.floor != null && tilemaps.floor.HasTile(cell);
        if (!hasFloor) return false;

        // 닫힌 문은 벽처럼 통행·시야 차단. 열린 문은 통과.
        if (_doors.TryGetValue(pos, out var door))
        {
            if (door == null) _doors.Remove(pos);
            else if (!door.IsOpen) return false;
        }

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
    /// 맵에 존재하는 모든 통행 가능 셀 반환 
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
    private readonly Dictionary<Vector2Int, EntityBase> _occupiedCells = new();
 
    public bool IsCellOccupied(Vector2Int pos) 
    {
        if (!_occupiedCells.TryGetValue(pos, out var entity))
            return false;
    
        // Unity의 null 체크 (오브젝트가 Destroy 되었을 때 null로 평가됨)
        if (entity == null)
        {
            _occupiedCells.Remove(pos); // 찌꺼기 데이터 청소
            return false;
        }
        return true;
    }
    public EntityBase GetEntityAt(Vector2Int pos)
    {
        if (!_occupiedCells.TryGetValue(pos, out var entity))
            return null;
        
        if (entity == null)
        {
            _occupiedCells.Remove(pos); // 찌꺼기 데이터 청소
            return null;
        }
        return entity;
    }
    public void RegisterOccupancy(Vector2Int pos, EntityBase entity)
    {
        _occupiedCells[pos] = entity;
    }
    public void UnRegisterOccupancy(Vector2Int pos)
    {
        _occupiedCells.Remove(pos);
    }

    // ───── 문 ─────
    private readonly Dictionary<Vector2Int, GridDoor> _doors = new();

    public void RegisterDoor(Vector2Int pos, GridDoor door) => _doors[pos] = door;

    public void UnregisterDoor(Vector2Int pos)
    {
        // 같은 셀에 새 문이 이미 등록됐을 수 있으니 그대로 둠 — 빈 경우만 제거.
        _doors.Remove(pos);
    }

    public GridDoor GetDoorAt(Vector2Int pos)
    {
        if (!_doors.TryGetValue(pos, out var door)) return null;
        if (door == null) { _doors.Remove(pos); return null; }
        return door;
    }

    /// <summary>
    /// 플레이어를 Safe/Special 방을 제외한 맵의 랜덤 빈 타일로 순간이동시킵니다.
    /// 적이 점유한 타일이나 현재 위치는 후보에서 제외됩니다.
    /// </summary>
    public void MoveRanPos()
    {
        var player = GameManager.Instance?.player?.GetComponent<EntityBase>();
        if (player == null || MapGenerator.Instance?.CurrentRooms == null) return;

        var validCells = new List<Vector2Int>();
        foreach (var room in MapGenerator.Instance.CurrentRooms)
        {
            if (room.type == RoomType.Safe || room.type == RoomType.Special)
                continue;

            foreach (var cell in room.floorCells)
            {
                if (cell == player.GridPosition) continue;
                if (!IsWalkable(cell)) continue;
                if (IsCellOccupied(cell)) continue;
                validCells.Add(cell);
            }
        }

        if (validCells.Count == 0) return;

        Vector2Int target = MapGenerator.Instance.Rng.Pick(validCells);
        player.SetGridPosition(target);
    }
}
