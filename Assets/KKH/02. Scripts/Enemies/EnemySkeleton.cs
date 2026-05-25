using UnityEngine;

/// <summary>
/// 일반 스켈레톤 (EnemySkeleton) [Common] — Area 2 (던전, 6~10층)
///
/// 핵심 메커니즘:
///   1. 기본 추적 + 근접 1칸 공격 (EnemyBase 기본 템플릿 사용)
///   2. 뼈 방패: data.hasShield == true이면 매 턴 시작 시 50% 확률로 방패 활성화
///      → 다음 피격 1회를 0~2만큼 추가 경감 (마법사 스켈레톤과 메커니즘 동일)
///
/// override 목록:
///   OnTurnUpdate()   → 방패 확률 롤 후 부모 OnTurnUpdate 호출
///   TakeDamage()     → 방패 활성 시 추가 경감
/// </summary>
public class EnemySkeleton : EnemyBase
{
    /// <summary>
    /// 방패 활성화 플래그.
    /// OnTurnUpdate에서 50% 확률로 true, TakeDamage에서 소비 후 false.
    /// </summary>
    private bool _shieldRaised = false;

    /// <summary>
    /// 턴 시작 override.
    /// data.hasShield == true일 때 50% 확률로 _shieldRaised = true 설정.
    /// </summary>
    public override void OnTurnUpdate()
    {
        if (data != null && data.hasShield && !IsDead)
        {
            if (Random.value <= 0.5f)
            {
                _shieldRaised = true;
            }
        }

        base.OnTurnUpdate();
    }

    /// <summary>
    /// 피해 처리 override.
    /// 방패 활성 시 0~2 추가 경감 후 base.TakeDamage 호출.
    /// 방패는 1회 사용 후 즉시 해제.
    /// </summary>
    public override void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        if (_shieldRaised)
        {
            int shieldReduction = Random.Range(0, 3);
            int rawDamageAfterShield = Mathf.Max(0, amount - shieldReduction);
            
            Debug.Log($"{name} 방패 발동: 피해 {shieldReduction} 경감 (남은 피해: {rawDamageAfterShield})");
            
            _shieldRaised = false; // 1회성
            base.TakeDamage(rawDamageAfterShield);
        }
        else
        {
            base.TakeDamage(amount);
        }
    }
}
