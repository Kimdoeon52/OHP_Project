using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 한 층의 모든 Tilemap 레이어 묶음. 인스펙터에서 한 번에 드래그-드롭으로 할당.
/// 비주얼+지형타입 단일 소스 — Get(cell) 이 어느 레이어에 타일이 있는지 보고 TerrainType 결정.
/// Phase 3 시점 사용 레이어: floor / wall / hazard. water / grass / chasm 은 후속 Phase 에서 활성화 (null 허용).
/// </summary>
[System.Serializable]
public class TerrainTilemaps
{
    public Tilemap floor;
    public Tilemap wall;
    public Tilemap hazard;
    public Tilemap water;
    public Tilemap grass;
    public Tilemap chasm;

    /// <summary>
    /// 한 셀의 지형 타입을 위에서부터 (영향 큰 순서로) 검사해 결정.
    /// Chasm > Hazard > Water > Grass > Floor > None.
    /// </summary>
    public TerrainType Get(Vector2Int cell)
    {
        var v = new Vector3Int(cell.x, cell.y, 0);
        if (chasm != null && chasm.HasTile(v)) return TerrainType.Chasm;
        if (hazard != null && hazard.HasTile(v)) return TerrainType.Hazard;
        if (water != null && water.HasTile(v)) return TerrainType.Water;
        if (grass != null && grass.HasTile(v)) return TerrainType.Grass;
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
        grass?.ClearAllTiles();
        chasm?.ClearAllTiles();
    }
}
