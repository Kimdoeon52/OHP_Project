using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 가장 단순한 페인터 — 방/터널의 floorCells 합집합을 floor 타일로 깔고,
/// 그 8방향 이웃 중 floor 가 아닌 셀에 wall 타일을 깐다
/// </summary>
[CreateAssetMenu(fileName = "DefaultPainter", menuName = "60s Dungeon/Painters/Default Painter")]
public class DefaultPainter : ScriptableObject, IRegionPainter
{
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;

    [SerializeField] private int outerWallPadding = 5;

    public void Paint(
        List<RoomData> rooms,
        TerrainTilemaps tilemaps,
        FloorFeeling feeling,
        DungeonRandom rng)
    {
        if (tilemaps == null)
        {
            Debug.LogError("[DefaultPainter] TerrainTilemaps 미할당");
            return;
        }
        if (tilemaps.floor == null || tilemaps.wall == null)
        {
            Debug.LogError("[DefaultPainter] floor/wall Tilemap 미할당");
            return;
        }
        if (floorTile == null || wallTile == null)
        {
            Debug.LogError("[DefaultPainter] floorTile/wallTile 미설정");
            return;
        }

        tilemaps.ClearAll();

        var floorSet = new HashSet<Vector2Int>();
        foreach (var room in rooms)
        {
            if (room.floorCells == null) continue;
            foreach (var c in room.floorCells) floorSet.Add(c);
        }

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

        var toDissolve = new List<Vector2Int>();
        while (true)
        {
            toDissolve.Clear();
            foreach (var w in wallSet)
            {
                bool hSandwich =
                    floorSet.Contains(new Vector2Int(w.x - 1, w.y)) &&
                    floorSet.Contains(new Vector2Int(w.x + 1, w.y));
                bool vSandwich =
                    floorSet.Contains(new Vector2Int(w.x, w.y - 1)) &&
                    floorSet.Contains(new Vector2Int(w.x, w.y + 1));
                if (hSandwich || vSandwich) toDissolve.Add(w);
            }
            if (toDissolve.Count == 0) break;
            foreach (var w in toDissolve)
            {
                wallSet.Remove(w);
                floorSet.Add(w);
            }
        }

        if (floorSet.Count > 0)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (var c in floorSet)
            {
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
            }
            minX -= outerWallPadding; minY -= outerWallPadding;
            maxX += outerWallPadding; maxY += outerWallPadding;
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (!floorSet.Contains(c)) wallSet.Add(c);
                }
            }
        }

        foreach (var c in floorSet)
            tilemaps.floor.SetTile(new Vector3Int(c.x, c.y, 0), floorTile);
        foreach (var c in wallSet)
            tilemaps.floor.SetTile(new Vector3Int(c.x, c.y, 0), floorTile);

        foreach (var c in wallSet)
            tilemaps.wall.SetTile(new Vector3Int(c.x, c.y, 0), wallTile);

        var flipMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, 1f, 1f));
        foreach (var c in wallSet)
        {
            bool floorW = floorSet.Contains(c + Vector2Int.left);
            bool floorE = floorSet.Contains(c + Vector2Int.right);
            bool floorN = floorSet.Contains(c + Vector2Int.up);
            bool floorS = floorSet.Contains(c + Vector2Int.down);
            bool isPureRightSideWall = floorW && !floorE && !floorN && !floorS;
            if (!isPureRightSideWall) continue;

            var pos = new Vector3Int(c.x, c.y, 0);
            tilemaps.wall.SetTransformMatrix(pos, flipMatrix);
            tilemaps.wall.SetTileFlags(pos, TileFlags.LockTransform);
        }

        if (feeling is HazardFeeling hazard && MapGenerator.Instance != null)
        {
            hazard.PaintHazardOverlay(
                rooms,
                tilemaps,
                MapGenerator.Instance.CurrentDepth,
                rng,
                MapGenerator.Instance.HazardCells);
        }
    }
}
