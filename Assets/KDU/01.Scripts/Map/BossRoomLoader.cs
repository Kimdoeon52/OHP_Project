using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


// 보스 층(5/10/15)의 미리 찍어둔 고정 맵 prefab으로 배치
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

   
    // depth 의 보스 prefab 을 로드해 target Tilemap 에 타일 복사
    public List<RoomData> Load(int depth, TerrainTilemaps target, out Vector2Int? exitCell, out List<GridDoor> doors)
    {
        exitCell = null;
        doors = null;
        if (target == null)
        {
            Debug.LogError("[BossRoomLoader] target TerrainTilemaps 가 null");
            return null;
        }

        Entry entry = null;
        foreach (var e in entries) { if (e != null && e.depth == depth) { entry = e; break; } }
        if (entry == null || entry.prefab == null)
        {
            Debug.LogError($"[BossRoomLoader] {depth}층 보스 prefab 미할당");
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
            safeCells = ReadTilemapCells(tempRoot.transform, "Safe");
            safeCell = ReadAnchorCell(tempRoot.transform, "SafeAnchor");
            exitCell = ReadAnchorCell(tempRoot.transform, "ExitAnchor");
            doors = DetachDoors(tempRoot.transform);
        }
        finally
        {
            if (Application.isPlaying) Destroy(tempRoot);
            else DestroyImmediate(tempRoot);
        }

        if (!exitCell.HasValue)
        {
            Debug.LogWarning("[BossRoomLoader] ExitAnchor 없음");
        }

        return BuildBossRoomData(target, safeCells, safeCell);
    }

    // prefab 안 GridDoor 들을 파괴 전에 씬으로 분리 — selfInitInScene 으로 자동 초기화
    private static List<GridDoor> DetachDoors(Transform root)
    {
        var found = root.GetComponentsInChildren<GridDoor>(true);
        if (found == null || found.Length == 0)
            return null;

        var result = new List<GridDoor>(found.Length);
        foreach (var door in found)
        {
            door.transform.SetParent(null, true);
            result.Add(door);
        }
        return result;
    }

    // prefab 안의 anchor 자식 Transform 위치를 prefab 의 Floor Tilemap 기준 셀 좌표로 변환
    private static Vector2Int? ReadAnchorCell(Transform root, string anchorName)
    {
        var anchorTr = root.Find(anchorName);
        if (anchorTr == null) 
            return null;

        var floorTr = root.Find("Floor");
        if (floorTr == null) 
            return null;
        var floorTilemap = floorTr.GetComponent<Tilemap>();
        if (floorTilemap == null) 
            return null;

        var cell = floorTilemap.WorldToCell(anchorTr.position);
        return new Vector2Int(cell.x, cell.y);
    }


    // prefab 안의 childName Tilemap 자식에서 타일이 칠해진 셀 전부를 셀 좌표로 수집
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

        // Safe Tilemap으로 세이프룸 생성
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
            Debug.LogWarning("[BossRoomLoader] 'Safe' 타일맵 없음");
        }

        return result;
    }
}
