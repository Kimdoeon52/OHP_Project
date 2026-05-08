using UnityEngine;

namespace EnemySystem
{
    /// <summary>
    /// RoomSpawnRule이 반환하는 스폰 결과 단위.
    /// "이 EnemyData를 이 격자 좌표에 스폰하라"는 의미.
    /// </summary>
    public struct EnemySpawnEntry
    {
        public EnemyData enemyData;
        public Vector2Int position;

        public EnemySpawnEntry(EnemyData data, Vector2Int pos)
        {
            enemyData = data;
            position = pos;
        }
    }
}
