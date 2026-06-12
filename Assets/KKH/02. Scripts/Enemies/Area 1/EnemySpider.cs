using UnityEngine;
using System.Collections;

/// <summary>
/// 거미 (EnemySpider) [Common] — Area 1 (하수구, 1~5층)
///
/// 핵심 메커니즘:
///   1. 기본 추적 + 근접 1칸 공격 (EnemyBase 기본 템플릿 사용)
///   2. 공격 명중 시 상태이상 2종 동시 발동:
///      - 독(Poison)      : EnemyData.onHitEffect SO 설정 → EnemyBase.TryApplyOnHitEffect 자동 처리
///      - 이속 감소(Cripple): slowDebuffData SO를 직접 읽어 TryApplySlowDebuff로 수동 추가 부여
///
/// override 목록:
///   ExecutePendingAttack()  → 기본 공격 + TryApplySlowDebuff 추가 호출
/// </summary>
public class EnemySpider : EnemyBase
{
    [Header("거미 추가 디버프 설정")]
    [Tooltip("플레이어 적중 시 추가로 부여할 이속 감소(Cripple) 상태이상 데이터. 인스펙터에서 할당.")]
    [SerializeField] private StatusEffectData slowDebuffData;

    /// <summary>
    /// 공격 발동 override.
    /// 명중 성공 시 독(TryHitPlayer 내부 공통 처리) + 이속감소(TryApplySlowDebuff) 순서로 2중 상태이상 적용.
    /// </summary>
    protected override IEnumerator ExecutePendingAttack(Vector2Int targetCell, float delaySeconds)
    {
        ShowTelegraph(targetCell);

        yield return new WaitForSeconds(delaySeconds);

        if (!IsDead)
        {
            UpdateSpriteDirection(targetCell);
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }
            if (Player.GridPosition == targetCell)
            {
                int damage = RollAttackDamage();
                if (TryHitPlayer(damage))
                {
                    // 빗나간 공격에는 고유 디버프도 붙지 않도록 명중 성공 뒤에만 추가 적용합니다.
                    TryApplySlowDebuff(Player);
                }
            }
        }
        ClearTelegraphs();
    }

    /// <summary>
    /// slowDebuffData를 기반으로 Cripple 상태이상을 대상에게 안전하게 부여.
    /// slowDebuffData가 null이거나 대상이 사망이면 아무것도 안 함.
    /// </summary>
    private void TryApplySlowDebuff(IDamageable target)
    {
        if (slowDebuffData == null) return;
        if (target == null || target.IsDead) return;
        if (target is not Component component) return;

        // 상태이상 요청 구축
        var request = new StatusRequest(
            target as IGridEntity,
            (target as IGridEntity)?.GridPosition ?? GridPosition,
            slowDebuffData.statusType,
            slowDebuffData.durationTurns,
            slowDebuffData.power,
            null // trapSource: 적 본인 공격이므로 null
        );

        // StatusController 없으면 자동 부착 후 상태 적용
        StatusController controller = component.GetComponent<StatusController>();
        if (controller == null)
            controller = component.gameObject.AddComponent<StatusController>();
        controller.ApplyStatus(request);

        // 상태이상 발동 전역 이벤트 알림 (UI / 사운드 구독자용)
        GameEvents.OnStatusEffectApplied?.Invoke(request);
    }
}
