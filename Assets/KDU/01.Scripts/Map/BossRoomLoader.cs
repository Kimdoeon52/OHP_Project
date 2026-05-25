using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 보스 층(5/10/15)의 미리 찍어둔 고정 맵 prefab 을 로드.
/// 절차생성 파이프라인을 타지 않고 직접 그린 Tilemap 데이터를 MapGenerator 의 런타임 Tilemap 에 복사.
///
/// Prefab 규격 (사용자가 만들 prefab 구조):
///   Root (GameObject — Grid 컴포넌트 권장, 자식 Tilemap 들의 부모)
///   ├── Floor       (Tilemap 컴포넌트 — 바닥 타일 페인트)
///   ├── Wall        (Tilemap — 벽 타일 페인트)
///   ├── Hazard      (Tilemap — 옵션, 위험 바닥 페인트)
///   └── SafeAnchor  (빈 Transform — 플레이어 스폰 셀 1개 위치)
///
/// 자식 이름은 정확히 "Floor"/"Wall"/"Hazard"/"SafeAnchor". 없으면 해당 항목 스킵.
/// SafeAnchor 가 있으면 그 셀 1개를 floorCells 로 하는 RoomData(type=Safe) 가 추가로 반환됨
/// (출품 빌드 결정 — MAP_GENERATION.md 1.2 참조).
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

        // 임시 인스턴스화 → 자식 Tilemap 의 타일 데이터 + SafeAnchor 위치 추출 → destroy.
        // Tilemap WorldToCell 은 prefab 의 Floor Tilemap 을 기준으로 계산 — 같은 Grid 안의 위치라
        // CopyChildTilemap 이 셀 좌표를 그대로 옮기는 것과 좌표계가 정확히 일치한다.
        var tempRoot = Instantiate(entry.prefab);
        Vector2Int? safeCell = null;
        try
        {
            CopyChildTilemap(tempRoot.transform, "Floor", target.floor);
            CopyChildTilemap(tempRoot.transform, "Wall", target.wall);
            CopyChildTilemap(tempRoot.transform, "Hazard", target.hazard);
            safeCell = ReadAnchorCell(tempRoot.transform, "SafeAnchor");
        }
        finally
        {
            if (Application.isPlaying) Destroy(tempRoot);
            else DestroyImmediate(tempRoot);
        }

        return BuildBossRoomData(target, safeCell);
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

    private static List<RoomData> BuildBossRoomData(TerrainTilemaps tilemaps, Vector2Int? safeCell)
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

        // 출품 빌드: SafeAnchor 1셀 = Safe RoomData 1셀 (MAP_GENERATION.md 1.2).
        // 영역 분리(flood fill) 로의 마이그레이션은 문 시스템 도입 시 — BACKLOG.md "문 시스템" 항목.
        if (safeCell.HasValue)
        {
            var s = safeCell.Value;
            var safeRoom = new RoomData
            {
                type = RoomType.Safe,
                sizeCategory = SizeCategory.Normal,
                bounds = new RectInt(s.x, s.y, 1, 1),
                floorCells = new List<Vector2Int> { s },
                distanceFromSafe = 0,
            };
            result.Add(safeRoom);
        }
        else
        {
            Debug.LogWarning("[BossRoomLoader] 'SafeAnchor' 자식 미발견 — 플레이어 스폰 지점이 없음. " +
                             "보스 prefab Root 에 빈 GameObject 'SafeAnchor' 를 보스방 안의 안전한 한 셀에 두세요.");
        }

        return result;
    }
}
