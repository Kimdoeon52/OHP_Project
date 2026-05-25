using System.Collections.Generic;
using UnityEngine;
using EnemySystem;

/// <summary>
/// 에디터 전용 스폰 테스트 헬퍼.
/// 맵 생성기 없이 RoomSpawnRule이 올바른 마릿수와 좌표를 내놓는지 빠르게 검증 가능.
///
/// 사용 방법:
///   1. 인스펙터에서 spawnTable, prefabMappings, testRoomType 등 설정
///   2. 컴포넌트 우클릭 → "스폰 테스트 실행" 선택
///   3. 씬 뷰에 임시 몬스터 생성 확인
///   4. 재실행 시 이전 임시 오브젝트 자동 삭제 후 재생성
///
/// 주의: 에디터 전용. 빌드에 포함되어도 런타임 동작엔 영향 없음.
/// </summary>
public class EnemySpawnTester : MonoBehaviour
{
    [Header("테스트용 리소스 의존성 설정")]
    [Tooltip("몬스터 확률 풀을 읽어올 SpawnTable SO")]
    public EnemySpawnTable spawnTable;
    
    [Tooltip("(EnemyData, Prefab) 매핑 목록. EnemySpawner.floorConfigs 대신 직접 입력.")]
    public List<EnemyPrefabMapping> prefabMappings;
    
    [Tooltip("보스 방 테스트 시 사용할 보스 SO 데이터")]
    public EnemyData testBossData;

    [Header("가상 테스트 룸 구성")]
    [Tooltip("테스트할 방 타입")]
    public RoomType testRoomType = RoomType.Normal;
    
    [Tooltip("Safe Room으로부터의 거리. 높을수록 어려운 티어 적용.")]
    public int distanceFromSafe = 2;
    
    [Tooltip("가상 방의 격자 크기 (너비 x 높이)")]
    public Vector2Int roomSize = new Vector2Int(5, 5);
    
    [Tooltip("가상 방의 격자 기준 좌표 (좌하단)")]
    public Vector2Int roomPosition = new Vector2Int(0, 0);

    /// <summary> 시드 기반 결정론적 난수 생성기 </summary>
    private DungeonRandom rng = new DungeonRandom();
    
    /// <summary> 이전 테스트에서 소환된 임시 몬스터 오브젝트 목록. 재실행 시 DestroyImmediate. </summary>
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    /// <summary>
    /// 가상 방 데이터를 구성하고 RoomSpawnRule.Generate()를 실행해 씬에 임시 몬스터를 소환.
    /// 에디터 컨텍스트 메뉴에서 실행 (우클릭 → "스폰 테스트 실행").
    /// </summary>
    [ContextMenu("스폰 테스트 실행 (가상 방 생성 및 적 스폰)")]
    public void RunSpawnTest()
    {
        // 1. 이전 테스트 임시 오브젝트 정리
        foreach (var obj in spawnedEnemies)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        spawnedEnemies.Clear();

        // 2. 가상 방 데이터 조립
        RoomData mockRoom = new RoomData
        {
            type             = testRoomType,
            sizeCategory     = SizeCategory.Normal,
            distanceFromSafe = distanceFromSafe,
            bounds           = new RectInt(roomPosition.x, roomPosition.y, roomSize.x, roomSize.y)
        };

        // 방 크기만큼 바닥 셀 추가
        for (int x = 0; x < roomSize.x; x++)
        {
            for (int y = 0; y < roomSize.y; y++)
            {
                mockRoom.floorCells.Add(new Vector2Int(roomPosition.x + x, roomPosition.y + y));
            }
        }

        // 문 타일 등록 (가장자리 중앙, 플레이어 진입 흉내)
        mockRoom.doorCells.Add(new Vector2Int(roomPosition.x + roomSize.x / 2, roomPosition.y));

        // 3. 방 타입에 맞는 룰 선택
        RoomSpawnRule rule = null;
        switch (testRoomType)
        {
            case RoomType.Safe:   rule = new SafeRoomSpawnRule();              break;
            case RoomType.Normal: rule = new NormalRoomSpawnRule();            break;
            case RoomType.Exit:   rule = new ExitRoomSpawnRule();              break;
            case RoomType.Boss:   rule = new BossRoomSpawnRule(testBossData);  break;
            default:              rule = new NormalRoomSpawnRule();            break;
        }

        // 4. 난수 시드 초기화 후 스폰 목록 계산
        rng.Init(Random.Range(0, 100000));
        var spawns = rule.Generate(mockRoom, spawnTable, rng);

        // 5. 프리팹 매핑 거쳐 실제 소환
        int count = 0;
        foreach (var spawn in spawns)
        {
            GameObject prefabToSpawn = GetPrefabFor(spawn.enemyData);
            if (prefabToSpawn != null)
            {
                // GridSystem 있으면 격자→월드 변환, 없으면 1:1 단위 배치
                Vector3 worldPos = new Vector3(spawn.position.x, spawn.position.y, 0);
                if (GridSystem.Instance != null)
                {
                    worldPos = GridSystem.Instance.GridToWorld(spawn.position);
                }

                GameObject inst = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);
                inst.name = $"{spawn.enemyData.id}_{count++}";
                
                spawnedEnemies.Add(inst); // 다음 실행 시 정리를 위해 캐싱
            }
            else
            {
                Debug.LogWarning($"Tester: {spawn.enemyData.name}에 매핑된 프리팹 없음.");
            }
        }

        Debug.Log($"[SpawnTest] {testRoomType} 방 (거리 {distanceFromSafe}) → {count}마리 스폰 완료.");
    }

    /// <summary>
    /// 내부 prefabMappings에서 특정 EnemyData에 대응하는 프리팹 반환.
    /// 없으면 null.
    /// </summary>
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
