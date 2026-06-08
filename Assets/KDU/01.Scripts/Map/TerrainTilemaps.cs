using UnityEngine;
using UnityEngine.Tilemaps;

// 한 층의 모든 Tilemap 레이어
[System.Serializable]
public class TerrainTilemaps
{
    public Tilemap floor;
    public Tilemap wall;
    public Tilemap hazard;
    public Tilemap attackFloor;

    public TerrainType Get(Vector2Int cell)
    {
        var v = new Vector3Int(cell.x, cell.y, 0);
        if (hazard != null && hazard.HasTile(v)) 
            return TerrainType.Hazard;

        if (floor != null && floor.HasTile(v)) 
            return TerrainType.Floor;

        return TerrainType.None;
    }

    public void ClearAll()
    {
        if (floor != null)
            floor.ClearAllTiles();

        if (wall != null) 
            wall.ClearAllTiles();

        if (hazard != null)
            hazard.ClearAllTiles();

        if (attackFloor != null) 
            attackFloor.ClearAllTiles();
    }
}
