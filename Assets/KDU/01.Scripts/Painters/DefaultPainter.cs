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

    [Header("외곽 벽 채우기 (맵 바운딩박스 + padding 안의 비-floor 셀 전부 wall)")]
    [SerializeField] private int outerWallPadding = 5;

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

        // 2. wall = floor 의 8방향 이웃 중 floor 가 아닌 셀
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

        // 3. 1줄짜리 벽 dissolve — 마주보는 양쪽이 floor 인 wall 을 floor 로 흡수.
        // RuleTile 이 1줄 벽을 인식하지 못하는 문제를 원천 차단. Cascade(흡수로 새로 생기는
        // sandwich) 까지 처리하기 위해 변동이 없을 때까지 반복.
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

        // 4. 외곽 + 가운데 빈 공간을 wall 로 채움 — 맵 바운딩박스 ± padding 안에서 floor 가 아닌 셀 전부.
        // 루프 가운데 hole 과 맵 바깥 padding 영역이 모두 솔리드 wall 이 되어 RuleTile 이 자연스럽게 연결됨.
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

        // 5. floor 페인트 — wall 셀 아래에도 깔아서 시각 연속성 확보.
        // 실제 통행 가능 여부는 GridSystem.IsWalkable 이 wall.HasTile 로 가린다 (시각/판정 분리).
        foreach (var c in floorSet)
            tilemaps.floor.SetTile(new Vector3Int(c.x, c.y, 0), floorTile);
        foreach (var c in wallSet)
            tilemaps.floor.SetTile(new Vector3Int(c.x, c.y, 0), floorTile);

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

        // 6. Feeling overlay — Painter 가 feeling 인자를 직접 읽음 (PHASE_3_5_PLAN.md Q1 결정 A).
        //    HazardFeeling 이면 방의 일부 셀에 hazard 타일 + MapGenerator.HazardCells 등록.
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
