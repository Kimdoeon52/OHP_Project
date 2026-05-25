using UnityEngine;

/// <summary>
/// 거미 [Common] — Area 1 (하수구, 1~5층)
/// 
/// 이동: 기본 추적 (EnemyBase 기본 TryStepTowardPlayer)
/// 공격: 근접 1칸, 텔레그래프
/// 특수: 명중 시 독(5초) + 이동속도 -0.3 디버프(2초)
/// 
/// 독은 EnemyData.onHitEffect로 적용.
/// 이동속도 감소는 별도 Cripple 상태이상으로 추가 적용.
/// 
/// 난이도: ★★☆ (StatusEffect 중첩 필요)
/// </summary>
public class EnemySpider : EnemyBase
{
    [Header("거미 추가 디버프 설정")]
    [Tooltip("이동속도 감소 상태이상 SO (Cripple 등)")]
    [SerializeField] private StatusEffectData slowDebuffData;

    /// <summary>
    /// 공격 발동 시 독 + 이동속도 감소 중첩 적용.
    /// 독은 EnemyBase의 TryApplyOnHitEffect (onHitEffect SO)로 처리.
    /// 이동속도 감소는 여기서 추가 적용.
    /// </summary>
    protected override void ExecutePendingAttack()
    {
        if (_pendingAttackCell.HasValue && Player.GridPosition == _pendingAttackCell.Value)
        {
            int damage = RollAttackDamage();
            Player.TakeDamage(damage);

            // 1. 독 적용 (onHitEffect SO — EnemyBase에서 처리)
            TryApplyOnHitEffect(Player);

            // 2. 이동속도 감소 추가 적용 (Cripple)
            TryApplySlowDebuff(Player);
        }
        _pendingAttackCell = null;
        ClearTelegraphs();
    }

    private void TryApplySlowDebuff(IDamageable target)
    {
        if (slowDebuffData == null) return;
        if (target == null || target.IsDead) return;
        if (target is not Component component) return;

        var request = new StatusRequest(
            target as IGridEntity,
            (target as IGridEntity)?.GridPosition ?? GridPosition,
            slowDebuffData.statusType,
            slowDebuffData.durationTurns,
            slowDebuffData.power,
            null
        );

        StatusController controller = component.GetComponent<StatusController>();
        if (controller == null)
            controller = component.gameObject.AddComponent<StatusController>();
        controller.ApplyStatus(request);

        GameEvents.OnStatusEffectApplied?.Invoke(request);
    }
}
