using System.Collections.Generic;
using UnityEngine;

namespace EnemySystem
{
    [System.Serializable]
    public class EnemyPrefabMapping
    {
        public EnemyData enemyData;
        public GameObject prefab;
    }

    /// <summary>
    /// 특정 층(또는 구역) 범위 내에서 사용하는 적 스폰 테이블과 프리팹 매핑 데이터.
    /// </summary>
    [CreateAssetMenu(fileName = "FloorSpawnConfig", menuName = "60s Dungeon/Floor Spawn Config")]
    public class FloorSpawnConfig : ScriptableObject
    {
        [Header("스폰 규칙 및 난이도 곡선")]
        public EnemySpawnTable spawnTable;

        //  [Tooltip("보스방이 있을 경우 출현할 보스 데이터")]
        //  public EnemyData bossData;

        [Header("EnemyData ↔ 프리팹 매핑 리스트")]
        public List<EnemyPrefabMapping> prefabMappings = new List<EnemyPrefabMapping>();

        /// <summary>
        /// EnemyData에 해당하는 프리팹을 검색하여 반환합니다.
        /// </summary>
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
