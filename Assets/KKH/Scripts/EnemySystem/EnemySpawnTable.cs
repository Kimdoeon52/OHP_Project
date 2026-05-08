using UnityEngine;

namespace EnemySystem
{
    /// <summary>
    /// Safe Room으로부터의 그래프 거리(1~4)별 적 스폰 설정.
    /// RoomSpawnRule이 참조해서 난이도 곡선을 적용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnTable", menuName = "60s Dungeon/Enemy Spawn Table")]
    public class EnemySpawnTable : ScriptableObject
    {
        [Header("거리별 스폰 티어 (index 0 = distance 1)")]
        public SpawnTier[] tiers;

        /// <summary>
        /// distanceFromSafe 에 해당하는 스폰 티어 반환. 범위 초과 시 마지막 티어.
        /// </summary>
        public SpawnTier GetTier(int distanceFromSafe)
        {
            if (tiers == null || tiers.Length == 0) return null;
            int idx = Mathf.Clamp(distanceFromSafe - 1, 0, tiers.Length - 1);
            return tiers[idx];
        }
    }

    /// <summary>
    /// 하나의 난이도 단계에서 어떤 적을 몇 마리 스폰할 수 있는지 정의.
    /// </summary>
    [System.Serializable]
    public class SpawnTier
    {
        [Tooltip("이 티어에서 스폰 가능한 적 목록")]
        public EnemyPoolEntry[] pool;

        [Tooltip("이 티어에서 방당 최소 스폰 수")]
        public int minCount = 1;

        [Tooltip("이 티어에서 방당 최대 스폰 수")]
        public int maxCount = 3;

        [Tooltip("Elite 변종이 출현할 확률 (0~1)")]
        [Range(0f, 1f)]
        public float eliteChance = 0f;
    }

    /// <summary>
    /// 스폰 풀의 개별 항목. 가중치 기반 랜덤 선택에 사용.
    /// </summary>
    [System.Serializable]
    public class EnemyPoolEntry
    {
        public EnemyData enemyData;

        [Tooltip("선택 가중치 (높을수록 자주 등장)")]
        public int weight = 1;

        [Tooltip("true면 Archer처럼 직선 LOS가 확보된 셀에만 배치")]
        public bool requiresLineOfSight;
    }
}
