using System.Collections.Generic;
using UnityEngine;

namespace EnemySystem
{
    /// <summary>
    /// EnemyData SO와 실제 소환할 Unity 프리팹을 1:1로 매핑하는 보관용 클래스.
    /// FloorSpawnConfig.prefabMappings 리스트에서 사용됨.
    /// </summary>
    [System.Serializable]
    public class EnemyPrefabMapping
    {
        [Tooltip("매핑할 적의 SO 데이터")]
        public EnemyData enemyData;

        [Tooltip("런타임에 Instantiate할 유니티 프리팹")]
        public GameObject prefab;
    }

    /// <summary>
    /// 특정 층 구간(예: 1~5층)에서 사용하는 스폰 테이블과 프리팹 목록을 한 번에 관리하는 ScriptableObject.
    ///
    /// 인스펙터 설정 방법:
    ///   1. spawnTable: 해당 구간의 EnemySpawnTable SO 할당
    ///   2. prefabMappings: 출현할 적의 (EnemyData, Prefab) 쌍 목록 등록
    /// </summary>
    [CreateAssetMenu(fileName = "FloorSpawnConfig", menuName = "60s Dungeon/Floor Spawn Config")]
    public class FloorSpawnConfig : ScriptableObject
    {
        [Header("적용할 층 범위 설정")]
        [Tooltip("이 설정이 적용되는 최소 층 (depth)임.")]
        public int minDepth = 1;

        [Tooltip("이 설정이 적용되는 최대 층 (depth)임.")]
        public int maxDepth = 5;

        [Tooltip("이 설정이 적용되는 특정 층 목록임 (비어 있지 않으면 min/max 범위보다 우선 적용됨).")]
        public List<int> specificDepths = new List<int>();

        [Header("난이도 설정 테이블")]
        [Tooltip("이 구역의 방별 거리/확률 규칙을 갖는 SpawnTable")]
        public EnemySpawnTable spawnTable;

        [Header("몬스터 리소스 프리팹 매핑")]
        [Tooltip("이 구역에서 사용할 모든 (EnemyData, Prefab) 쌍 목록")]
        public List<EnemyPrefabMapping> prefabMappings = new List<EnemyPrefabMapping>();

        /// <summary>
        /// 지정한 깊이(depth)가 이 설정의 적용 범위에 속하는지 검사함.
        /// </summary>
        public bool IsDepthMatching(int depth)
        {
            if (specificDepths != null && specificDepths.Count > 0)
            {
                return specificDepths.Contains(depth);
            }
            return depth >= minDepth && depth <= maxDepth;
        }

        /// <summary>
        /// 특정 EnemyData에 대응하는 프리팹을 반환.
        /// 매핑 없으면 null 반환.
        /// </summary>
        /// <param name="data">검색할 적 SO 데이터</param>
        public GameObject GetPrefab(EnemyData data)
        {
            if (data == null) return null;

            foreach (var mapping in prefabMappings)
            {
                if (mapping.enemyData == data)
                    return mapping.prefab;
            }
            return null;
        }
    }
}
