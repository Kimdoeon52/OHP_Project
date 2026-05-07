using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Enemies
{
    // --- [임시 정의] 컴파일 에러 방지용 ---
    public class RoomNode 
    { 
        public List<Vector2Int> Entrance = new List<Vector2Int>(); 
        public bool IsWalkable(Vector2Int pos) => true; 
        public List<Vector2Int> GetEmptyTiles() => new List<Vector2Int>();
    }
    public class EnemySpawnPool 
    { 
        public object GetArcher() => null; 
        public object GetTracker() => null; 
    }
    public class DungeonRandom 
    { 
        public bool ShouldSpawnArcher() => true; 
        public bool ShouldSpawnTracker() => true; 
    }
    public struct Spawn 
    { 
        public object Enemy; 
        public Vector2Int Position;
        public Spawn(object enemy, Vector2Int pos) { Enemy = enemy; Position = pos; }
    }
    // ---------------------------------------------------------------------

    public abstract class RoomSpawnRule
    {
        public abstract IEnumerable<Spawn> Generate(RoomNode room, EnemySpawnPool pool, DungeonRandom rng, int graphDistanceFromSafe);

        protected bool IsTooCloseToEntrance(Vector2Int pos, RoomNode room)
        {
            foreach (var entrancePos in room.Entrance)
            {
                if (Vector2Int.Distance(pos, entrancePos) <= 1.5f) return true;
            }
            return false;
        }

        protected bool HasStraightLOS(Vector2Int pos, RoomNode room)
        {
            int requiredClearance = 3;
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions)
            {
                if (CheckClearance(pos, dir, requiredClearance, room)) return true;
            }
            return false;
        }

        private bool CheckClearance(Vector2Int startPos, Vector2Int direction, int distance, RoomNode room)
        {
            for (int i = 1; i <= distance; i++)
            {
                Vector2Int nextPos = startPos + (direction * i);
                if (!room.IsWalkable(nextPos)) return false;
            }
            return true;   
        }
    }

    public sealed class SafeRoomSpawnRule : RoomSpawnRule
    {
        public override IEnumerable<Spawn> Generate(RoomNode room, EnemySpawnPool pool, DungeonRandom rng, int graphDistanceFromSafe)
        {
            return Enumerable.Empty<Spawn>();
        }
    }

    public sealed class NormalRoomSpawnRule : RoomSpawnRule
    {
        public override IEnumerable<Spawn> Generate(RoomNode room, EnemySpawnPool pool, DungeonRandom rng, int graphDistanceFromSafe)
        {
            List<Spawn> spawns = new List<Spawn>();
            var candidatePositions = room.GetEmptyTiles(); 

            var validPositions = candidatePositions
                .Where(pos => !IsTooCloseToEntrance(pos, room))
                .ToList();

            foreach (var pos in validPositions)
            {
                // 기획 조건: 궁수는 직선 시야 확보 시 스폰, Tracker는 입구만 아니면 스폰
                if (rng.ShouldSpawnArcher() && HasStraightLOS(pos, room))
                {
                    spawns.Add(new Spawn(pool.GetArcher(), pos));
                }
                else if (rng.ShouldSpawnTracker())
                {
                    spawns.Add(new Spawn(pool.GetTracker(), pos));
                }
            }
            return spawns;
        }
    }

    public sealed class ExitRoomSpawnRule : RoomSpawnRule
    {
        public override IEnumerable<Spawn> Generate(RoomNode room, EnemySpawnPool pool, DungeonRandom rng, int graphDistanceFromSafe)
        {
            // TODO: 강한 적 + Elite 스폰 로직 추가
            return Enumerable.Empty<Spawn>();
        }
    }

    public sealed class BossRoomSpawnRule : RoomSpawnRule
    {
        public override IEnumerable<Spawn> Generate(RoomNode room, EnemySpawnPool pool, DungeonRandom rng, int graphDistanceFromSafe)
        {
            // TODO: 보스 1체 스폰 로직 추가
            return Enumerable.Empty<Spawn>();
        }
    }
} 
