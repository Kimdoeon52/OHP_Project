using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 한 층의 모든 Tilemap 레이어
/// </summary>
[System.Serializable]
public class TerrainTilemaps
{
    public Tilemap floor;
    public Tilemap wall;
    public Tilemap hazard;
    public Tilemap water;
    public Tilemap chasm;

    public TerrainType Get(Vector2Int cell)
    {
        var v = new Vector3Int(cell.x, cell.y, 0);
        if (chasm != null && chasm.HasTile(v)) return TerrainType.Chasm;
        if (hazard != null && hazard.HasTile(v)) return TerrainType.Hazard;
        if (water != null && water.HasTile(v)) return TerrainType.Water;
        if (floor != null && floor.HasTile(v)) return TerrainType.Floor;
        return TerrainType.None;
    }

    /// <summary>모든 레이어의 타일을 비운다 (맵 재생성 직전 호출).</summary>
    public void ClearAll()
    {
        floor?.ClearAllTiles();
        wall?.ClearAllTiles();
        hazard?.ClearAllTiles();
        water?.ClearAllTiles();
        chasm?.ClearAllTiles();
    }
}
