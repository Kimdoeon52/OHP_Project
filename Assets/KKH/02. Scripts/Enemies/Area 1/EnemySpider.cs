using System.Collections;
using UnityEngine;

// 거미 (EnemySpider) — 명중 시 독(EnemyData) + 이속 감소(Cripple) 2중 상태이상 부여
public class EnemySpider : EnemyBase
{
    [Header("거미 추가 디버프 설정")]
    [Tooltip("이속 감소 상태이상 데이터")]
    [SerializeField] private StatusEffectData slowDebuffData;

    protected override IEnumerator ExecutePendingAttack(Vector2Int targetCell, float delaySeconds)
    {
        _isAttacking = true;
        ShowTelegraph(targetCell);

        yield return WaitGameSeconds(delaySeconds);

        if (!IsDead)
        {
            UpdateSpriteDirection(targetCell);
            if (_animator != null)
                _animator.SetTrigger("Attack");

            yield return WaitGameSeconds(0.25f);

            if (!IsDead && Player.GridPosition == targetCell)
            {
                int damage = RollAttackDamage();
                if (TryHitPlayer(damage))
                    TryApplySlowDebuff(Player);
            }
        }

        ClearTelegraphs();
        _attackActionPoints = 0f;
        _isAttacking = false;
        _pendingAttackCell = null;
    }

    private void TryApplySlowDebuff(IDamageable target)
    {
        if (slowDebuffData == null)
            return;

        if (target == null || target.IsDead)
            return;

        if (target is not Component component)
            return;

        var request = new StatusRequest(target as IGridEntity, (target as IGridEntity)?.GridPosition ?? GridPosition, slowDebuffData.statusType, slowDebuffData.durationTurns, slowDebuffData.power);

        StatusController controller = component.GetOrAddComponent<StatusController>();
        controller.ApplyStatus(request);
        GameEvents.OnStatusEffectApplied?.Invoke(request);
    }
}
