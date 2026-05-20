using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EnemySystem
{
    /// <summary>
    /// 방 타입별 적 스폰 규칙 추상 클래스.
    /// MapGenerator의 Spawn Phase에서 방마다 호출되어 EnemySpawnEntry 리스트를 반환한다.
    /// </summary>
    public abstract class RoomSpawnRule
    {
        /// <summary>
        /// 해당 방에 스폰할 적 목록을 생성한다.
        /// </summary>
        /// <param name="room">맵 생성기가 만든 방 데이터</param>
        /// <param name="spawnTable">난이도 곡선 테이블 (SO)</param>
        /// <param name="rng">시드 기반 결정론적 난수</param>
        /// <param name="countModifier">외부(FloorFeeling 등)에 의한 스폰 수 보정치</param>
        /// <returns>스폰할 적-위치 쌍 목록</returns>
        public abstract IEnumerable<EnemySpawnEntry> Generate(
            RoomData room,
            EnemySpawnTable spawnTable,
            DungeonRandom rng,
            int countModifier = 0);

        // ──────────── 공용 헬퍼 ────────────

        /// <summary>
        /// 해당 셀이 방 입구(doorCells)에서 맨해튼 거리 1 이내인지.
        /// GDD 규칙: 입구 인접 1칸 거리 스폰 금지 (즉사 방지).
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
        /// 4방향 직선으로 일정 거리 이상 통행 가능한 시야가 확보되는지.
        /// Archer처럼 requiresLineOfSight인 적의 배치 유효성 검증용.
        /// </summary>
        protected static bool HasStraightLOS(Vector2Int pos, int requiredClearance = 3)
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions)
            {
                if (CheckClearance(pos, dir, requiredClearance)) return true;
            }
            return false;
        }

        private static bool CheckClearance(Vector2Int startPos, Vector2Int direction, int distance)
        {
            // GridSystem이 없으면 검증 불가 → true 반환 (에디터 테스트 대비)
            if (GridSystem.Instance == null) return true;

            for (int i = 1; i <= distance; i++)
            {
                Vector2Int next = startPos + (direction * i);
                if (!GridSystem.Instance.IsWalkable(next)) return false;
            }
            return true;
        }

        /// <summary>
        /// 방의 바닥 셀에서 스폰 가능한 후보 위치를 필터링한다.
        /// - 입구 인접 셀 제외 (맨해튼 거리 1 이내 즉사 방지)
        /// - 낭떨어지(Chasm)나 벽 타일 등 통행 불가 셀 제외 (GridSystem)
        /// - 이미 다른 엔티티가 점유한 셀 제외 (TurnManager)
        /// </summary>
        protected static List<Vector2Int> GetValidSpawnPositions(RoomData room)
        {
            return room.floorCells
                .Where(pos => !IsTooCloseToDoor(pos, room))
                .Where(pos => GridSystem.Instance == null || GridSystem.Instance.IsWalkable(pos))
                .Where(pos => TurnManager.Instance == null || TurnManager.Instance.GetEntityAt(pos) == null)
                .ToList();
        }

        /// <summary>
        /// 피셔-예이츠 셔플로 리스트 순서를 무작위화한다.
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
        /// 가중치 기반 랜덤으로 풀에서 적 하나를 선택한다.
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
    //  Safe Room — 적 스폰 없음
    // ════════════════════════════════════════════════════════════════

    public sealed class SafeRoomSpawnRule : RoomSpawnRule
    {
        public override IEnumerable<EnemySpawnEntry> Generate(
            RoomData room, EnemySpawnTable spawnTable, DungeonRandom rng, int countModifier = 0)
        {
            return Enumerable.Empty<EnemySpawnEntry>();
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Normal Room — distanceFromSafe 기반 난이도 곡선
    // ════════════════════════════════════════════════════════════════

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

            // 셔플해서 순서 무작위화
            ShuffleList(validPositions, rng);

            // 방 크기 카테고리에 비례하여 스폰 개수 스케일링
            float sizeMultiplier = 1f;
            switch (room.sizeCategory)
            {
                case SizeCategory.Normal:
                    sizeMultiplier = 1f;
                    break;
                case SizeCategory.Large:
                    sizeMultiplier = 1.5f;
                    break;
                case SizeCategory.Giant:
                    sizeMultiplier = 2f;
                    break;
            }

            int minCount = Mathf.RoundToInt(tier.minCount * sizeMultiplier);
            int maxCount = Mathf.RoundToInt(tier.maxCount * sizeMultiplier);

            // TrapsFeeling 등 외부 보정치 적용
            minCount = Mathf.Max(0, minCount + countModifier);
            maxCount = Mathf.Max(0, maxCount + countModifier);

            int spawnCount = rng.Range(minCount, maxCount + 1);
            spawnCount = Mathf.Min(spawnCount, validPositions.Count);

            int posIndex = 0;
            for (int i = 0; i < spawnCount && posIndex < validPositions.Count; i++)
            {
                EnemyPoolEntry picked = PickWeightedRandom(tier.pool, rng);
                if (picked == null || picked.enemyData == null) continue;

                // LOS 요구 적 (Archer 등)은 시야 확보 셀 찾기
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
                        // LOS 적을 스폰할 자리가 없으면, 풀에서 requiresLineOfSight가 거짓인 다른 일반 몬스터를 대신 뽑음
                        var nonLOSPool = System.Array.FindAll(tier.pool, e => !e.requiresLineOfSight);
                        if (nonLOSPool.Length > 0)
                        {
                            picked = PickWeightedRandom(nonLOSPool, rng);
                        }
                        else
                        {
                            // 대체할 수 있는 일반 몹도 없으면 스킵
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
    //  Exit Room — 강한 적 + Elite 확정 스폰
    // ════════════════════════════════════════════════════════════════

    public sealed class ExitRoomSpawnRule : RoomSpawnRule
    {
        public override IEnumerable<EnemySpawnEntry> Generate(
            RoomData room, EnemySpawnTable spawnTable, DungeonRandom rng, int countModifier = 0)
        {
            if (spawnTable == null) yield break;

            // Exit Room은 항상 최고 티어 사용
            SpawnTier tier = spawnTable.GetTier(int.MaxValue);
            if (tier == null || tier.pool == null || tier.pool.Length == 0) yield break;

            var validPositions = GetValidSpawnPositions(room);
            if (validPositions.Count == 0) yield break;

            ShuffleList(validPositions, rng);

            // 방 크기 카테고리에 비례하여 스폰 개수 스케일링
            float sizeMultiplier = 1f;
            switch (room.sizeCategory)
            {
                case SizeCategory.Normal:
                    sizeMultiplier = 1f;
                    break;
                case SizeCategory.Large:
                    sizeMultiplier = 1.5f;
                    break;
                case SizeCategory.Giant:
                    sizeMultiplier = 2f;
                    break;
            }

            int minCount = Mathf.RoundToInt(tier.minCount * sizeMultiplier);
            int maxCount = Mathf.RoundToInt(tier.maxCount * sizeMultiplier);

            // TrapsFeeling 등 외부 보정치 적용
            minCount = Mathf.Max(0, minCount + countModifier);
            maxCount = Mathf.Max(0, maxCount + countModifier);

            // Exit Room은 최소 2체, 최대는 티어 설정 따름
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
                        // LOS 적을 스폰할 자리가 없으면, 풀에서 requiresLineOfSight가 거짓인 다른 일반 몬스터를 대신 뽑음
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
    //  Boss Room — 보스 1체를 방 중앙에 스폰
    // ════════════════════════════════════════════════════════════════

    public sealed class BossRoomSpawnRule : RoomSpawnRule
    {
        [Tooltip("보스 EnemyData — 외부에서 주입")]
        private readonly EnemyData _bossData;

        public BossRoomSpawnRule(EnemyData bossData)
        {
            _bossData = bossData;
        }

        /// <summary>
        /// 파라미터 없는 생성자 (spawnTable의 최고 티어에서 보스를 찾는 폴백용).
        /// </summary>
        public BossRoomSpawnRule() { }

        public override IEnumerable<EnemySpawnEntry> Generate(
            RoomData room, EnemySpawnTable spawnTable, DungeonRandom rng, int countModifier = 0)
        {
            // 방 중앙 좌표 계산
            Vector2Int center = new Vector2Int(
                room.bounds.x + room.bounds.width / 2,
                room.bounds.y + room.bounds.height / 2
            );

            // 우선순위:
            // 1. GridSystem이 있으면 walkable한 바닥 셀 중 중앙과 가장 가까운 곳
            // 2. GridSystem이 없으면 그냥 floorCells 중 중앙과 가장 가까운 곳
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
                    // 아직 walkable한 타일을 하나도 못 찾았다면, fallback으로 가장 가까운 cell 선택
                    float dist = (cell - center).sqrMagnitude;
                    if (dist < minDist)
                    {
                        minDist = dist;
                        bestCenter = cell;
                    }
                }
            }
            center = bestCenter;

            // 보스 데이터 결정
            EnemyData boss = _bossData;
            if (boss == null && spawnTable != null)
            {
                // spawnTable 최고 티어의 첫 번째 적을 보스로 폴백
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
