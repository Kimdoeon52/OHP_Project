using System.Collections.Generic;
using UnityEngine;

namespace EnemySystem
{
    /// <summary>
    /// 층별로 적을 실제 격자 위치에 배치하고 인스턴스화하는 스파우너.
    /// FloorSpawnConfig를 활용하여 층별(구역별) 설정의 유지보수성을 극대화합니다.
    /// </summary>
    public class EnemySpawner : MonoBehaviour, IEnemySpawner
    {
        [System.Serializable]
        public struct FloorConfigEntry
        {
            [Tooltip("시작 층 (포함)")]
            public int minDepth;
            [Tooltip("끝 층 (포함)")]
            public int maxDepth;
            [Tooltip("해당 층 범위에서 적용할 스폰 설정")]
            public FloorSpawnConfig config;
        }

        [Header("층별 스폰 설정 범위 목록")]
        [SerializeField] private List<FloorConfigEntry> floorConfigs = new List<FloorConfigEntry>();

        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>
        /// 스폰 수량 보정치 (FloorFeeling 등에서 조절할 수 있습니다)
        /// </summary>
        public int SpawnCountModifier { get; set; } = 0;

        /// <summary>
        /// IEnemySpawner 구현 - MapGenerator에서 맵 빌드 및 타일 배치가 완료된 후 호출됩니다.
        /// </summary>
        public void Spawn(List<RoomData> rooms, int depth, DungeonRandom rng)
        {
            ClearPreviousSpawns();

            if (rooms == null || rng == null) return;

            FloorSpawnConfig currentConfig = GetConfigForDepth(depth);
            if (currentConfig == null)
            {
                Debug.LogWarning($"EnemySpawner: {depth}층에 대한 FloorSpawnConfig가 설정되지 않았습니다.");
                return;
            }

            if (currentConfig.spawnTable == null)
            {
                Debug.LogWarning($"EnemySpawner: FloorSpawnConfig '{currentConfig.name}'에 스폰 테이블이 지정되지 않았습니다.");
                return;
            }

            foreach (var room in rooms)
            {
                // 통로(Tunnel) 구조체엔 적 스폰 안 함
                if (room.type == RoomType.Tunnel) continue;

                // 방 타입별 스폰 규칙 설정 (Safe, Normal, Exit, Boss 등)
                RoomSpawnRule rule = ResolveRule(room.type, currentConfig.bossData);
                if (rule == null) continue;

                // 룰에 따른 몬스터 목록 및 그리드 스폰 위치 반환 후 생성
                foreach (var entry in rule.Generate(room, currentConfig.spawnTable, rng, SpawnCountModifier))
                {
                    InstantiateOne(entry, currentConfig);
                }
            }

            // 다음 생성을 위해 보정치 초기화
            SpawnCountModifier = 0;
        }

        /// <summary>
        /// 주어진 깊이(depth)에 알맞은 FloorSpawnConfig를 탐색합니다.
        /// </summary>
        private FloorSpawnConfig GetConfigForDepth(int depth)
        {
            foreach (var entry in floorConfigs)
            {
                if (depth >= entry.minDepth && depth <= entry.maxDepth)
                {
                    return entry.config;
                }
            }
            return null;
        }

        /// <summary>
        /// 방 타입에 따른 스폰 규칙 클래스를 매핑합니다.
        /// </summary>
        private RoomSpawnRule ResolveRule(RoomType type, EnemyData bossData)
        {
            switch (type)
            {
                case RoomType.Safe: return new SafeRoomSpawnRule();
                case RoomType.Normal: return new NormalRoomSpawnRule();
                case RoomType.Exit: return new ExitRoomSpawnRule();
                case RoomType.Boss: return new BossRoomSpawnRule(bossData);
                case RoomType.Special:
                case RoomType.Secret:
                    return new NormalRoomSpawnRule(); // 임시 폴백
                default: return null;
            }
        }

        /// <summary>
        /// 몬스터를 인스턴스화하고 격자 위치에 등록합니다.
        /// </summary>
        private void InstantiateOne(EnemySpawnEntry entry, FloorSpawnConfig config)
        {
            if (entry.enemyData == null) return;

            GameObject prefab = config.GetPrefab(entry.enemyData);
            if (prefab == null)
            {
                Debug.LogWarning($"EnemySpawner: '{config.name}' 설정에서 {entry.enemyData.displayName} 에 해당하는 프리팹을 찾을 수 없습니다.");
                return;
            }

            Vector3 worldPos = GridSystem.Instance != null
                ? GridSystem.Instance.GridToWorld(entry.position)
                : new Vector3(entry.position.x, entry.position.y, 0);

            GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
            go.name = $"{entry.enemyData.id}_{_spawned.Count}";

            // EntityBase 기반 몬스터일 경우 격자 내부 위치 초기화
            var entity = go.GetComponent<EntityBase>();
            if (entity != null)
            {
                entity.Initialize(entry.position);
            }

            _spawned.Add(go);
        }

        /// <summary>
        /// 이전 층의 생성되었던 몬스터들을 일괄 소멸합니다.
        /// </summary>
        private void ClearPreviousSpawns()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Destroy(go);
            }
            _spawned.Clear();
        }
    }
}
