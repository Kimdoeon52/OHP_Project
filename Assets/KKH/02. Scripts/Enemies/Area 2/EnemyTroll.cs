using UnityEngine;

/// <summary>
/// 트롤 (EnemyTroll) [Elite] — Area 2 (던전, 6~10층)
///
/// 행동 패턴:
///   1. 이동: 느린 1칸 추적 (moveSpeed = 0.75, EnemyBase.TryStepTowardPlayer 사용)
///   2. 공격: 강타, 공격 비용 1.5초 (attackSpeed ≈ 0.67)
///   3. 특수: 강타 명중 시 Paralysis(행동 불능) 상태이상 1턴 부여
///
///   모든 수치(moveSpeed, attackSpeed)와 Paralysis 상태이상은
///   EnemyData SO 인스펙터에서 기획자가 직접 설정 가능.
///   EnemyBase 공통 로직(AP 누적, TryApplyOnHitEffect)이 자동 구동하므로 추가 C# 불필요.
/// </summary>
public class EnemyTroll : EnemyBase
{
    // 추가 override 없음 — 순수 데이터 기반 구현체.
}
