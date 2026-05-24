using UnityEngine;

/// <summary>
/// 뱀 [Elite] — Area 1 (하수구, 1~5층)
/// 
/// 이동: 기본 추적 (EnemyBase 기본 TryStepTowardPlayer 사용)
/// 공격: 독니 물기 (1.5초 공격 비용)
/// 특수: 명중 시 독 (3초간 2~4 데미지) — EnemyData.onHitEffect SO 연결
/// 
/// EnemyBase의 기본 OnTurnUpdate 템플릿을 그대로 사용합니다.
/// 공격 비용과 독 효과는 ScriptableObject(EnemyData) 설정을 기반으로 
/// EnemyBase 코어에서 자동으로 처리됩니다.
/// </summary>
public class EnemySnake : EnemyBase
{
    // 추가적인 오버라이드가 필요 없는 간결한 구현체입니다.
    // 템플릿 메서드에 의해 이동, 공격, 텔레그래프가 구동됩니다.
}
