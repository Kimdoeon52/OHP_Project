using System;
using UnityEngine;

/// <summary>
/// 플레이어의 근접 전투 판정, 데미지 산출, 쿨다운(행동 지연 비용)을 전담하여 관리하는 클래스입니다.
/// 마우스 입력 방향의 인접 칸에 적(IDamageable)이 존재하는지 확인하고,
/// 스탯(NYHPlayerStats)과 장비(NYHEquipmentController)를 참조하여 산출된 데미지를 적중시킵니다.
/// 공격이 성립되면 TurnManager를 호출해 턴 시간 비용을 지불하게 됩니다.
/// (원거리/투척은 아이템 사용 로직에서 별도로 처리되므로 제외됩니다.)
/// </summary>
public class GridPlayerCombat : MonoBehaviour
{
    /// <summary>
    /// 공격이 실제로 성공했을 때 호출합니다.
    /// 애니메이션 컨트롤러가 이 이벤트를 받아 공격 모션을 재생합니다.
    /// </summary>
    public event Action<Vector2Int> OnAttackPerformed;

    [Header("Equipment")]
    [Tooltip("현재 장착 무기를 읽어 근접 피해를 계산하는 장비 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private NYHEquipmentController equipmentController;

    [Header("Attack Speed")]
    [Tooltip("GDD 기준 근접 공격 기본 쿨다운입니다. AttackSpeed 1일 때 이 값을 그대로 사용합니다.")]
    [SerializeField, Min(0.001f)] private float baseMeleeCooldown = 1.0f;

    [Tooltip("AttackSpeed가 1보다 높을 때 1 증가당 근접 공격 쿨다운과 행동 지연을 줄이는 비율입니다.")]
    [SerializeField, Min(0.0001f)] private float attackSpeedCooldownMultiplier = 0.12f;

    [Tooltip("AttackSpeed가 높아져도 근접 공격 쿨다운이 이 값보다 낮아지지 않게 막습니다.")]
    [SerializeField, Min(0.0001f)] private float minMeleeCooldown = 0.0001f;

    [Tooltip("AttackSpeed가 높아져도 근접 공격 시간 비용이 이 값보다 낮아지지 않게 막습니다.")]
    [SerializeField, Min(0.01f)] private float minMeleeTimeCost = 0.1f;

    [Header("Debug")]
    [Tooltip("켜면 플레이어가 공격으로 준 피해량을 Console에 출력합니다.")]
    [SerializeField] private bool logDamageDealt = true;

    private GridPlayer _player;
    private NYHPlayerStats _stats;

    // 공격 지연 시간의 마지막 보정 통로입니다.
    // AttackSpeed와 상태이상 계산이 끝난 뒤, 타격의 반지처럼 "공격 쿨다운 -0.05초" 효과를 여기에 태웁니다.
    private NYHFinalStatModifierController _finalStatModifiers;
    private float _nextMeleeAttackTime;

    /// <summary>
    /// GridPlayer에서 호출하는 초기화 진입점입니다.
    /// YJW 장비 시스템을 읽어 근접 피해 계산에 사용합니다.
    /// </summary>
    public void Init(GridPlayer player)
    {
        _player = player;
        _stats = GetComponent<NYHPlayerStats>();
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;

        if (equipmentController == null)
            equipmentController = FindFirstObjectByType<NYHEquipmentController>();
    }

    /// <summary>
    /// 입력받은 타겟 좌표(clickedCell)를 기준으로 플레이어의 4방향 인접 칸 중 한 곳을 특정하여 근접 공격을 시도합니다.
    /// 쿨다운, 상태이상 제약 등을 통과하고 대상 칸에 타격 가능한 엔티티가 있을 때만 최종 공격이 실행됩니다.
    /// </summary>
    public void TryMeleeAt(Vector2Int clickedCell)
    {
        if (_player.ActionDelay != null && !_player.ActionDelay.CanAct)
            return;

        if (!CanMeleeAttack())
            return;

        Vector2Int diff = clickedCell - _player.GridPosition;
        if (diff == Vector2Int.zero) return;

        Vector2Int dir = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y)
            ? new Vector2Int(Math.Sign(diff.x), 0)
            : new Vector2Int(0, Math.Sign(diff.y));

        if (dir == Vector2Int.zero) return;

        _player.Movement.SetFacing(dir);

        Vector2Int target = _player.GridPosition + dir;
        IGridEntity entity = TurnManager.Instance.GetEntityAt(target);
        if (entity is IDamageable damageable && (UnityEngine.Object)damageable != _player)
        {
            MagicData weaponMagic = GetEquippedMagic(EquipmentSlot.Weapon);
            EnemyBase enemyTarget = entity as EnemyBase;
            BossBase bossTarget = entity as BossBase;

            bool hitGuaranteed = weaponMagic != null && weaponMagic.IsHitGuaranteed;
            int targetEvasion = 0;
            if (enemyTarget != null && enemyTarget.Data != null)
            {
                targetEvasion = enemyTarget.Data.evasion;
            }
            else if (bossTarget != null)
            {
                targetEvasion = bossTarget.Evasion;
            }

            if (!hitGuaranteed && (enemyTarget != null || bossTarget != null) && !RollPlayerHit(targetEvasion))
            {
                // 플레이어 공격도 적/보스의 회피 스탯을 반영합니다.
                weaponMagic?.OnMiss();
                PublishCombatText(GetCombatWorldPosition(damageable), "Miss", 0, CombatTextType.Miss);
                FinishMeleeAttack(dir);
                return;
            }

            NYHAttackDamageRange damageRange = GetMeleeDamageRange();
            int damage = GetModifiedOutgoingDamage(RollMeleeDamage(damageRange));
            weaponMagic?.OnAttack(ref damage, enemyTarget, damageRange.MaxDamage);
            int beforeHp = damageable.CurrentHp;
            CombatTextCriticalContext.SetCurrentMaxDamage(damageRange.MaxDamage);
            try
            {
                damageable.TakeDamage(damage);
            }
            finally
            {
                CombatTextCriticalContext.Clear();
            }
            int finalDamage = Mathf.Max(0, beforeHp - damageable.CurrentHp);

            if (damageable.CurrentHp <= 0)
                weaponMagic?.OnKill();

            // BossBase는 BossTakeDamage 내부에서도 표시 이벤트를 쏘므로 여기서 중복 표시하지 않습니다.
            if (finalDamage > 0 && damageable is not BossBase)
            {
                CombatTextType textType = IsCriticalDamage(finalDamage, damageRange.MaxDamage)
                    ? CombatTextType.CriticalDamage
                    : CombatTextType.Damage;

                PublishCombatText(GetCombatWorldPosition(damageable), finalDamage.ToString(), finalDamage, textType);
            }

            LogDamageDealt("Melee", damageable, damage, beforeHp);
            FinishMeleeAttack(dir);
        }
    }

    private void FinishMeleeAttack(Vector2Int dir)
    {
        StartMeleeCooldown();
        OnAttackPerformed?.Invoke(dir);
        float actionDelay = GetModifiedMeleeTimeCost();
        _player.ActionDelay?.StartDelay(actionDelay);
        CompletePlayerAction(actionDelay);
    }

    /// <summary>
    /// 공격 행동이 완료되었을 때 TurnManager에 행동 완료 시그널을 보내 적들의 턴(AI)을 진행시킵니다.
    /// 행동 딜레이 무시 상태가 설정되어 있다면 적 턴 진행 없이 시간만 소모합니다.
    /// </summary>
    private void CompletePlayerAction(float actionDelay)
    {
        if (_player.Movement != null && _player.Movement.IgnoresActionDelayForMovement)
            TurnManager.Instance.OnPlayerActionCompletedWithoutEnemyTurns(actionDelay);
        else
            TurnManager.Instance.OnPlayerActionCompleted(actionDelay);
    }

    /// <summary>
    /// 계산된 기본 피해량에 상태이상 컨트롤러(독, 약화 등)의 보정치를 합산하여 최종 가해 피해량을 반환합니다.
    /// </summary>
    private int GetModifiedOutgoingDamage(int baseDamage)
    {
        StatusController statusController = _player.GetComponent<StatusController>();
        return statusController != null ? statusController.ModifyOutgoingDamage(baseDamage) : baseDamage;
    }

    /// <summary>
    /// 장비 컨트롤러와 플레이어 기본 스탯을 기반으로 무기의 굴림 피해(Roll)를 계산합니다.
    /// 공격력의 최소~최대 범위에서 난수를 생성하는 역할은 내부의 NYHPlayerAttackCalculator가 담당합니다.
    /// </summary>
    private NYHAttackDamageRange GetMeleeDamageRange()
    {
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;

        if (equipmentController == null)
            equipmentController = FindFirstObjectByType<NYHEquipmentController>();

        _finalStatModifiers = GetComponent<NYHFinalStatModifierController>();
        if (_finalStatModifiers == null)
        {
            // 타격의 반지 같은 최종 공격 지연 보정이 공격 쿨다운과 행동 비용에 같이 적용되도록 준비한다.
            _finalStatModifiers = gameObject.AddComponent<NYHFinalStatModifierController>();
        }

        return NYHPlayerAttackCalculator.CalculateMeleeRange(_stats, equipmentController);
    }

    private int RollMeleeDamage(NYHAttackDamageRange range)
    {
        if (range.MinDamage == range.MaxDamage)
            return range.MinDamage;

        // UnityEngine.Random.Range(int, int)는 maxExclusive라서 +1 한다.
        return UnityEngine.Random.Range(range.MinDamage, range.MaxDamage + 1);
    }

    private bool RollPlayerHit(int targetEvasion)
    {
        int attackerAccuracy = _stats != null ? _stats.Accuracy : 0;
        int missChancePercent = Mathf.Clamp(targetEvasion - attackerAccuracy, 0, 50);
        return UnityEngine.Random.Range(0, 100) >= missChancePercent;
    }

    private static bool IsCriticalDamage(int finalDamage, int maxPossibleDamage)
    {
        if (finalDamage <= 0 || maxPossibleDamage <= 0)
            return false;

        return finalDamage >= Mathf.CeilToInt(maxPossibleDamage * 0.8f);
    }

    private static Vector3 GetCombatWorldPosition(IDamageable target)
    {
        return target is Component component ? component.transform.position : Vector3.zero;
    }

    private static void PublishCombatText(Vector3 worldPosition, string text, int damage, CombatTextType type)
    {
        GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(worldPosition, text, damage, type));
    }

    /// <summary>
    /// 현재 시간이 계산된 다음 공격 가능 시간(_nextMeleeAttackTime)을 지났는지 판별하여 공격 쿨다운 통과 여부를 반환합니다.
    /// </summary>
    private bool CanMeleeAttack()
    {
        return Time.time >= _nextMeleeAttackTime;
    }

    /// <summary>
    /// AttackSpeed 스탯을 반영해 다음 근접 공격 가능 시각을 계산합니다.
    /// 값이 높을수록 쿨다운이 짧아지지만 minMeleeCooldown 아래로는 내려가지 않습니다.
    /// </summary>
    private void StartMeleeCooldown()
    {
        _nextMeleeAttackTime = Time.time + GetMeleeCooldown();
    }

    private float GetMeleeCooldown()
    {
        float cooldown = Mathf.Max(minMeleeCooldown, baseMeleeCooldown / GetAttackSpeedFactor());
        StatusController statusController = _player.GetComponent<StatusController>();
        if (statusController != null)
            cooldown = statusController.ModifyAttackCooldown(cooldown);

        // 최종 초 단위 보정은 AttackSpeed/상태이상 계산 뒤에 적용한다.
        // 타격의 반지 강화 효과는 "다음 공격 가능 시간"을 줄이는 효과라서 공격 쿨다운에 들어간다.
        return _finalStatModifiers != null ? _finalStatModifiers.ModifyAttackActionDelay(cooldown) : cooldown;
    }

    private float GetModifiedMeleeTimeCost()
    {
        float timeCost = Mathf.Max(minMeleeTimeCost, _player.Config.meleeTimeDealy / GetAttackSpeedFactor());
        StatusController statusController = _player.GetComponent<StatusController>();
        if (statusController != null)
            timeCost = statusController.ModifyAttackCooldown(timeCost);

        // 공격 행동 자체가 턴 시스템에 지불하는 시간 비용도 같은 최종 보정을 받는다.
        // 그래서 타격의 반지는 "공격 쿨다운"과 "공격 행동 시간 비용" 양쪽을 동시에 줄인다.
        return _finalStatModifiers != null ? _finalStatModifiers.ModifyAttackActionDelay(timeCost) : timeCost;
    }

    public float GetMeleeActionInterval()
    {
        return Mathf.Max(GetMeleeCooldown(), GetModifiedMeleeTimeCost());
    }

    private float GetAttackSpeedFactor()
    {
        int attackSpeed = GetAttackSpeed();
        return 1f + Mathf.Max(0, attackSpeed - 1) * attackSpeedCooldownMultiplier;
    }

    private int GetAttackSpeed()
    {
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;

        return _stats != null ? _stats.AttackSpeed : 0;
    }

    // 장착 장비의 MagicData를 가져옴
    private MagicData GetEquippedMagic(EquipmentSlot slot)
    {
        if (equipmentController == null)
            equipmentController = FindFirstObjectByType<NYHEquipmentController>();
        if (equipmentController == null || !equipmentController.TryGetEquipped(slot, out NYHEquipmentItemInstance item))
            return null;
        return item.SourceInstance is EquipmentInstance inst ? inst.magic : null;
    }

    /// <summary>
    /// 요청 피해량과 실제 HP 감소량을 비교해 방어/회피 처리 확인용 로그로 남깁니다.
    /// </summary>
    private void LogDamageDealt(string attackName, IDamageable target, int requestedDamage, int beforeHp)
    {
        if (!logDamageDealt || target == null) return;

        int hpDelta = Mathf.Max(0, beforeHp - target.CurrentHp);
        string targetName = target is UnityEngine.Object targetObject ? targetObject.name : target.GetType().Name;

        Debug.Log($"[PlayerAttack] {attackName} -> {targetName}, requested {requestedDamage}, hp delta {hpDelta}, HP {target.CurrentHp}/{target.MaxHp}");
    }
}
