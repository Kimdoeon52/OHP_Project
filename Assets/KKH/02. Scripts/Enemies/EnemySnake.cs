using UnityEngine;

/// <summary>
/// 뱀 (EnemySnake) [Elite] — Area 1 (하수구, 1~5층)
///
/// 행동 패턴:
///   1. 이동: 기본 1칸 추적 (EnemyBase.TryStepTowardPlayer 사용)
///   2. 공격: 독니 물기, 공격 비용 1.5초 (attackSpeed = 0.67 → 보통 몬스터보다 딜레이 큼)
///   3. 특수: 명중 시 강한 독 (3초간 매초 2~4 데미지)
///
///   공격 비용(attackSpeed)과 독 상태이상은 EnemyData SO 인스펙터에서 전부 설정 가능.
///   EnemyBase AP 누적기 및 TryApplyOnHitEffect가 자동 구동.
/// </summary>
public class EnemySnake : EnemyBase
{
    // 추가 override 없음 — 순수 데이터 기반 구현체.
}
