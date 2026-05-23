using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Hazard Feeling — 위험 바닥 (영역별 리스킨: Area1 오염수 / Area2 함정바닥 / Area3 용암).
/// 해당 바닥 진입 시 이동 시간 2배 + 데미지 (GDD 6.3 / 8.5.1 / 9.16). 통과는 가능 — 통과 불가 아님.
/// 부유 포션·공허 발걸음 마법이면 페널티 무시 (상태 조회 — 이동 처리부에서 분기).
/// </summary>
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

    /// <summary>
    /// 방의 floorCells 중 일부를 hazard 타일로 덮고 hazardCells 집합에 등록.
    /// Safe Room / Tunnel / 입구 인접 셀(맨해튼 ≤1) 제외.
    /// 결정론: rng 호출 순서 보존 — 방 리스트 순서대로 처리.
    /// </summary>
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
            if (room.type == RoomType.Safe || room.type == RoomType.Tunnel) continue;
            if (room.floorCells == null || room.floorCells.Count == 0) continue;

            // 1. 후보 셀 (입구 인접 제외)
            roomCandidates.Clear();
            foreach (var c in room.floorCells)
            {
                if (!IsNearDoor(c, room)) roomCandidates.Add(c);
            }
            if (roomCandidates.Count < 4) continue; // 2x2 도 못 깔면 스킵

            // 2. 시드 순서 Fisher-Yates 셔플 — 결정론.
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

                // 3. 직사각 사이즈 결정. 항상 rng 2회 호출 — 검증 실패 시에도 호출 패턴 유지(결정론).
                int w = PickEvenSize(rng);
                int h = PickEvenSize(rng);

                // 4. (seed 좌하단 기준) wxh 직사각이 후보에 모두 포함되고 미페인트 상태인지 검증.
                if (!CanPaintRect(seed, w, h, roomCandidates, hazardCells))
                {
                    // 폴백: 최소 2x2 만 시도
                    if (!CanPaintRect(seed, 2, 2, roomCandidates, hazardCells)) continue;
                    w = 2; h = 2;
                }

                // 5. 페인트
                for (int dx = 0; dx < w; dx++)
                {
                    for (int dy = 0; dy < h; dy++)
                    {
                        var c = new Vector2Int(seed.x + dx, seed.y + dy);
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

    /// <summary>2/4/6 중 하나 가중치 선택 (60/30/10). 짝수 보장 → 어떤 조합이든 2x2 이상.</summary>
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
