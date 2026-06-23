using System.Collections.Generic;
using UnityEngine;

// 1/6/11층 Safe Room 테두리에 상품 진열대를 결정론 배치
public class ShopSpawner : MonoBehaviour
{
    [Header("프리팹 / 데이터")]
    [SerializeField] private ShopStall stallPrefab;
    [SerializeField] private ShopTableData table;
    [Tooltip("상인 장식 프롭 (선택)")]
    [SerializeField] private GameObject merchantPrefab;
    [Tooltip("중앙에서 방 안쪽으로 밀 칸 수")]
    [SerializeField] private int merchantInsetCells = 2;

    private static readonly Vector2Int[] Neighbors =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private readonly List<GameObject> _spawned = new();

    public void Spawn(List<RoomData> rooms, int depth, DungeonRandom rng)
    {
        Clear();
        if (stallPrefab == null || table == null || rooms == null || rng == null)
            return;

        int idx = table.FloorIndex(depth);
        if (idx < 0)
            return;

        var safe = rooms.Find(r => r.type == RoomType.Safe);
        if (safe == null || safe.floorCells == null || safe.floorCells.Count == 0)
            return;

        var border = BorderCells(safe);
        if (border.Count == 0)
        {
            Debug.LogWarning("[ShopSpawner] 테두리 셀 없음");
            return;
        }

        var products = BuildProducts(idx, rng);

        foreach (var p in products)
        {
            if (border.Count == 0)
            {
                Debug.LogWarning("[ShopSpawner] 진열 셀 부족 — 일부 상품 미배치");
                break;
            }
            Vector2Int cell = SpawnCells.PopRandom(border, rng);
            SpawnStall(cell, p);
        }

        SpawnMerchant(safe);
    }

    public void Clear()
    {
        foreach (var go in _spawned)
        {
            if (go == null)
                continue;
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }
        _spawned.Clear();
    }

    private struct Product
    {
        public ItemBase item;
        public int price;
        public int enhanceLevel;
    }

    // 등장확률 판정 → 개수 → 상품 결정
    private List<Product> BuildProducts(int idx, DungeonRandom rng)
    {
        var list = new List<Product>();
        if (table.Entries == null)
            return list;

        foreach (var entry in table.Entries)
        {
            if (entry == null || !rng.Chance(entry.Chance(idx)))
                continue;

            int count = entry.Count(idx, rng);
            for (int i = 0; i < count; i++)
            {
                ItemBase product = entry.PickProduct(rng);
                if (product == null)
                    continue;
                list.Add(new Product
                {
                    item = product,
                    price = entry.Price(idx),
                    enhanceLevel = entry.enhanceLevel,
                });
            }
        }
        return list;
    }

    // 벽에 인접한 외곽 셀 (문/문인접 제외)
    private static List<Vector2Int> BorderCells(RoomData room)
    {
        var floor = new HashSet<Vector2Int>(room.floorCells);

        var avoid = new HashSet<Vector2Int>();
        if (room.doorCells != null)
        {
            foreach (var d in room.doorCells)
            {
                avoid.Add(d);
                foreach (var n in Neighbors)
                    avoid.Add(d + n);
            }
        }

        var hazard = MapGenerator.Instance != null ? MapGenerator.Instance.HazardCells : null;
        var grid = GridSystem.Instance;
        var result = new List<Vector2Int>();

        foreach (var c in room.floorCells)
        {
            if (avoid.Contains(c))
                continue;
            if (hazard != null && hazard.Contains(c))
                continue;
            if (grid != null && !grid.IsWalkable(c))
                continue;
            if (SpawnCells.IsOccupied(c))
                continue;

            bool onBorder = false;
            foreach (var n in Neighbors)
            {
                if (!floor.Contains(c + n))
                {
                    onBorder = true;
                    break;
                }
            }
            if (onBorder)
                result.Add(c);
        }
        return result;
    }

    private void SpawnStall(Vector2Int cell, Product p)
    {
        Vector3 pos = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(cell) : new Vector3(cell.x, cell.y, 0);

        ShopStall stall = Instantiate(stallPrefab, pos, Quaternion.identity);
        stall.Initialize(cell, p.item, p.price, p.enhanceLevel);
        _spawned.Add(stall.gameObject);
    }

    private void SpawnMerchant(RoomData safe)
    {
        if (merchantPrefab == null)
            return;

        Vector2Int cell = MerchantCell(safe);
        Vector3 pos = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(cell) : new Vector3(cell.x, cell.y, 0);
        _spawned.Add(Instantiate(merchantPrefab, pos, Quaternion.identity));
    }

    // 중앙에서 문 반대쪽(방 안쪽)으로 살짝 민 셀
    private Vector2Int MerchantCell(RoomData safe)
    {
        Vector2 center = Centroid(safe.floorCells);
        Vector2 target = center + InwardDir(safe, center) * merchantInsetCells;
        return NearestFloorCell(safe.floorCells, target, center);
    }

    private static Vector2 Centroid(List<Vector2Int> cells)
    {
        Vector2 sum = Vector2.zero;
        foreach (var c in cells)
            sum += c;
        return sum / cells.Count;
    }

    // 문 평균 → 중앙 방향 (문 없으면 0)
    private static Vector2 InwardDir(RoomData safe, Vector2 center)
    {
        if (safe.doorCells == null || safe.doorCells.Count == 0)
            return Vector2.zero;

        Vector2 doorAvg = Vector2.zero;
        foreach (var d in safe.doorCells)
            doorAvg += d;
        doorAvg /= safe.doorCells.Count;

        Vector2 dir = center - doorAvg;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.zero;
    }

    // target에 가장 가까운 통행 가능·미점유 floor 셀 (없으면 fallback 셀)
    private static Vector2Int NearestFloorCell(List<Vector2Int> cells, Vector2 target, Vector2 fallback)
    {
        var grid = GridSystem.Instance;
        Vector2Int best = NearestCell(cells, fallback);
        float bestDist = float.MaxValue;

        foreach (var c in cells)
        {
            if (grid != null && !grid.IsWalkable(c))
                continue;
            if (SpawnCells.IsOccupied(c))
                continue;

            float d = ((Vector2)c - target).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }
        return best;
    }

    private static Vector2Int NearestCell(List<Vector2Int> cells, Vector2 target)
    {
        Vector2Int best = cells[0];
        float bestDist = float.MaxValue;
        foreach (var c in cells)
        {
            float d = ((Vector2)c - target).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }
        return best;
    }
}
