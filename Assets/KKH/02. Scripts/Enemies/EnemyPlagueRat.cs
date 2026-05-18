using UnityEngine;

/// <summary>
/// 역병 쥐 [Common] — Area 1 (하수구, 1~5층)
/// 
/// 이동: 기본 추적 (EnemyBase 기본 TryStepTowardPlayer)
/// 공격: 근접 1칸, 텔레그래프
/// 특수: 명중 시 독 (5초간 1초당 1 데미지) — EnemyData.onHitEffect SO 연결
/// 
/// EnemyBase의 기본 템플릿 메서드를 그대로 사용.
/// onHitEffect가 SO에 설정되어 있으면 ExecutePendingAttack에서
/// TryApplyOnHitEffect가 자동으로 독을 적용함.
/// 
/// 난이도: ★☆☆ (Tracker 거의 그대로)
/// </summary>
public class EnemyPlagueRat : EnemyBase
{
    // EnemyBase 기본 OnTurnUpdate 템플릿 사용:
    //   ExecutePendingAttack() → RollAttackDamage + TryApplyOnHitEffect(독)
    //   PerformMovement()     → TryStepTowardPlayer
    //   CheckAttackTelegraph()→ 인접 시 텔레그래프
    //
    // 독 효과는 EnemyData SO의 onHitEffect 필드에
    // StatusEffectData(Poison, 5턴, 1파워)를 할당하면 자동 적용.
}
