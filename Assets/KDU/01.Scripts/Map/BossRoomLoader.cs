using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 보스 층(5/10/15)의 미리 찍어둔 고정 맵 prefab 을 로드.
/// 절차생성 파이프라인을 타지 않고 직접 그린 Tilemap 데이터를 MapGenerator 의 런타임 Tilemap 에 복사.
///
/// Prefab 규격 (사용자가 만들 prefab 구조):
///   Root (GameObject)
///   ├── Floor   (Tilemap 컴포넌트 — 바닥 타일 페인트)
///   ├── Wall    (Tilemap — 벽 타일 페인트)
///   └── Hazard  (Tilemap — 옵션, 위험 바닥 페인트)
///
/// 자식 이름은 정확히 "Floor"/"Wall"/"Hazard". 없으면 해당 레이어 스킵.
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
    /// </summary>
    public List<RoomData> Load(int depth, TerrainTilemaps target)
    {
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

        // 임시 인스턴스화 → 자식 Tilemap 의 타일 데이터 복사 → destroy.
        var tempRoot = Instantiate(entry.prefab);
        try
        {
            CopyChildTilemap(tempRoot.transform, "Floor", target.floor);
            CopyChildTilemap(tempRoot.transform, "Wall", target.wall);
            CopyChildTilemap(tempRoot.transform, "Hazard", target.hazard);
        }
        finally
        {
            if (Application.isPlaying) Destroy(tempRoot);
            else DestroyImmediate(tempRoot);
        }

        return BuildBossRoomData(target);
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

    private static List<RoomData> BuildBossRoomData(TerrainTilemaps tilemaps)
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
            distanceFromSafe = 0
        };
        return new List<RoomData> { bossRoom };
    }
}
