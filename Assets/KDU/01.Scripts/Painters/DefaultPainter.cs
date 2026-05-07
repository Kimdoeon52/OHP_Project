using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 가장 단순한 페인터 — 방/터널의 floorCells 합집합을 floor 타일로 깔고,
/// 그 8방향 이웃 중 floor 가 아닌 셀에 wall 타일을 깐다.
/// RuleTile 을 floorTile/wallTile 에 할당하면 자동으로 벽 모양이 연결됨.
/// </summary>
[CreateAssetMenu(fileName = "DefaultPainter", menuName = "60s Dungeon/Painters/Default Painter")]
public class DefaultPainter : ScriptableObject, IRegionPainter
{
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;

    public void Paint(
        List<RoomData> rooms,
        Tilemap floorTilemap,
        Tilemap wallTilemap,
        FloorFeeling feeling,
        DungeonRandom rng)
    {
        if (floorTilemap == null || wallTilemap == null)
        {
            Debug.LogError("DefaultPainter.Paint: Tilemap 참조 없음");
            return;
        }
        if (floorTile == null || wallTile == null)
        {
            Debug.LogError("DefaultPainter.Paint: floorTile/wallTile 미설정");
            return;
        }

        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        // 1. 모든 방/터널 floorCells 합집합
        var floorSet = new HashSet<Vector2Int>();
        foreach (var room in rooms)
        {
            if (room.floorCells == null) continue;
            foreach (var c in room.floorCells) floorSet.Add(c);
        }

        // 2. floor 페인트
        foreach (var c in floorSet)
            floorTilemap.SetTile(new Vector3Int(c.x, c.y, 0), floorTile);

        // 3. wall = floor 의 8방향 이웃 중 floor 가 아닌 셀
        var wallSet = new HashSet<Vector2Int>();
        foreach (var c in floorSet)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var n = new Vector2Int(c.x + dx, c.y + dy);
                    if (!floorSet.Contains(n)) wallSet.Add(n);
                }
            }
        }
        foreach (var c in wallSet)
            wallTilemap.SetTile(new Vector3Int(c.x, c.y, 0), wallTile);
    }
}
