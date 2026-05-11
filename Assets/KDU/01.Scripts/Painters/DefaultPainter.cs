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
        TerrainTilemaps tilemaps,
        FloorFeeling feeling,
        DungeonRandom rng)
    {
        if (tilemaps == null)
        {
            Debug.LogError("DefaultPainter.Paint: TerrainTilemaps 미할당");
            return;
        }
        if (tilemaps.floor == null || tilemaps.wall == null)
        {
            Debug.LogError("DefaultPainter.Paint: floor/wall Tilemap 미할당");
            return;
        }
        if (floorTile == null || wallTile == null)
        {
            Debug.LogError("DefaultPainter.Paint: floorTile/wallTile 미설정");
            return;
        }

        tilemaps.ClearAll();

        // 1. 모든 방/터널 floorCells 합집합
        var floorSet = new HashSet<Vector2Int>();
        foreach (var room in rooms)
        {
            if (room.floorCells == null) continue;
            foreach (var c in room.floorCells) floorSet.Add(c);
        }

        // 2. floor 페인트
        foreach (var c in floorSet)
            tilemaps.floor.SetTile(new Vector3Int(c.x, c.y, 0), floorTile);

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

        // Pass 1: 모든 wall 타일 먼저 SetTile — RuleTile 이웃 갱신 다 끝낸 후
        // Pass 2 에서 transform 을 잠가야 refresh 에 의해 reset 되지 않음.
        foreach (var c in wallSet)
            tilemaps.wall.SetTile(new Vector3Int(c.x, c.y, 0), wallTile);

        // Pass 2: 우측 측면 벽만 X-flip + LockTransform.
        // 표준 RuleTile MirrorX 는 측면 벽 패턴이 X 대칭이라 발동하지 않으므로 강제 미러.
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
    }
}
