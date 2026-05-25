using UnityEngine;

namespace EnemySystem
{
    /// <summary>
    /// RoomSpawnRule.Generate()의 반환 결과 단위.
    /// "이 EnemyData를 이 격자 좌표에 스폰하라"는 명령 쌍을 담음.
    /// EnemySpawner.InstantiateOne()에서 소비됨.
    /// </summary>
    public struct EnemySpawnEntry
    {
        [Tooltip("스폰할 적의 스탯/보상 데이터 (ScriptableObject)")]
        public EnemyData enemyData;

        [Tooltip("초기 배치될 격자 좌표 (Vector2Int)")]
        public Vector2Int position;

        /// <summary>
        /// 스폰 정보 쌍 생성자.
        /// </summary>
        /// <param name="data">적 SO 데이터</param>
        /// <param name="pos">배치 격자 좌표</param>
        public EnemySpawnEntry(EnemyData data, Vector2Int pos)
        {
            enemyData = data;
            position  = pos;
        }
    }
}
