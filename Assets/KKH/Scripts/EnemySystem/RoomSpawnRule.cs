using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Enemies
{
     public class RoomNode 
    { 
        public List<Vector2Int> Entrance = new List<Vector2Int>(); 
        public bool IsWalkable(Vector2Int pos) => true; // 임시 구현
    }
    public class EnemySpawnPool { public object GetArcher() => null; public object GetTracker() => null; }
    public class DungeonRandom { public bool ShouldSpawnArcher() => true; public bool ShouldSpawnTracker() => true; }
    public struct Spawn 
    { 
        public object Enemy; 
        public Vector2Int Position;
        public Spawn(object enemy, Vector2Int pos) { Enemy = enemy; Position = pos; }
    }
    /// <summary>
    /// 특정 방 타입에 대한 적 스폰 규칙을 정의하는 추상 클래스
    /// </summary>
    public abstract class RoomSpawnRule
    {
        /// <summary>
        /// 적 스폰 규칙을 구현하고 스폰 목록을 반환합니다.
        /// <param name="room">적을 스폰할 방</param>
        /// <param name="pool">적 스폰 풀</param>
        /// <param name="rng">던전 랜덤 생성기</param>
        /// <param name="graphDistanceFromSafe">Safe Room으로부터의 그래프 거리</param>
        /// <returns>스폰할 적 목록</returns>
        /// </summary>
        public abstract IEnumerable<Spawn> Generate(RoomNode room, EnemySpawnPool pool, DungeonRandom rng, int graphDistanceFromSafe);

        /// 공통 스폰 제약 로직 
        /// <summary>
        /// 입구 인접 셀(1칸 거리) 스폰 금지 (즉사 방지)
        /// Archer는 4방향 직선 LOS 확보 셀에만 배치
        /// </summary>
        protected bool IsTooCloseToEntrance(Vector2Int pos, RoomNode room)
        {
            //room.Entrance는 해당 방과 연결된 통로들의 좌표 리스트
            foreach (var entrancePos in room.Entrance)
            {
                if (Vector2Int.Distance(pos, entrancePos) <= 1.5f)
                {
                    return true;
                }
            }
            return false;
        }

        protected bool HasStraightLOS(Vector2Int pos, Vector2Int room)
        {
            // 상하좌우 4방향으로 벽에 부딪히기 전까지 일정 거리 이상의 빈 공간이 있는지 체크
            int requiredClearance = 3; // 최소 3칸 이상 직선으로 빈 공간 필요

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions)
            {
                if (CheckClearance(pos, dir, room))
                    return true;
            }
            return false;
        }

        private bool CheckClearance(Vector2Int startPos, Vector2Int direction, RoomNode room)
        {
           for (int i = 1; i <= distance; i++)
            {
                Vector2Int nextPos = start + (dir * i);
                if (!room.IsWalkable(nextPos)) return false; // 벽이나 장애물을 만나면 실패
            }
            return true;   
        }


    }

    public sealed class SafeRoomSpawnRule : RoomSpawnRule
    {
        /// <summary>
        /// Safe Room에서는 적을 스폰하지 않습니다.
        /// </summary>
        public override IEnumerable<Spawn> Generate(RoomNode room, EnemySpawnPool pool, DungeonRandom rng, int graphDistanceFromSafe)
        {
            return System.Linq.Enumerable.Empty<Spawn>();
        }
    }

    public sealed class NormalRoomSpawnRule : RoomSpawnRule
    {
        /// <summary>
        /// 일반 방의 적 스폰 규칙: Safe Room으로부터의 거리에 따라 난이도 조정
        /// </summary>
        public override IEnumerable<Spawn> Generate(RoomNode room, EnemySpawnPool pool, DungeonRandom rng, int graphDistanceFromSafe)
        {
            // TODO: 거리 기반 난이도 곡선 구현
            return System.Linq.Enumerable.Empty<Spawn>();
        }
    }

    public sealed class ExitRoomSpawnRule : RoomSpawnRule
    {
        /// <summary>
        /// Exit Room의 적 스폰 규칙: 강한 적 + Elite 스폰
        /// </summary>
        public override IEnumerable<Spawn> Generate(RoomNode room, EnemySpawnPool pool, DungeonRandom rng, int graphDistanceFromSafe)
        {
            // TODO: 강한 적 + Elite 스폰 구현
            return System.Linq.Enumerable.Empty<Spawn>();
        }
    }

    public sealed class BossRoomSpawnRule : RoomSpawnRule
    {
        /// <summary>
        /// Boss Room의 적 스폰 규칙: 보스 1체 스폰
        /// </summary>
        public override IEnumerable<Spawn> Generate(RoomNode room, EnemySpawnPool pool, DungeonRandom rng, int graphDistanceFromSafe)
        {
            // TODO: 보스 1체 스폰 구현
            return System.Linq.Enumerable.Empty<Spawn>();
        }
    }

}
    public sealed class NormalRoomSpawnRule : RoomSpawnRule
    {
        public override IEnumerable<Spawn> Generate(RoomNode room, EnemySpawnPool pool, DungeonRandom rng, int graphDistanceFromSafe)
        {
            List<Spawn> spawns = new List<Spawn>();
            
            // 1. 해당 방에서 스폰 가능한 모든 빈 타일 목록을 가져옴
            var candidatePositions = room.GetEmptyTiles(); 

            // 2. 공통 제약으로 필터링 (입구 근처 제외)
            var validPositions = candidatePositions
                .Where(pos => !IsTooCloseToEntrance(pos, room))
                .ToList();

            // 3. 몬스터 타입별 추가 제약 적용
            foreach (var pos in validPositions)
            {
                // 예: 궁수(Archer) 스폰 시도 시 시야 체크
                if (ShouldSpawnArcher(rng) && HasStraightLOS(pos, room))
                {
                    spawns.Add(new Spawn(pool.GetArcher(), pos));
                }
                // Tracker는 입구만 아니면 어디든 가능
                else if (ShouldSpawnTracker(rng))
                {
                    spawns.Add(new Spawn(pool.GetTracker(), pos));
                }
            }

            return spawns;
        }
    }
