using UnityEngine;

namespace EnemySystem
{
    /// <summary>
    /// 안전 구역(Safe Room)으로부터의 거리별 난이도 곡선과 스폰 풀을 관리하는 ScriptableObject.
    ///
    /// 사용 흐름:
    ///   EnemySpawner → GetConfigForDepth → FloorSpawnConfig → EnemySpawnTable → SpawnTier → EnemyPoolEntry
    ///
    /// 새 티어 추가 방법:
    ///   tiers 배열에 원소 추가. 인덱스 0 = Safe Room 거리 1인 방.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnTable", menuName = "60s Dungeon/Enemy Spawn Table")]
    public class EnemySpawnTable : ScriptableObject
    {
        [Header("거리별 스폰 티어 목록")]
        [Tooltip("인덱스 0 = 거리 1. 거리가 멀수록 높은 인덱스 티어 적용.")]
        public SpawnTier[] tiers;

        /// <summary>
        /// Safe Room에서의 거리 값에 해당하는 SpawnTier 반환.
        /// 범위 초과 시 마지막 티어(최고 난이도)로 Clamp.
        /// </summary>
        /// <param name="distanceFromSafe">Safe Room으로부터의 방 그래프 거리</param>
        public SpawnTier GetTier(int distanceFromSafe)
        {
            if (tiers == null || tiers.Length == 0) return null;
            // 인덱스 0이 거리 1이므로 1을 뺌
            int idx = Mathf.Clamp(distanceFromSafe - 1, 0, tiers.Length - 1);
            return tiers[idx];
        }
    }

    /// <summary>
    /// 특정 난이도 단계(티어)에서의 스폰 규칙.
    /// 몬스터 풀(pool)과 방당 최소/최대 마릿수, Elite 등장 확률을 정의.
    /// </summary>
    [System.Serializable]
    public class SpawnTier
    {
        [Tooltip("이 티어에서 스폰 가능한 몬스터 항목 목록 (가중치 포함)")]
        public EnemyPoolEntry[] pool;

        [Tooltip("방 1개당 최소 스폰 마릿수")]
        public int minCount = 1;

        [Tooltip("방 1개당 최대 스폰 마릿수")]
        public int maxCount = 3;
    }

    /// <summary>
    /// 스폰 풀 단일 항목.
    /// weight 값이 클수록 같은 풀 내에서 더 자주 선택됨.
    /// requiresLineOfSight가 true면 배치 시 직선 시야 3칸 확보 검증 후 배치.
    /// </summary>
    [System.Serializable]
    public class EnemyPoolEntry
    {
        [Tooltip("스폰될 몬스터의 SO 데이터")]
        public EnemyData enemyData;

        [Tooltip("가중치. 값이 클수록 출현 빈도 높음.")]
        public int weight = 1;

        [Tooltip("true이면 직선 3칸 시야가 확보된 위치에만 배치 (원거리 적 전용).")]
        public bool requiresLineOfSight;
    }
}
