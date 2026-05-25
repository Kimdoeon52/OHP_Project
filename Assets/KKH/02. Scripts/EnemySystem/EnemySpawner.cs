using System.Collections.Generic;
using UnityEngine;

namespace EnemySystem
{
    /// <summary>
    /// 던전 맵 생성 흐름(MapGenerator) 완료 후 각 방에 몬스터를 소환하는 총괄 스포너.
    ///
    /// 처리 흐름:
    ///   MapGenerator.Build 완료
    ///   → IEnemySpawner.Spawn(rooms, depth, rng) 호출
    ///   → 이전 층 몬스터 전부 Destroy
    ///   → depth에 맞는 FloorSpawnConfig 검색
    ///   → 방마다 RoomSpawnRule.Generate() 실행 → EnemySpawnEntry 목록 획득
    ///   → InstantiateOne으로 프리팹 소환 + EntityBase 초기화
    ///
    /// 인스펙터 설정 방법:
    ///   floorConfigs 리스트에 (minDepth, maxDepth, FloorSpawnConfig) 쌍 등록.
    ///   층 범위가 겹치면 첫 번째 일치 항목이 선택됨.
    /// </summary>
    public class EnemySpawner : MonoBehaviour, IEnemySpawner
    {
        [Header("층 범위 구간별 스폰 설정 목록")]
        [Tooltip("층별 스폰 규칙 정보들을 담은 FloorSpawnConfig SO 목록임.")]
        [SerializeField] private List<FloorSpawnConfig> floorConfigs = new List<FloorSpawnConfig>();

        /// <summary> 현재 층에 생성된 몬스터 오브젝트 관리 목록. 층 이동 시 전부 Destroy. </summary>
        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>
        /// 외부(FloorFeeling 등)에서 마릿수를 임시 조정할 수 있는 보정값.
        /// Spawn() 완료 후 자동으로 0으로 리셋됨.
        /// 예: TrapsFeeling에서 SpawnCountModifier = -1 하면 각 방 마릿수 1 감소.
        /// </summary>
        public int SpawnCountModifier { get; set; } = 0;

        private void Start()
        {
            // MapGenerator 없는 단순 테스트 씬에서 자동으로 타일맵 기반 스폰 시도
            SpawnFromTilemap();
        }

        /// <summary>
        /// GridSystem의 모든 Walkable 셀을 하나의 가상 Normal 방으로 묶어 적을 스폰하는 유틸리티.
        /// 에디터 ContextMenu에서도 실행 가능 (우클릭 → "타일맵 기반 적 자동 스폰").
        /// </summary>
        [ContextMenu("타일맵 기반 적 자동 스폰")]
        public void SpawnFromTilemap()
        {
            if (GridSystem.Instance == null)
            {
                Debug.LogWarning("EnemySpawner: GridSystem 없음 → 스폰 불가.");
                return;
            }

            // 1. Walkable 셀 전체 수집
            var walkableCells = new List<Vector2Int>(GridSystem.Instance.GetAllWalkableCells());
            if (walkableCells.Count == 0)
            {
                Debug.LogWarning("EnemySpawner: Walkable 타일 없음 → 스폰 불가.");
                return;
            }

            // 2. 수집된 셀로 가상 Normal 방 구성 (거리 2 = 2티어 난이도)
            RoomData mockRoom = new RoomData
            {
                type         = RoomType.Normal,
                sizeCategory = SizeCategory.Normal,
                distanceFromSafe = 2,
            };
            mockRoom.floorCells.AddRange(walkableCells);

            // 방 경계 계산
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var cell in walkableCells)
            {
                if (cell.x < minX) minX = cell.x;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.y > maxY) maxY = cell.y;
            }
            mockRoom.bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);

            // 3. 1층 규칙으로 스폰
            DungeonRandom rng = new DungeonRandom();
            rng.Init(Random.Range(0, 100000));
            Spawn(new List<RoomData> { mockRoom }, 1, rng);
        }

        /// <summary>
        /// IEnemySpawner 인터페이스 구현.
        /// MapGenerator가 맵 조각 조립 + 타일 페인팅 완료 후 최종 호출.
        ///
        /// 순서:
        ///   1. 이전 층 몬스터 전부 Destroy
        ///   2. depth로 FloorSpawnConfig 검색
        ///   3. 각 방에 RoomSpawnRule.Generate() 실행
        ///   4. 결과(EnemySpawnEntry) 목록을 InstantiateOne으로 소환
        ///   5. SpawnCountModifier 리셋
        /// </summary>
        /// <param name="rooms">던전 방 목록</param>
        /// <param name="depth">현재 층 깊이</param>
        /// <param name="rng">시드 기반 결정론적 난수 생성기</param>
        public void Spawn(List<RoomData> rooms, int depth, DungeonRandom rng)
        {
            ClearPreviousSpawns();

            if (rooms == null || rng == null) return;

            // 현재 층의 FloorSpawnConfig 검색
            FloorSpawnConfig currentConfig = GetConfigForDepth(depth);
            if (currentConfig == null)
            {
                Debug.LogWarning($"EnemySpawner: {depth}층에 FloorSpawnConfig 미설정.");
                return;
            }

            if (currentConfig.spawnTable == null)
            {
                Debug.LogWarning($"EnemySpawner: '{currentConfig.name}' SpawnTable 미할당.");
                return;
            }

            // 방별로 스폰 규칙 적용
            foreach (var room in rooms)
            {
                if (room.type == RoomType.Tunnel) continue; // 복도는 스킵

                // 방 타입에 맞는 RoomSpawnRule 결정
                RoomSpawnRule rule = ResolveRule(room.type);
                if (rule == null) continue;

                // RoomSpawnRule이 계산한 (EnemyData, 좌표) 튜플을 하나씩 소환
                foreach (var entry in rule.Generate(room, currentConfig.spawnTable, rng, SpawnCountModifier))
                {
                    InstantiateOne(entry, currentConfig);
                }
            }

            // 다음 층 스폰을 위해 보정값 초기화
            SpawnCountModifier = 0;
        }

        /// <summary>
        /// 지정한 depth가 적용 범위에 맞는 FloorSpawnConfig SO를 검색하여 반환함.
        /// 일치 항목 없으면 null 반환함.
        /// </summary>
        private FloorSpawnConfig GetConfigForDepth(int depth)
        {
            foreach (var config in floorConfigs)
            {
                if (config != null && config.IsDepthMatching(depth))
                {
                    return config;
                }
            }
            return null;
        }

        /// <summary>
        /// 방 타입에 맞는 RoomSpawnRule 인스턴스 반환.
        /// Special / Secret은 임시로 NormalRoomSpawnRule 폴백 사용.
        /// </summary>
        private RoomSpawnRule ResolveRule(RoomType type)
        {
            switch (type)
            {
                case RoomType.Safe:    return new SafeRoomSpawnRule();
                case RoomType.Normal:  return new NormalRoomSpawnRule();
                case RoomType.Exit:    return new ExitRoomSpawnRule();
                case RoomType.Boss:    return new BossRoomSpawnRule();
                case RoomType.Special:
                case RoomType.Secret:
                    return new NormalRoomSpawnRule(); // TODO: 전용 룰로 교체 예정
                default: return null;
            }
        }

        /// <summary>
        /// 단일 EnemySpawnEntry를 읽어 프리팹 소환 + EntityBase 격자 초기화 수행.
        ///
        /// 순서:
        ///   1. FloorSpawnConfig에서 프리팹 검색
        ///   2. 격자 좌표 → 월드 좌표 변환
        ///   3. Instantiate
        ///   4. EntityBase.Initialize(GridPos) 호출
        ///   5. _spawned 목록에 등록
        /// </summary>
        private void InstantiateOne(EnemySpawnEntry entry, FloorSpawnConfig config)
        {
            if (entry.enemyData == null) return;

            GameObject prefab = config.GetPrefab(entry.enemyData);
            if (prefab == null)
            {
                Debug.LogWarning($"EnemySpawner: '{config.name}'에서 {entry.enemyData.displayName} 프리팹 없음.");
                return;
            }

            // 격자 → 월드 좌표 변환
            Vector3 worldPos = GridSystem.Instance != null
                ? GridSystem.Instance.GridToWorld(entry.position)
                : new Vector3(entry.position.x, entry.position.y, 0);

            GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
            go.name = $"{entry.enemyData.id}_{_spawned.Count}";

            // EntityBase 격자 스냅 초기화
            var entity = go.GetComponent<EntityBase>();
            if (entity != null)
            {
                entity.Initialize(entry.position);
            }

            _spawned.Add(go);
        }

        /// <summary>
        /// _spawned 목록의 모든 몬스터 게임오브젝트를 Destroy하고 목록 초기화.
        /// 층 이동(Spawn 호출) 시 자동 실행.
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
