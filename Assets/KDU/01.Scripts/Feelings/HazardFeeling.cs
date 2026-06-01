using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "HazardFeeling", menuName = "60s Dungeon/Feelings/Hazard")]
public class HazardFeeling : FloorFeeling
{
    public override string Id => "Hazard";

    [Header("영역별 위험 바닥 타일")]
    [Tooltip("Area 1 (하수구) — 오염수")]
    [SerializeField] private TileBase tileArea1;
    [Tooltip("Area 2 (던전) — 함정 바닥")]
    [SerializeField] private TileBase tileArea2;
    [Tooltip("Area 3 (지옥) — 용암")]
    [SerializeField] private TileBase tileArea3;

    [Header("방 면적 대비 위험 바닥 비율")]
    [SerializeField, Range(0f, 1f)] private float coverage = 0.2f;

    public TileBase GetTile(int area)
    {
        switch (area)
        {
            case 1: return tileArea1;
            case 2: return tileArea2;
            default: return tileArea3;
        }
    }

    public void PaintHazardOverlay(
        List<RoomData> rooms,
        TerrainTilemaps tilemaps,
        int depth,
        DungeonRandom rng,
        HashSet<Vector2Int> hazardCells)
    {
        if (tilemaps == null || tilemaps.hazard == null)
        {
            Debug.LogWarning("[HazardFeeling] tilemaps.hazard 가 null — MapGenerator 인스펙터에서 Tilemaps.hazard 슬롯 확인");
            return;
        }
        if (rooms == null || rng == null || hazardCells == null)
        {
            Debug.LogWarning("[HazardFeeling] rooms/rng/hazardCells null");
            return;
        }

        int area = MapGenerator.AreaFromDepth(depth);
        TileBase tile = GetTile(area);
        if (tile == null)
        {
            Debug.LogWarning($"[HazardFeeling] Area {area} 의 tile 이 null — HazardFeeling SO 의 tileArea{area} 슬롯 확인");
            return;
        }

        int totalPainted = 0;

        var roomCandidates = new HashSet<Vector2Int>();
        var seedList = new List<Vector2Int>();

        foreach (var room in rooms)
        {
            // Safe/Tunnel + 보상 방(Special/Secret)은 Hazard 제외 — 보상 동선에 페널티 없음
            if (room.type == RoomType.Safe || room.type == RoomType.Tunnel
                || room.type == RoomType.Special || room.type == RoomType.Secret) continue;
            if (room.floorCells == null || room.floorCells.Count == 0) continue;

            roomCandidates.Clear();
            foreach (var c in room.floorCells)
            {
                if (!IsNearDoor(c, room)) roomCandidates.Add(c);
            }
            if (roomCandidates.Count < 4) continue;

            seedList.Clear();
            seedList.AddRange(roomCandidates);
            for (int i = seedList.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (seedList[i], seedList[j]) = (seedList[j], seedList[i]);
            }

            int targetCells = Mathf.RoundToInt(roomCandidates.Count * coverage);
            int painted = 0;

            foreach (var seed in seedList)
            {
                if (painted >= targetCells) break;

                // 룰타일이 1칸짜리 다리/모서리에서 깨지지 않게, 짝수 격자에 스냅해서 2-grid 정렬 보장
                var s = new Vector2Int(seed.x - (seed.x & 1), seed.y - (seed.y & 1));

                int w = PickEvenSize(rng);
                int h = PickEvenSize(rng);

                if (!CanPaintRect(s, w, h, roomCandidates, hazardCells))
                {
                    if (!CanPaintRect(s, 2, 2, roomCandidates, hazardCells)) continue;
                    w = 2; h = 2;
                }

                for (int dx = 0; dx < w; dx++)
                {
                    for (int dy = 0; dy < h; dy++)
                    {
                        var c = new Vector2Int(s.x + dx, s.y + dy);
                        tilemaps.hazard.SetTile(new Vector3Int(c.x, c.y, 0), tile);
                        hazardCells.Add(c);
                    }
                }
                int blockSize = w * h;
                painted += blockSize;
                totalPainted += blockSize;
            }
        }

        Debug.Log($"[HazardFeeling] area={area} coverage={coverage} 페인트 셀 {totalPainted}개");
    }

    private static int PickEvenSize(DungeonRandom rng)
    {
        float r = rng.Value();
        if (r < 0.6f) return 2;
        if (r < 0.9f) return 4;
        return 6;
    }

    private static bool CanPaintRect(
        Vector2Int seed, int w, int h,
        HashSet<Vector2Int> allowed,
        HashSet<Vector2Int> alreadyPainted)
    {
        for (int dx = 0; dx < w; dx++)
        {
            for (int dy = 0; dy < h; dy++)
            {
                var c = new Vector2Int(seed.x + dx, seed.y + dy);
                if (!allowed.Contains(c)) return false;
                if (alreadyPainted.Contains(c)) return false;
            }
        }
        return true;
    }

    private static bool IsNearDoor(Vector2Int cell, RoomData room)
    {
        if (room.doorCells == null) return false;
        foreach (var d in room.doorCells)
        {
            if (Mathf.Abs(cell.x - d.x) + Mathf.Abs(cell.y - d.y) <= 1) return true;
        }
        return false;
    }
}
