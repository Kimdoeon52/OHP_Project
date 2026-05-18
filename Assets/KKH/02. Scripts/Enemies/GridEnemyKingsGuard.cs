using UnityEngine;

/// <summary>
/// 근위병: 2턴에 1번만 행동, 피해 감소 (방어력 1 고정 감소).
/// TryStepTowardPlayer/TryStep은 이제 EnemyBase에서 상속.
/// </summary>
public class GridEnemyKingsGuard : EnemyBase
{
    private int _turnCounter = 0;

    public new void TakeDamage(int amount)
    {
        int reducedDamage = Mathf.Max(1, amount - 1);
        base.TakeDamage(reducedDamage);
    }

    public override void OnTurnUpdate()
    {
        if (IsDead || Player == null) return;

        _turnCounter++;

        // 1. 이전 턴 공격 의도 발동
        ExecutePendingAttack();

        // 핵심: 홀수 턴에는 이동/공격 예고를 하지 않고 쉰다 (2턴에 1번 행동)
        if (_turnCounter % 2 != 0)
        {
            return;
        }

        // 2. 시야 안이면 플레이어 방향으로 1칸 이동
        PerformMovement();

        // 3. 인접 시 다음 턴 공격 의도 표시
        CheckAttackTelegraph();
    }
}
