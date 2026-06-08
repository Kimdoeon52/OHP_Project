using System.Collections.Generic;
using UnityEngine;

// 맵 생성 후 Special/Secret 방에 상자를 배치하는 스포너
public class ChestSpawner : MonoBehaviour
{
    [Header("프리팹")]
    [SerializeField] private GridChest chestPrefab;
    [Tooltip("상자 열쇠(반지 상자 등) 픽업 프리팹.")]
    [SerializeField] private KeyPickup keyPickupPrefab;

    [Header("방 종류별 상자 구성 (해당 종류일 때만 배치 · 나머지 종류는 다른 스포너)")]
    [Tooltip("Special: 랜덤 상자 방 (랜덤상자 2~3).")]
    [SerializeField] private ChestRoomContent specialRandomChest;
    [Tooltip("Special: 반지 상자 방 (반지상자 2 + 상자 열쇠).")]
    [SerializeField] private ChestRoomContent specialRingChest;
    [Tooltip("Special: 골드 상자 방 (골드상자 3).")]
    [SerializeField] private ChestRoomContent specialGoldChest;
    [Tooltip("Secret: 장비 상자 방 (장비상자 1).")]
    [SerializeField] private ChestRoomContent secretEquipChest;

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
            // 룸타입 받아와서 상자 배치
            ChestRoomContent content = room.type switch
            {
                RoomType.Special => SpecialContent(room.specialKind),
                RoomType.Secret => SecretContent(room.secretKind),
                _ => null,
            };
            if (content != null) PlaceContent(rooms, room, content, rng);
        }

        // 랜덤 상자 — Normal/Exit 방마다 별도 흐름으로 20% 굴림
        SpawnRandomChests(rooms, rng);
    }

    // 상자 나오는 스페셜 방 종류
    private ChestRoomContent SpecialContent(SpecialRoomKind kind) => kind switch
    {
        SpecialRoomKind.RandomChest => specialRandomChest,
        SpecialRoomKind.RingChest => specialRingChest,
        SpecialRoomKind.GoldChest => specialGoldChest,
        _ => null, // 나머지는 null 처리
    };

    // 상자 나오는 시크릿 방 종류
    private ChestRoomContent SecretContent(SecretRoomKind kind) => kind switch
    {
        SecretRoomKind.EquipChest => secretEquipChest,
        _ => null, // 나머지는 null 처리
    };

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

        // 상자 열쇠는 같은 층 Normal/Exit 방에 배치
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
