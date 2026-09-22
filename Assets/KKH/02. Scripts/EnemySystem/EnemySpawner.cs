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
                type = RoomType.Normal,
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
        ///   3. 엘리트 몬스터를 Exit Room에 확정 배치 (단독)
        ///   4. Exit Room 제외한 각 방에 RoomSpawnRule.Generate() 실행
        ///   5. 결과(EnemySpawnEntry) 목록을 InstantiateOne으로 소환
        ///   6. SpawnCountModifier 리셋
        /// </summary>
        /// <param name="rooms">던전 방 목록</param>
        /// <param name="depth">현재 층 깊이</param>
        /// <param name="rng">시드 기반 결정론적 난수 생성기</param>
        public void Spawn(List<RoomData> rooms, int depth, DungeonRandom rng)
        {
            ClearPreviousSpawns();

            if (rooms == null || rng == null) return;

            // 보스 층인 경우 동적 적 스폰을 완전히 제외
            if (MapGenerator.Instance != null && MapGenerator.Instance.IsBossDepth(depth))
            {
                Debug.Log($"EnemySpawner: {depth}층은 보스 층이므로 적을 스폰하지 않음.");
                return;
            }

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

            // 1단계: 엘리트 몬스터 Exit Room 확정 배치 (단독)
            SpawnEliteInExitRoom(rooms, currentConfig, rng);
            SpawnGolemInSpecialMonsterRoom(rooms, currentConfig, rng);

            // 2단계: 방별 일반 스폰 (Exit Room은 엘리트 단독이므로 스킵)
            foreach (var room in rooms)
            {
                if (room.type == RoomType.Tunnel) continue; // 복도는 스킵
                if (room.type == RoomType.Exit) continue;   // Exit Room은 엘리트 단독
                if (room.type == RoomType.Special && room.specialKind == SpecialRoomKind.Monster) continue;
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

        // ──────────── 엘리트 Exit Room 전용 스폰 ────────────

        /// <summary>
        /// 스폰 테이블 전체 풀에서 EnemyGrade.Elite인 몬스터를 필터링하고,
        /// Exit Room의 출구 타일 인접(상하좌우) Walkable 위치에 딱 1마리만 배치.
        /// 엘리트가 풀에 없거나 Exit Room이 없으면 아무것도 안 함.
        /// </summary>
        private void SpawnEliteInExitRoom(List<RoomData> rooms, FloorSpawnConfig config, DungeonRandom rng)
        {
            if (config == null)
            {
                Debug.LogWarning("[SpawnEliteInExitRoom] FloorSpawnConfig가 null입니다.");
                return;
            }
            if (config.spawnTable == null)
            {
                Debug.LogWarning($"[SpawnEliteInExitRoom] {config.name}의 spawnTable이 null입니다.");
                return;
            }
            if (config.spawnTable.tiers == null)
            {
                Debug.LogWarning($"[SpawnEliteInExitRoom] {config.spawnTable.name}의 tiers가 null입니다.");
                return;
            }

            // 1. 스폰 테이블의 모든 티어에서 Elite 등급 몬스터 수집
            List<EnemyData> eliteCandidates = new List<EnemyData>();
            foreach (var tier in config.spawnTable.tiers)
            {
                if (tier?.pool == null) continue;
                foreach (var poolEntry in tier.pool)
                {
                    if (poolEntry?.enemyData != null)
                    {
                        if (poolEntry.enemyData.grade == EnemyGrade.Elite && !eliteCandidates.Contains(poolEntry.enemyData))
                        {
                            eliteCandidates.Add(poolEntry.enemyData);
                        }
                    }
                }
            }

            Debug.Log($"[SpawnEliteInExitRoom] 수집된 엘리트 후보군 개수: {eliteCandidates.Count}");
            foreach (var candidate in eliteCandidates)
            {
                Debug.Log($"[SpawnEliteInExitRoom] 엘리트 후보: {candidate.displayName} (grade: {candidate.grade})");
            }

            if (eliteCandidates.Count == 0) return; // 이 층에 엘리트 없음

            // 2. Exit Room 탐색
            RoomData exitRoom = null;
            Debug.Log($"[SpawnEliteInExitRoom] 전체 방(rooms) 목록 탐색 (총 {rooms.Count}개):");
            foreach (var room in rooms)
            {
                Debug.Log($"  - 방 ID/타입: {room.type}, floorCells 개수: {(room.floorCells != null ? room.floorCells.Count : 0)}");
                if (room.type == RoomType.Exit)
                {
                    exitRoom = room;
                }
            }

            if (exitRoom == null)
            {
                Debug.LogWarning("[SpawnEliteInExitRoom] Exit Room 검출 실패: rooms 목록에 RoomType.Exit 방이 없습니다.");
                return;
            }
            else
            {
                Debug.Log($"[SpawnEliteInExitRoom] Exit Room 검출 성공! floorCells 개수: {exitRoom.floorCells.Count}");
            }

            // 3. 출구(Exit) 타일 인접 Walkable 위치 탐색
            //    MapGenerator.FindExitCell과 동일한 방식으로 실제 출구 타일을 찾음
            Vector2Int exitTile = Vector2Int.zero;
            bool exitTileFound = false;

            if (exitRoom.floorCells != null && exitRoom.floorCells.Count > 0)
            {
                Vector2 center = Vector2.zero;
                foreach (var c in exitRoom.floorCells)
                    center += c;
                center /= exitRoom.floorCells.Count;

                Vector2Int best = exitRoom.floorCells[0];
                float bestDist = float.MaxValue;
                foreach (var c in exitRoom.floorCells)
                {
                    float d = ((Vector2)c - center).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = c;
                    }
                }
                exitTile = best;
                exitTileFound = true;
            }

            Vector2Int spawnPos = Vector2Int.zero;
            bool found = false;
            Vector2Int[] adjacentDirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            if (exitTileFound)
            {
                foreach (var dir in adjacentDirs)
                {
                    Vector2Int candidate = exitTile + dir;
                    // floorCells에 포함되고 Walkable인 위치만 허용
                    if (exitRoom.floorCells.Contains(candidate))
                    {
                        bool isWalkable = GridSystem.Instance == null || GridSystem.Instance.IsWalkable(candidate);
                        if (isWalkable)
                        {
                            spawnPos = candidate;
                            found = true;
                            break;
                        }
                    }
                }
            }

            // 폴백: 인접 셀을 못 찾으면 방 중앙 Walkable 셀 사용
            if (!found)
            {
                Vector2Int center = new Vector2Int(
                    exitRoom.bounds.x + exitRoom.bounds.width / 2,
                    exitRoom.bounds.y + exitRoom.bounds.height / 2
                );
                float minDist = float.MaxValue;
                foreach (var cell in exitRoom.floorCells)
                {
                    bool isWalkable = GridSystem.Instance == null || GridSystem.Instance.IsWalkable(cell);
                    if (isWalkable)
                    {
                        float dist = (cell - center).sqrMagnitude;
                        if (dist < minDist)
                        {
                            minDist = dist;
                            spawnPos = cell;
                            found = true;
                        }
                    }
                }
            }

            if (!found) return; // Walkable 셀 없음

            // 4. 랜덤으로 엘리트 1종 선택 후 배치
            EnemyData selectedElite = eliteCandidates[rng.Range(0, eliteCandidates.Count)];
            var spawnEntry = new EnemySpawnEntry(selectedElite, spawnPos);
            InstantiateOne(spawnEntry, config);

            Debug.Log($"EnemySpawner: Exit Room에 엘리트 '{selectedElite.displayName}' 배치 완료 ({spawnPos}).");
            return;
        }

        /// <summary>
        /// SpecialMonsterRoom에 골렘을 배치함
        /// </summary>
        private void SpawnGolemInSpecialMonsterRoom(List<RoomData> rooms, FloorSpawnConfig config, DungeonRandom rng)
        {
            if (config == null || config.prefabMappings == null)
            {
                Debug.LogWarning("스페셜 룸 골램 생성 실패: 설정이 없거나 프리팹 매핑이 비어있습니다.");
                return;
            }

            // 1. EnemyData에서 골램 정보 찾기
            EnemyData golemData = null;
            foreach (var mapping in config.prefabMappings)
            {
                if (mapping.enemyData != null && mapping.enemyData.displayName == "Golem")
                {
                    golemData = mapping.enemyData;
                    break;
                }
            }
            if (golemData == null)
            {
                Debug.Log("스페셜 룸 골램 생성 실패: EnemyData에서 골램 정보를 찾을 수 없습니다.");
                return;
            }
            foreach (var room in rooms)
            {
                if (room.type == RoomType.Special && room.specialKind == SpecialRoomKind.Monster)
                {
                    Vector2Int center = new Vector2Int(
                        room.bounds.x + room.bounds.width / 2,
                        room.bounds.y + room.bounds.height / 2
                    );
                    Vector2Int spawnPos = center;
                    bool foundWalkable = false;
                    float minDist = float.MaxValue;

                    foreach (var cell in room.floorCells)
                    {
                        bool isWalkable = GridSystem.Instance == null || GridSystem.Instance.IsWalkable(cell);
                        if (isWalkable)
                        {
                            float dist = (cell - center).sqrMagnitude;
                            if (dist < minDist)
                            {
                                minDist = dist;
                                spawnPos = cell;
                                foundWalkable = true;
                            }
                        }
                    }

                    if (!foundWalkable && room.floorCells.Count > 0)
                    {
                        spawnPos = room.floorCells[0];
                    }

                    var spawnEntry = new EnemySpawnEntry(golemData, spawnPos);
                    InstantiateOne(spawnEntry, config);

                    Debug.Log($"EnemySpawner: 스페셜 몬스터 룸({room.specialKind})에 골램 '{golemData.displayName}' 배치 완료 ({spawnPos}).");
                }
            }
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
                case RoomType.Safe: return new SafeRoomSpawnRule();
                case RoomType.Normal: return new NormalRoomSpawnRule();
                case RoomType.Exit: return new ExitRoomSpawnRule();
                case RoomType.Boss: return new BossRoomSpawnRule();
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
