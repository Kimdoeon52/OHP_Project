using UnityEngine;
using System.Collections.Generic;

public class GridSystem : Singleton<GridSystem>
{

    [Header("타일맵 레이어")]
    [SerializeField] private TerrainTilemaps tilemaps = new TerrainTilemaps();

    public TerrainTilemaps Tilemaps => tilemaps;

    protected override void Awake()
    {
        base.Awake();
    }

    // 격자 좌표 -> 월드 좌표
    public Vector3 GridToWorld(Vector2Int pos)
    {
        return tilemaps.floor.GetCellCenterWorld(new Vector3Int(pos.x, pos.y, 0));
    }


    // 월드 좌표 -> 격자 좌표
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

    // 해당 격자 셀이 이동 가능한지 체크
    public bool IsWalkable(Vector2Int pos)
    {
        if (IsCellBlocked(pos)) 
            return false;

        var cell = new Vector3Int(pos.x, pos.y, 0);

        if (tilemaps.wall != null && tilemaps.wall.HasTile(cell)) 
            return false;

        bool hasFloor = tilemaps.floor != null && tilemaps.floor.HasTile(cell);
        if (!hasFloor) 
            return false;

        if (_doors.TryGetValue(pos, out var door))
        {
            if (door == null)
                _doors.Remove(pos);
            else if (!door.IsOpen) 
                return false;
        }

        return true;
    }

    public TerrainType GetTerrain(Vector2Int pos)
    {
        return tilemaps.Get(pos);
    }

    // 맵 범위 안인지 체크
    public bool IsInBounds(Vector2Int pos)
    {
        var cell = new Vector3Int(pos.x, pos.y, 0);
        if (tilemaps.floor != null && tilemaps.floor.HasTile(cell)) 
            return true;

        return tilemaps.wall != null && tilemaps.wall.HasTile(cell);
    }

    // 맵에 존재하는 모든 통행 가능 셀 반환 
    public System.Collections.Generic.IEnumerable<Vector2Int> GetAllWalkableCells()
    {
        if (tilemaps.floor == null) 
            yield break;

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


    // 타일맵에 존재하는 모든 셀 반환
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
    private readonly Dictionary<Vector2Int, IGridEntity> _occupiedCells = new();
 
    public bool IsCellOccupied(Vector2Int pos) 
    {
        if (!_occupiedCells.TryGetValue(pos, out var entity))
            return false;
    
        if (entity == null || (entity is Object unityObj && unityObj == null))
        {
            _occupiedCells.Remove(pos); // Garbage data 청소
            return false;
        }
        return true;
    }
    public IGridEntity GetEntityAt(Vector2Int pos)
    {
        if (!_occupiedCells.TryGetValue(pos, out var entity))
            return null;
        
        if (entity == null || (entity is Object unityObj && unityObj == null))
        {
            _occupiedCells.Remove(pos); // Garbage data 청소
            return null;
        }
        return entity;
    }
    public void RegisterOccupancy(Vector2Int pos, IGridEntity entity)
    {
        _occupiedCells[pos] = entity;
    }
    public void UnRegisterOccupancy(Vector2Int pos)
    {
        _occupiedCells.Remove(pos);
    }

    // 문 
    private readonly Dictionary<Vector2Int, GridDoor> _doors = new();

    public void RegisterDoor(Vector2Int pos, GridDoor door) => _doors[pos] = door;

    public void UnregisterDoor(Vector2Int pos)
    {
        _doors.Remove(pos);
    }

    public GridDoor GetDoorAt(Vector2Int pos)
    {
        if (!_doors.TryGetValue(pos, out var door)) 
            return null;

        if (door == null) 
        { 
            _doors.Remove(pos); 
            return null;
        }
        return door;
    }


    // 플레이어를 Safe/Special 방을 제외한 맵의 랜덤 빈 타일로 순간이동
    // 적이 존재하는 타일이나 현재 위치는 후보에서 제외
    public void MoveRanPos()
    {
        var player = GameManager.Instance?.player?.GetComponent<EntityBase>();
        if (player == null || MapGenerator.Instance?.CurrentRooms == null)
            return;

        var validCells = new List<Vector2Int>();
        foreach (var room in MapGenerator.Instance.CurrentRooms)
        {
            if (room.type == RoomType.Safe || room.type == RoomType.Special)
                continue;

            foreach (var cell in room.floorCells)
            {
                if (cell == player.GridPosition) 
                    continue;

                if (!IsWalkable(cell)) 
                    continue;

                if (IsCellOccupied(cell))
                    continue;

                validCells.Add(cell);
            }
        }

        if (validCells.Count == 0) 
            return;

        Vector2Int target = MapGenerator.Instance.Rng.Pick(validCells);
        player.SetGridPosition(target);
    }
}
