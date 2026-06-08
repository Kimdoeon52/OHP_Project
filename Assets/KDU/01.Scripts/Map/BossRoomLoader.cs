using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 보스 층(5/10/15)의 미리 찍어둔 고정 맵 prefab 을 로드
/// 절차생성 파이프라인을 타지 않고 직접 그린 Tilemap 데이터를 MapGenerator 의 런타임 Tilemap 에 복사
/// </summary>
[CreateAssetMenu(fileName = "BossRoomLoader", menuName = "60s Dungeon/Map/Boss Room Loader")]
public class BossRoomLoader : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("이 prefab 이 적용될 depth (5/10/15)")]
        public int depth;
        [Tooltip("Root 아래 Floor/Wall/Hazard Tilemap 자식을 가진 prefab")]
        public GameObject prefab;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    /// <summary>
    /// depth 의 보스 prefab 을 로드해 target Tilemap 에 타일 복사. RoomData(타입 Boss) 1개 리스트 반환.
    /// exitCell 은 prefab 의 ExitAnchor 셀 좌표 (없으면 null).
    /// </summary>
    public List<RoomData> Load(int depth, TerrainTilemaps target, out Vector2Int? exitCell)
    {
        exitCell = null;
        if (target == null)
        {
            Debug.LogError("[BossRoomLoader] target TerrainTilemaps 가 null");
            return null;
        }

        Entry entry = null;
        foreach (var e in entries) { if (e != null && e.depth == depth) { entry = e; break; } }
        if (entry == null || entry.prefab == null)
        {
            Debug.LogError($"[BossRoomLoader] depth {depth} 에 대응하는 prefab 미할당");
            return null;
        }

        target.ClearAll();

        var tempRoot = Instantiate(entry.prefab);
        Vector2Int? safeCell = null;
        List<Vector2Int> safeCells = null;
        try
        {
            CopyChildTilemap(tempRoot.transform, "Floor", target.floor);
            CopyChildTilemap(tempRoot.transform, "Wall", target.wall);
            CopyChildTilemap(tempRoot.transform, "Hazard", target.hazard);
            CopyChildTilemap(tempRoot.transform, "AttackFloor", target.attackFloor);
            // SafeRoom 영역 = 'Safe' Tilemap 자식의 타일된 셀 전체 (다중 셀). 없으면 'SafeAnchor' 1칸으로 폴백.
            safeCells = ReadTilemapCells(tempRoot.transform, "Safe");
            safeCell = ReadAnchorCell(tempRoot.transform, "SafeAnchor");
            exitCell = ReadAnchorCell(tempRoot.transform, "ExitAnchor");
        }
        finally
        {
            if (Application.isPlaying) Destroy(tempRoot);
            else DestroyImmediate(tempRoot);
        }

        if (!exitCell.HasValue)
        {
            Debug.LogWarning("[BossRoomLoader] 'ExitAnchor' 자식 미발견 — 보스방 출구 미배치. " +
                             "보스 prefab Root 에 빈 GameObject 'ExitAnchor' 를 출구로 쓸 셀에 두세요.");
        }

        return BuildBossRoomData(target, safeCells, safeCell);
    }

    /// <summary>
    /// prefab 안의 anchor 자식 Transform 위치를 prefab 의 Floor Tilemap 기준 셀 좌표로 변환.
    /// SafeAnchor 가 없거나 Floor Tilemap 이 없으면 null.
    /// </summary>
    private static Vector2Int? ReadAnchorCell(Transform root, string anchorName)
    {
        var anchorTr = root.Find(anchorName);
        if (anchorTr == null) return null;

        var floorTr = root.Find("Floor");
        if (floorTr == null) return null;
        var floorTilemap = floorTr.GetComponent<Tilemap>();
        if (floorTilemap == null) return null;

        var cell = floorTilemap.WorldToCell(anchorTr.position);
        return new Vector2Int(cell.x, cell.y);
    }

    /// <summary>
    /// prefab 안의 childName Tilemap 자식에서 타일이 칠해진 셀 전부를 셀 좌표로 수집.
    /// Floor/Wall/Hazard 복사와 같은 좌표계(셀 위치 1:1) 이므로 그대로 RoomData 에 쓸 수 있다.
    /// 자식이나 Tilemap 이 없으면 빈 리스트.
    /// </summary>
    private static List<Vector2Int> ReadTilemapCells(Transform root, string childName)
    {
        var result = new List<Vector2Int>();
        var srcTr = root.Find(childName);
        if (srcTr == null) return result;
        var src = srcTr.GetComponent<Tilemap>();
        if (src == null) return result;

        foreach (var pos in src.cellBounds.allPositionsWithin)
        {
            if (src.HasTile(pos)) result.Add(new Vector2Int(pos.x, pos.y));
        }
        return result;
    }

    private static void CopyChildTilemap(Transform root, string childName, Tilemap dest)
    {
        if (dest == null) return;
        var srcTr = root.Find(childName);
        if (srcTr == null) return;
        var src = srcTr.GetComponent<Tilemap>();
        if (src == null) return;

        foreach (var pos in src.cellBounds.allPositionsWithin)
        {
            var tile = src.GetTile(pos);
            if (tile == null) continue;
            dest.SetTile(pos, tile);
            var mat = src.GetTransformMatrix(pos);
            if (mat != Matrix4x4.identity)
            {
                dest.SetTransformMatrix(pos, mat);
                dest.SetTileFlags(pos, src.GetTileFlags(pos));
            }
        }
    }

    private static List<RoomData> BuildBossRoomData(TerrainTilemaps tilemaps, List<Vector2Int> safeCells, Vector2Int? safeAnchorFallback)
    {
        var floorCells = new List<Vector2Int>();
        if (tilemaps.floor != null)
        {
            foreach (var pos in tilemaps.floor.cellBounds.allPositionsWithin)
            {
                if (tilemaps.floor.HasTile(pos)) floorCells.Add(new Vector2Int(pos.x, pos.y));
            }
        }
        var bounds = BuilderUtils.ComputeBounds(floorCells);

        var bossRoom = new RoomData
        {
            type = RoomType.Boss,
            sizeCategory = SizeCategory.Giant,
            bounds = bounds,
            floorCells = floorCells,
            distanceFromSafe = 1
        };

        var result = new List<RoomData> { bossRoom };

        // SafeRoom: 'Safe' 타일맵 영역(다중 셀) 우선, 없으면 'SafeAnchor' 1칸 폴백.
        List<Vector2Int> safeFloor = (safeCells != null && safeCells.Count > 0)
            ? safeCells
            : (safeAnchorFallback.HasValue ? new List<Vector2Int> { safeAnchorFallback.Value } : null);

        if (safeFloor != null && safeFloor.Count > 0)
        {
            var safeRoom = new RoomData
            {
                type = RoomType.Safe,
                sizeCategory = SizeCategory.Normal,
                bounds = BuilderUtils.ComputeBounds(safeFloor),
                floorCells = safeFloor,
                distanceFromSafe = 0,
            };
            result.Add(safeRoom);
        }
        else
        {
            Debug.LogWarning("[BossRoomLoader] 'Safe' 타일맵·'SafeAnchor' 모두 미발견 — 플레이어 스폰 지점이 없음. " +
                             "보스 prefab Root 에 'Safe' Tilemap 자식(SafeRoom 칸을 칠함) 또는 빈 GameObject 'SafeAnchor' 를 두세요.");
        }

        return result;
    }
}
