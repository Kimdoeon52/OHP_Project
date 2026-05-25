using UnityEngine;

/// <summary>
/// 역병 쥐 (EnemyPlagueRat) [Common] — Area 1 (하수구, 1~5층)
///
/// 행동 패턴:
///   1. 이동: 기본 1칸 추적 (EnemyBase.TryStepTowardPlayer 사용)
///   2. 공격: 근접 1칸, 1턴 전 텔레그래프 표시
///   3. 특수: 명중 시 독 상태이상 (5초간 매초 1 데미지)
///
/// 코드가 비어있는 이유:
///   독 효과는 EnemyData SO 인스펙터의 "On Hit Effect" 필드에
///   Poison 데이터(Duration=5, Power=1)를 할당하면 EnemyBase.TryApplyOnHitEffect가 자동 처리.
///   추가 커스텀 AI가 필요할 때만 이 클래스에 override 추가.
/// </summary>
public class EnemyPlagueRat : EnemyBase
{
    // EnemyBase 기본 턴 템플릿 그대로 사용:
    //   ExecutePendingAttack()  → RollAttackDamage + TryApplyOnHitEffect (독)
    //   PerformMovement()       → TryStepTowardPlayer
    //   CheckAttackTelegraph()  → 인접 시 텔레그래프 생성
}
