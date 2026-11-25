using UnityEngine;

// 일반 스켈레톤 (EnemySkeleton) — 행동 개시 시 50% 확률로 방패 가드 활성화
public class EnemySkeleton : EnemyBase
{
    private bool _shieldRaised = false;

    protected override void PerformMovement()
    {
        if (data != null && data.hasShield && !IsDead)
        {
            if (GetRng().Chance(0.5f))
                _shieldRaised = true;
        }

        base.PerformMovement();
    }

    public override void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        if (_shieldRaised)
        {
            int shieldReduction = GetRng().Range(0, 3);
            int rawDamageAfterShield = Mathf.Max(0, amount - shieldReduction);
            _shieldRaised = false;
            base.TakeDamage(rawDamageAfterShield);
            return;
        }

        base.TakeDamage(amount);
    }
}
