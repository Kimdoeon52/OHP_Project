using System.Collections.Generic;
using UnityEngine;
using EnemySystem;

/// <summary>
/// RoomSpawnRule 로직을 테스트하기 위한 헬퍼 스크립트.
/// 에디터 Inspector에서 ContextMenu "스폰 테스트 실행"을 통해 작동.
/// </summary>
public class EnemySpawnTester : MonoBehaviour
{
    [Header("의존성")]
    public EnemySpawnTable spawnTable;
    public List<EnemyPrefabMapping> prefabMappings;
    public EnemyData testBossData;

    [Header("가상 방 설정")]
    public RoomType testRoomType = RoomType.Normal;
    public int distanceFromSafe = 2;
    public Vector2Int roomSize = new Vector2Int(5, 5);
    public Vector2Int roomPosition = new Vector2Int(0, 0);

    private DungeonRandom rng = new DungeonRandom();
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    [ContextMenu("스폰 테스트 실행 (가상 방 생성 및 적 스폰)")]
    public void RunSpawnTest()
    {
        // 1. 기존 생성된 적 지우기
        foreach (var obj in spawnedEnemies)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        spawnedEnemies.Clear();

        // 2. 가상 RoomData 생성
        RoomData mockRoom = new RoomData
        {
            type = testRoomType,
            sizeCategory = SizeCategory.Normal,
            distanceFromSafe = distanceFromSafe,
            bounds = new RectInt(roomPosition.x, roomPosition.y, roomSize.x, roomSize.y)
        };

        // 바닥 셀 채우기
        for (int x = 0; x < roomSize.x; x++)
        {
            for (int y = 0; y < roomSize.y; y++)
            {
                mockRoom.floorCells.Add(new Vector2Int(roomPosition.x + x, roomPosition.y + y));
            }
        }

        // 가상 입구 (가장자리에 하나 배치)
        mockRoom.doorCells.Add(new Vector2Int(roomPosition.x + roomSize.x / 2, roomPosition.y));

        // 3. RoomSpawnRule 결정
        RoomSpawnRule rule = null;
        switch (testRoomType)
        {
            case RoomType.Safe: rule = new SafeRoomSpawnRule(); break;
            case RoomType.Normal: rule = new NormalRoomSpawnRule(); break;
            case RoomType.Exit: rule = new ExitRoomSpawnRule(); break;
            case RoomType.Boss: rule = new BossRoomSpawnRule(testBossData); break;
            default: rule = new NormalRoomSpawnRule(); break;
        }

        // 4. 스폰 실행
        rng.Init(Random.Range(0, 100000));
        var spawns = rule.Generate(mockRoom, spawnTable, rng);

        // 5. 실제 Prefab 인스턴스화
        int count = 0;
        foreach (var spawn in spawns)
        {
            GameObject prefabToSpawn = GetPrefabFor(spawn.enemyData);
            if (prefabToSpawn != null)
            {
                // GridSystem이 씬에 있다면 GridToWorld 사용, 아니면 대략적인 월드 좌표
                Vector3 worldPos = new Vector3(spawn.position.x, spawn.position.y, 0);
                if (GridSystem.Instance != null)
                {
                    worldPos = GridSystem.Instance.GridToWorld(spawn.position);
                }

                GameObject inst = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);
                inst.name = $"{spawn.enemyData.id}_{count++}";
                spawnedEnemies.Add(inst);
            }
            else
            {
                Debug.LogWarning($"Tester: {spawn.enemyData.name}에 매핑된 프리팹이 없습니다!");
            }
        }

        Debug.Log($"[SpawnTest] {testRoomType} 방(거리 {distanceFromSafe})에 총 {count}마리 스폰 완료.");
    }

    private GameObject GetPrefabFor(EnemyData data)
    {
        if (data == null) return null;
        foreach (var mapping in prefabMappings)
        {
            if (mapping.enemyData == data) return mapping.prefab;
        }
        return null;
    }
}
