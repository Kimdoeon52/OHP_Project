using UnityEngine;

/// <summary>
/// 추적자: 시야 안에 플레이어 들어오면 1칸씩 이동, 인접 시 다음 턴 공격 의도 표시.
/// 
/// EnemyBase의 기본 템플릿 메서드를 그대로 사용.
/// (ExecutePendingAttack → PerformMovement → CheckAttackTelegraph)
/// 별도 override 불필요 — 기본 동작이 정확히 Tracker 사양.
/// </summary>
public class GridEnemyTracker : EnemyBase
{
    // EnemyBase의 기본 OnTurnUpdate() 템플릿 메서드가
    // Tracker의 기존 로직과 동일하므로 override 필요 없음.
    // TryStepTowardPlayer(), TryStep() 도 EnemyBase로 이동 완료.
}
