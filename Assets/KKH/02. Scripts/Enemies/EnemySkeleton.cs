using UnityEngine;

/// <summary>
/// 스켈레톤 (방패 X/O) [Common] — Area 2 (던전, 6~10층)
/// 
/// 이동: 기본 추적, moveSpeed=0.75
/// 공격: 근접 1칸
/// 특수(방패O): 50% 확률로 방패 들기 → 다음 피격 0~2 추가 감소 (일회성)
/// 
/// 방패 소지 여부는 EnemyData의 hasShield 플래그로 결정됩니다.
/// </summary>
public class EnemySkeleton : EnemyBase
{
    private bool _shieldRaised = false;

    public override void OnTurnUpdate()
    {
        // 턴 시작 시 방패를 들고 있고 죽지 않았다면 50% 확률로 방패 들기
        if (data != null && data.hasShield && !IsDead)
        {
            if (Random.value <= 0.5f)
            {
                _shieldRaised = true;
            }
        }

        base.OnTurnUpdate();
    }

    public override void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        if (_shieldRaised)
        {
            int shieldReduction = Random.Range(0, 3); // 0~2 감소
            int rawDamageAfterShield = Mathf.Max(0, amount - shieldReduction);
            
            Debug.Log($"{name}의 방패가 피해를 {shieldReduction} 감소시켰습니다. (남은 피해: {rawDamageAfterShield})");
            
            _shieldRaised = false; // 일회성 차감 후 해제
            base.TakeDamage(rawDamageAfterShield);
        }
        else
        {
            base.TakeDamage(amount);
        }
    }
}
