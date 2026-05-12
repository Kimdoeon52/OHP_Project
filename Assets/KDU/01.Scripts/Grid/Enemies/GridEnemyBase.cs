using UnityEngine;

/// <summary>
/// [폐기됨] 이 클래스는 EnemyBase로 통합되었습니다.
/// 모든 적은 EnemyBase를 상속하세요.
/// 
/// 이 파일은 기존 프리팹 참조가 깨지지 않도록 당분간 유지합니다.
/// 새 코드에서는 절대 사용하지 마세요.
/// </summary>
[System.Obsolete("GridEnemyBase는 EnemyBase로 통합되었습니다. EnemyBase를 사용하세요.")]
public abstract class GridEnemyBase : EnemyBase
{
    // EnemyBase로 완전 이전됨 — 이 클래스는 빈 래퍼.
    // 기존 프리팹이 이 타입을 참조할 수 있으므로 삭제하지 않고 유지.
    // 추후 모든 프리팹 교체 완료 시 삭제할 것.
}
