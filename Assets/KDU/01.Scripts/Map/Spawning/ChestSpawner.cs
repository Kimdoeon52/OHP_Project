using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 생성 후 Special/Secret 방에 상자를 배치하는 스포너 (GDD 6.2.1 / 6.2.2 · 9.14).
/// MapGenerator 가 문/열쇠 배치 뒤에 Spawn() 을 호출한다 (EnemySpawner 와 같은 주입 패턴).
///
/// 방 종류별 풀(specialRoomPool / secretRoomPool)에서 ChestRoomContent 1개를 뽑아
/// 해당 방의 floor 셀에 상자를 배치한다. 반지 상자 방처럼 상자 열쇠가 필요한 구성은
/// 같은 층 Normal/Exit 방에 열쇠를 떨군다(GDD 9.15: 열쇠는 일반/exit 방에서 등장).
///
/// 한계(추후): Special/Secret 방의 "종류 5/4가지 중 1개" 중앙 결정 시스템이 아직 없어,
/// 현재는 이 스포너가 상자류 종류만 직접 선택한다. 몬스터 방/스크롤 방/인챈트 방 등이
/// 생기면 중앙 subtype 선택으로 옮기고 이 풀은 상자 구성만 제공하도록 정리할 것.
/// </summary>
public class ChestSpawner : MonoBehaviour
{
    [Header("프리팹")]
    [SerializeField] private GridChest chestPrefab;
    [Tooltip("상자 열쇠(반지 상자 등) 픽업 프리팹.")]
    [SerializeField] private KeyPickup keyPickupPrefab;

    [Header("방 종류별 상자 구성 풀 (방마다 균등 1/n 선택)")]
    [SerializeField] private List<ChestRoomContent> specialRoomPool = new();
    [SerializeField] private List<ChestRoomContent> secretRoomPool = new();

    [Header("방 종류 총 개수 N (균등 1/n 분모)")]
    [Tooltip("Special 방 전체 종류 수. GDD 6.2.1 = 5종(랜덤상자/몬스터/반지상자/골드상자/스크롤). " +
             "상자 풀에 없는 나머지(몬스터방/스크롤방 등)는 다른 스포너가 담당 → 여기선 그 몫만큼 배치 스킵.")]
    [SerializeField, Min(1)] private int specialRoomTypeCount = 5;
    [Tooltip("Secret 방 전체 종류 수. GDD 6.2.2 = 4종(포션/장비상자/골드/인챈트).")]
    [SerializeField, Min(1)] private int secretRoomTypeCount = 4;

    [Header("랜덤 상자 (Normal/Exit 방당 확률 — GDD 9.14)")]
    [Tooltip("일반/exit 방에 랜덤 등장하는 '랜덤 상자' ChestData.")]
    [SerializeField] private ChestData randomChest;
    [SerializeField, Range(0f, 1f)] private float randomChestChance = 0.2f;

    private readonly List<GameObject> _spawned = new();

    public void Spawn(List<RoomData> rooms, DungeonRandom rng)
    {
        Clear();
        if (chestPrefab == null || rooms == null || rng == null) return;

        foreach (var room in rooms)
        {
            List<ChestRoomContent> pool;
            int typeCount;
            switch (room.type)
            {
                case RoomType.Special: pool = specialRoomPool; typeCount = specialRoomTypeCount; break;
                case RoomType.Secret: pool = secretRoomPool; typeCount = secretRoomTypeCount; break;
                default: continue;
            }
            if (pool == null || pool.Count == 0) continue;

            // 균등 1/n: 분모는 전체 방 종류 수. 상자 풀 밖(비-상자 종류)이 뽑히면 배치 스킵.
            int n = Mathf.Max(pool.Count, typeCount);
            int pick = rng.Range(0, n);
            if (pick >= pool.Count) continue;

            PlaceContent(rooms, room, pool[pick], rng);
        }

        // 랜덤 상자 — Normal/Exit 방마다 별도 흐름으로 20% 굴림 (GDD 9.14).
        SpawnRandomChests(rooms, rng);
    }

    private void SpawnRandomChests(List<RoomData> rooms, DungeonRandom rng)
    {
        if (randomChest == null) return;

        foreach (var room in rooms)
        {
            if (room.type != RoomType.Normal && room.type != RoomType.Exit) continue;
            if (!rng.Chance(randomChestChance)) continue;

            var cells = SpawnCells.Candidates(room);
            if (cells.Count == 0) continue;
            SpawnChest(SpawnCells.PopRandom(cells, rng), randomChest, rng);
        }
    }

    public void Clear()
    {
        foreach (var go in _spawned)
        {
            if (go == null) continue;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        _spawned.Clear();
    }

    private void PlaceContent(List<RoomData> rooms, RoomData room, ChestRoomContent content, DungeonRandom rng)
    {
        if (content == null || content.Chest == null) return;

        var cells = SpawnCells.Candidates(room);
        if (cells.Count == 0) return;

        int count = Mathf.Min(rng.Range(content.CountMin, content.CountMax + 1), cells.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int cell = SpawnCells.PopRandom(cells, rng);
            SpawnChest(cell, content.Chest, rng);
        }

        // 상자 열쇠는 같은 층 Normal/Exit 방에 (GDD 9.15).
        if (content.SpawnChestKey && keyPickupPrefab != null)
            SpawnChestKeys(rooms, content.ChestKeyType, content.ChestKeyCount, rng);
    }

    private void SpawnChest(Vector2Int cell, ChestData data, DungeonRandom rng)
    {
        Vector3 pos = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);

        GridChest chest = Instantiate(chestPrefab, pos, Quaternion.identity);
        chest.Initialize(cell, data, rng);
        _spawned.Add(chest.gameObject);
    }

    private void SpawnChestKeys(List<RoomData> rooms, KeyType keyType, int keyCount, DungeonRandom rng)
    {
        var cells = SpawnCells.CandidatesIn(rooms, RoomType.Normal, RoomType.Exit);
        for (int k = 0; k < keyCount && cells.Count > 0; k++)
        {
            Vector2Int cell = SpawnCells.PopRandom(cells, rng);
            Vector3 pos = GridSystem.Instance != null
                ? GridSystem.Instance.GridToWorld(cell)
                : new Vector3(cell.x, cell.y, 0);
            KeyPickup pickup = Instantiate(keyPickupPrefab, pos, Quaternion.identity);
            pickup.Initialize(cell, keyType);
            _spawned.Add(pickup.gameObject);
        }
    }
}
