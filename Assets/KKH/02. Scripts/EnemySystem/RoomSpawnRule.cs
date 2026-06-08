using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EnemySystem
{
    /// <summary>
    /// 방 타입별 몬스터 스폰 알고리즘의 최상위 추상 클래스.
    /// 구현체: SafeRoomSpawnRule / NormalRoomSpawnRule / ExitRoomSpawnRule / BossRoomSpawnRule
    ///
    /// 새 방 타입 스폰 룰 추가 방법:
    ///   1. RoomSpawnRule 상속 클래스 작성
    ///   2. Generate() 구현
    ///   3. EnemySpawner.ResolveRule()에 case 추가
    /// </summary>
    public abstract class RoomSpawnRule
    {
        /// <summary>
        /// 방 데이터와 스폰 테이블을 받아 배치할 (EnemyData, 좌표) 목록 생산.
        /// IEnumerable로 yield return 방식 사용 → 대량 방 처리 시 메모리 효율적.
        /// </summary>
        /// <param name="room">방의 기하학적 데이터 (floorCells, doorCells, bounds 등)</param>
        /// <param name="spawnTable">현재 층 난이도 테이블</param>
        /// <param name="rng">시드 기반 결정론적 난수 생성기</param>
        /// <param name="countModifier">외부(FloorFeeling 등)에서 주입하는 마릿수 보정값</param>
        public abstract IEnumerable<EnemySpawnEntry> Generate(
            RoomData room,
            EnemySpawnTable spawnTable,
            DungeonRandom rng,
            int countModifier = 0);

        // ──────────── 공용 배치 연산 헬퍼 ────────────

        /// <summary>
        /// 지정 위치가 문(doorCells)과 맨해튼 거리 1 이내인지 판별.
        /// 문 바로 앞 배치 시 플레이어가 방 진입 즉시 피해를 입는 부정 경험 방지 목적.
        /// </summary>
        protected static bool IsTooCloseToDoor(Vector2Int pos, RoomData room)
        {
            foreach (var door in room.doorCells)
            {
                int manhattan = Mathf.Abs(pos.x - door.x) + Mathf.Abs(pos.y - door.y);
                if (manhattan <= 1) return true;
            }
            return false;
        }

        /// <summary>
        /// 4방향 중 1방향 이상으로 requiredClearance칸 이상의 직선 시야가 확보되는지 확인.
        /// requiresLineOfSight == true인 원거리 적(마법사/궁수) 배치 검증에 사용.
        /// </summary>
        /// <param name="pos">검증할 격자 좌표</param>
        /// <param name="requiredClearance">필요한 최소 시야 거리 (기본값 3칸)</param>
        protected static bool HasStraightLOS(Vector2Int pos, int requiredClearance = 3)
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions)
            {
                if (CheckClearance(pos, dir, requiredClearance)) return true;
            }
            return false;
        }

        /// <summary>
        /// 시작점에서 특정 방향으로 distance칸이 전부 Walkable인지 체크.
        /// GridSystem 없는 에디터 테스트 환경에서는 true 반환.
        /// </summary>
        private static bool CheckClearance(Vector2Int startPos, Vector2Int direction, int distance)
        {
            if (GridSystem.Instance == null) return true;

            for (int i = 1; i <= distance; i++)
            {
                Vector2Int next = startPos + (direction * i);
                if (!GridSystem.Instance.IsWalkable(next)) return false;
            }
            return true;
        }

        /// <summary>
        /// 방 바닥 셀 중 스폰 가능한 위치만 걸러내 반환.
        ///
        /// 제외 조건:
        ///   - 문 인접 타일 (IsTooCloseToDoor)
        ///   - Walkable이 아닌 타일 (낭떠러지, 벽 등)
        ///   - 다른 엔티티가 선점한 타일 (TurnManager.GetEntityAt != null)
        ///   - 플레이어가 서 있는 타일
        /// </summary>
        protected static List<Vector2Int> GetValidSpawnPositions(RoomData room)
        {
            var player = UnityEngine.Object.FindFirstObjectByType<GridPlayer>();
            Vector2Int? playerPos = player != null ? player.GridPosition : null;

            return room.floorCells
                .Where(pos => !IsTooCloseToDoor(pos, room))
                .Where(pos => GridSystem.Instance == null || GridSystem.Instance.IsWalkable(pos))
                .Where(pos => TurnManager.Instance == null || TurnManager.Instance.GetEntityAt(pos) == null)
                .Where(pos => GridSystem.Instance == null || !GridSystem.Instance.IsCellOccupied(pos))
                .Where(pos => !playerPos.HasValue || pos != playerPos.Value)
                .ToList();
        }

        /// <summary>
        /// Fisher-Yates 알고리즘으로 배치 후보 위치 목록 무작위 셔플.
        /// 시드 기반 rng를 사용하므로 같은 시드이면 항상 같은 결과.
        /// </summary>
        protected static void ShuffleList(List<Vector2Int> list, DungeonRandom rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// 가중치 기반 룰렛 휠 방식으로 풀에서 몬스터 1종 결정.
        /// weight 합산 후 랜덤 롤 → 누적 합 초과 시 해당 항목 선택.
        /// </summary>
        protected static EnemyPoolEntry PickWeightedRandom(EnemyPoolEntry[] pool, DungeonRandom rng)
        {
            if (pool == null || pool.Length == 0) return null;

            int totalWeight = 0;
            foreach (var entry in pool)
                totalWeight += entry.weight;

            if (totalWeight <= 0) return pool[0];

            int roll = rng.Range(0, totalWeight);
            int cumulative = 0;
            foreach (var entry in pool)
            {
                cumulative += entry.weight;
                if (roll < cumulative) return entry;
            }
            return pool[pool.Length - 1];
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Safe Room — 스폰 없음
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Safe Room 전용 룰.
    /// 몬스터 일절 없음 — 플레이어 휴식 및 계획 구간.
    /// </summary>
    public sealed class SafeRoomSpawnRule : RoomSpawnRule
    {
        public override IEnumerable<EnemySpawnEntry> Generate(
            RoomData room, EnemySpawnTable spawnTable, DungeonRandom rng, int countModifier = 0)
        {
            return Enumerable.Empty<EnemySpawnEntry>();
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Normal Room — 거리 비례 난이도 곡선 적용
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 일반 방 스폰 룰.
    ///
    /// 핵심 로직:
    ///   1. Safe Room 거리에 따른 SpawnTier 조회
    ///   2. 방 크기(Normal/Large/Giant)에 비례해 마릿수 스케일링
    ///   3. countModifier(FloorFeeling 보정값) 추가 적용
    ///   4. requiresLineOfSight 적은 LOS 확보 위치에만 배치, 없으면 일반 몬스터로 대체
    /// </summary>
    public sealed class NormalRoomSpawnRule : RoomSpawnRule
    {
        public override IEnumerable<EnemySpawnEntry> Generate(
            RoomData room, EnemySpawnTable spawnTable, DungeonRandom rng, int countModifier = 0)
        {
            if (spawnTable == null) yield break;

            SpawnTier tier = spawnTable.GetTier(room.distanceFromSafe);
            if (tier == null || tier.pool == null || tier.pool.Length == 0) yield break;

            var validPositions = GetValidSpawnPositions(room);
            if (validPositions.Count == 0) yield break;

            ShuffleList(validPositions, rng);

            // 방 크기 배율 결정
            float sizeMultiplier = 1f;
            switch (room.sizeCategory)
            {
                case SizeCategory.Normal: sizeMultiplier = 1f; break;
                case SizeCategory.Large: sizeMultiplier = 1.5f; break;
                case SizeCategory.Giant: sizeMultiplier = 2f; break;
            }

            int minCount = Mathf.RoundToInt(tier.minCount * sizeMultiplier);
            int maxCount = Mathf.RoundToInt(tier.maxCount * sizeMultiplier);

            // FloorFeeling 등 외부 보정값 적용
            minCount = Mathf.Max(0, minCount + countModifier);
            maxCount = Mathf.Max(0, maxCount + countModifier);

            int spawnCount = rng.Range(minCount, maxCount + 1);
            spawnCount = Mathf.Min(spawnCount, validPositions.Count);

            // 일반 스폰 풀에서 엘리트(Elite) 등급 몬스터 제외
            var nonElitePool = System.Array.FindAll(tier.pool, e => e.enemyData != null && e.enemyData.grade != EnemyGrade.Elite);

            // 현재 티어에 일반 몬스터가 없는 경우, 하위 티어에서 검색하여 대체 (ex. Area1의 4티어는 뱀[Elite]만 존재)
            if (nonElitePool.Length == 0)
            {
                for (int d = room.distanceFromSafe - 1; d >= 1; d--)
                {
                    var fallbackTier = spawnTable.GetTier(d);
                    if (fallbackTier != null && fallbackTier.pool != null)
                    {
                        nonElitePool = System.Array.FindAll(fallbackTier.pool, e => e.enemyData != null && e.enemyData.grade != EnemyGrade.Elite);
                        if (nonElitePool.Length > 0)
                        {
                            tier = fallbackTier; // 해당 티어의 기준 값(마릿수 등) 차용
                            break;
                        }
                    }
                }
            }

            if (nonElitePool.Length == 0) yield break;

            int posIndex = 0;
            for (int i = 0; i < spawnCount && posIndex < validPositions.Count; i++)
            {
                EnemyPoolEntry picked = PickWeightedRandom(nonElitePool, rng);
                if (picked == null || picked.enemyData == null) continue;

                // LOS 필요 적은 적합한 위치 탐색
                if (picked.requiresLineOfSight)
                {
                    bool found = false;
                    int tempIndex = posIndex;
                    while (tempIndex < validPositions.Count)
                    {
                        if (HasStraightLOS(validPositions[tempIndex]))
                        {
                            found = true;
                            posIndex = tempIndex;
                            break;
                        }
                        tempIndex++;
                    }

                    if (!found)
                    {
                        // LOS 위치 없음 → requiresLineOfSight = false인 일반 몬스터로 대체
                        var nonLOSPool = System.Array.FindAll(nonElitePool, e => !e.requiresLineOfSight);
                        if (nonLOSPool.Length > 0)
                        {
                            picked = PickWeightedRandom(nonLOSPool, rng);
                        }
                        else
                        {
                            continue; // 대체 몬스터도 없으면 스킵
                        }
                    }
                }

                yield return new EnemySpawnEntry(picked.enemyData, validPositions[posIndex]);
                posIndex++;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Exit Room — 최고 티어 + 최소 2마리 경비
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 출구 방 스폰 룰.
    ///
    /// NormalRoom과 다른 점:
    ///   - 항상 SpawnTable 최고 티어(GetTier(int.MaxValue)) 사용
    ///   - 최소 스폰 마릿수 2 보장 (출구 앞 경비 강화)
    /// </summary>
    public sealed class ExitRoomSpawnRule : RoomSpawnRule
    {
        public override IEnumerable<EnemySpawnEntry> Generate(
            RoomData room, EnemySpawnTable spawnTable, DungeonRandom rng, int countModifier = 0)
        {
            if (spawnTable == null) yield break;

            // Exit Room은 항상 최고 티어
            SpawnTier tier = spawnTable.GetTier(int.MaxValue);
            if (tier == null || tier.pool == null || tier.pool.Length == 0) yield break;

            var validPositions = GetValidSpawnPositions(room);
            if (validPositions.Count == 0) yield break;

            ShuffleList(validPositions, rng);

            // 방 크기 배율
            float sizeMultiplier = 1f;
            switch (room.sizeCategory)
            {
                case SizeCategory.Normal: sizeMultiplier = 1f; break;
                case SizeCategory.Large: sizeMultiplier = 1.5f; break;
                case SizeCategory.Giant: sizeMultiplier = 2f; break;
            }

            int minCount = Mathf.RoundToInt(tier.minCount * sizeMultiplier);
            int maxCount = Mathf.RoundToInt(tier.maxCount * sizeMultiplier);

            minCount = Mathf.Max(0, minCount + countModifier);
            maxCount = Mathf.Max(0, maxCount + countModifier);

            // Exit Room 최소 2마리 보장
            int spawnCount = Mathf.Max(2, rng.Range(minCount, maxCount + 1));
            spawnCount = Mathf.Min(spawnCount, validPositions.Count);

            int posIndex = 0;
            for (int i = 0; i < spawnCount && posIndex < validPositions.Count; i++)
            {
                EnemyPoolEntry picked = PickWeightedRandom(tier.pool, rng);
                if (picked == null || picked.enemyData == null) continue;

                if (picked.requiresLineOfSight)
                {
                    bool found = false;
                    int tempIndex = posIndex;
                    while (tempIndex < validPositions.Count)
                    {
                        if (HasStraightLOS(validPositions[tempIndex]))
                        {
                            found = true;
                            posIndex = tempIndex;
                            break;
                        }
                        tempIndex++;
                    }

                    if (!found)
                    {
                        var nonLOSPool = System.Array.FindAll(tier.pool, e => !e.requiresLineOfSight);
                        if (nonLOSPool.Length > 0)
                        {
                            picked = PickWeightedRandom(nonLOSPool, rng);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                yield return new EnemySpawnEntry(picked.enemyData, validPositions[posIndex]);
                posIndex++;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Boss Room — 방 중앙에 보스 1마리
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 보스 방 스폰 룰.
    ///
    /// 핵심 동작:
    ///   방 bounds 중앙과 가장 가까운 Walkable 셀에 보스 1마리 배치.
    ///   _bossData가 null이면 SpawnTable 최고 티어 0번 몬스터로 폴백.
    ///
    /// 사용 방법:
    ///   BossRoomSpawnRule(bossData)로 보스 데이터 직접 주입.
    ///   또는 인수 없이 생성하면 자동 폴백.
    /// </summary>
    public sealed class BossRoomSpawnRule : RoomSpawnRule
    {
        [Tooltip("배치할 보스 몬스터 SO 데이터. null이면 SpawnTable 최고 티어 폴백.")]
        private readonly EnemyData _bossData;

        /// <summary> 보스 데이터를 명시적으로 주입하는 생성자. </summary>
        public BossRoomSpawnRule(EnemyData bossData)
        {
            _bossData = bossData;
        }

        /// <summary>
        /// 보스 데이터 미설정 시 SpawnTable 폴백을 사용하는 기본 생성자.
        /// </summary>
        public BossRoomSpawnRule() { }

        public override IEnumerable<EnemySpawnEntry> Generate(RoomData room, EnemySpawnTable spawnTable, DungeonRandom rng, int countModifier = 0)
        {
            // 방 bounds 중앙 좌표 계산
            Vector2Int center = new Vector2Int(
                room.bounds.x + room.bounds.width / 2,
                room.bounds.y + room.bounds.height / 2
            );

            // 중앙과 가장 가까운 Walkable 셀 탐색
            // Walkable 없으면 그냥 가장 가까운 floorCell 사용
            Vector2Int bestCenter = center;
            float minDist = float.MaxValue;
            bool foundWalkable = false;

            foreach (var cell in room.floorCells)
            {
                bool isWalkable = GridSystem.Instance == null || GridSystem.Instance.IsWalkable(cell);
                if (isWalkable)
                {
                    float dist = (cell - center).sqrMagnitude;
                    if (!foundWalkable || dist < minDist)
                    {
                        minDist = dist;
                        bestCenter = cell;
                        foundWalkable = true;
                    }
                }
                else if (!foundWalkable)
                {
                    // Walkable 아직 없음 → 임시 fallback으로 가장 가까운 셀
                    float dist = (cell - center).sqrMagnitude;
                    if (dist < minDist)
                    {
                        minDist = dist;
                        bestCenter = cell;
                    }
                }
            }
            center = bestCenter;

            // 보스 데이터 결정 (_bossData 없으면 SpawnTable 최고 티어 폴백)
            EnemyData boss = _bossData;
            if (boss == null && spawnTable != null)
            {
                var tier = spawnTable.GetTier(int.MaxValue);
                if (tier?.pool != null && tier.pool.Length > 0)
                    boss = tier.pool[0].enemyData;
            }

            if (boss != null)
            {
                yield return new EnemySpawnEntry(boss, center);
            }
        }
    }
}
