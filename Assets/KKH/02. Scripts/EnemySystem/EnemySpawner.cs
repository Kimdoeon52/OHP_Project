using System.Collections.Generic;
using UnityEngine;

namespace EnemySystem
{
    /// <summary>
    /// 한 층의 모든 적을 실제로 인스턴스화하는 메인 진입점.
    /// 각 방의 RoomType 에 맞는 RoomSpawnRule 을 골라 EnemySpawnEntry 들을 얻고, prefab 매핑을 통해 GameObject 생성.
    /// MapGenerator 의 Spawn 단계에서 호출되며, 매 Generate 호출마다 이전 적은 정리.
    /// </summary>
    public class EnemySpawner : MonoBehaviour, IEnemySpawner
    {
        [Header("스폰 데이터")]
        [SerializeField] private EnemySpawnTable spawnTable;
        [Tooltip("Boss 룸이 있을 때 출현할 보스 EnemyData (옵션 — 없으면 SpawnTable 최고 티어에서 폴백)")]
        [SerializeField] private EnemyData bossData;

        [Header("EnemyData → Prefab 매핑")]
        [SerializeField] private List<EnemyPrefabMapping> prefabMappings = new List<EnemyPrefabMapping>();

        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>
        /// IEnemySpawner — MapGenerator 가 한 층 생성 후 호출. rooms 의 각 방을 룰로 처리, prefab 생성.
        /// 같은 (runSeed, depth) 면 RoomSpawnRule 이 결정론적이므로 결과 동일.
        /// </summary>
        public void Spawn(List<RoomData> rooms, int depth, DungeonRandom rng)
        {
            ClearPreviousSpawns();

            if (rooms == null || rng == null) return;
            if (spawnTable == null)
            {
                Debug.LogWarning("EnemySpawner: spawnTable 미할당 — 스폰 스킵");
                return;
            }

            foreach (var room in rooms)
            {
                if (room.type == RoomType.Tunnel) continue; // 터널엔 스폰 안 함

                var rule = ResolveRule(room.type);
                if (rule == null) continue;

                foreach (var entry in rule.Generate(room, spawnTable, rng))
                {
                    InstantiateOne(entry);
                }
            }
        }

        /// <summary>방 타입 → 룰 매핑. Special/Secret 등 미구현 타입은 Normal 룰로 폴백.</summary>
        private RoomSpawnRule ResolveRule(RoomType type)
        {
            switch (type)
            {
                case RoomType.Safe: return new SafeRoomSpawnRule();
                case RoomType.Normal: return new NormalRoomSpawnRule();
                case RoomType.Exit: return new ExitRoomSpawnRule();
                case RoomType.Boss: return new BossRoomSpawnRule(bossData);
                case RoomType.Special:
                case RoomType.Secret:
                    return new NormalRoomSpawnRule(); // 임시 폴백 — Phase 5 에서 별도 룰
                default: return null;
            }
        }

        private void InstantiateOne(EnemySpawnEntry entry)
        {
            if (entry.enemyData == null) return;

            var prefab = FindPrefab(entry.enemyData);
            if (prefab == null)
            {
                Debug.LogWarning($"EnemySpawner: {entry.enemyData.name} 에 매핑된 prefab 없음 — 스킵");
                return;
            }

            Vector3 worldPos = GridSystem.Instance != null
                ? GridSystem.Instance.GridToWorld(entry.position)
                : new Vector3(entry.position.x, entry.position.y, 0);

            var go = Instantiate(prefab, worldPos, Quaternion.identity);
            go.name = $"{entry.enemyData.name}_{_spawned.Count}";
            _spawned.Add(go);
        }

        private GameObject FindPrefab(EnemyData data)
        {
            foreach (var m in prefabMappings)
                if (m.enemyData == data) return m.prefab;
            return null;
        }

        private void ClearPreviousSpawns()
        {
            foreach (var go in _spawned)
                if (go != null) Destroy(go);
            _spawned.Clear();
        }
    }
}
