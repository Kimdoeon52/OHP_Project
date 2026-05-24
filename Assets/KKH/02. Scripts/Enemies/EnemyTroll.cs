using UnityEngine;

/// <summary>
/// 트롤 [Elite] — Area 2 (던전, 6~10층)
/// 
/// 이동: 기본 추적, moveSpeed=0.75
/// 공격: 강타 (1.5초 비용, 인접 1×1)
/// 특수: 강타 명중 시 1초 기절 (Stun) — StatusEffect(Paralysis) 사용
/// 
/// EnemyBase의 기본 OnTurnUpdate 템플릿을 사용합니다.
/// 공격 속도(1.5초 비용 = attackSpeed 0.67)와 상태이상 효과(Paralysis)는 
/// ScriptableObject(EnemyData) 설정을 기반으로 EnemyBase 코어에서 자동으로 처리됩니다.
/// </summary>
public class EnemyTroll : EnemyBase
{
    // 추가적인 오버라이드가 필요 없는 간결한 구현체입니다.
}
