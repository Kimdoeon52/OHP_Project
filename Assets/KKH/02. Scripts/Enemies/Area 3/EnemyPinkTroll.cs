using UnityEngine;

public class EnemyPinkTroll : EnemyBase
{
    protected override void PerformMovement()
    {
        if (IsDead) return;

        // 플레이어가 세이프룸에 있으면 추적 불가
        if (SafeRoomTimerGate.PlayerInSafeRoom)
        {
            _pendingAttackCell = null;
            ClearTelegraphs();
            return;
        }

        // 자신과 플레이어의 방 정보 가져오기
        RoomData myRoom = MapGenerator.Instance != null ? MapGenerator.Instance.GetRoomAt(GridPosition) : null;
        RoomData playerRoom = MapGenerator.Instance != null ? MapGenerator.Instance.GetRoomAt(Player.GridPosition) : null;

        // 플레이어와 같은 방에 있는지 확인
        bool isSameRoom = (myRoom != null && playerRoom != null && myRoom == playerRoom);

        // 시야 내에 플레이어가 있고 같은 방에 있을 때만 추적
        if (isSameRoom && SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            TryStepTowardPlayer();
            return;
        }

        // 다른 방에 있거나 시야 밖이면 추적하지 않고 정지 (길찾기 대상 초기화)
        _pendingAttackCell = null;
        ClearTelegraphs();
    }
}
