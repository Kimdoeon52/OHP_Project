using System;
using UnityEngine;

/// <summary>
/// 플레이어 근접 전투를 처리합니다.
/// 투척 무기는 YJW 아이템 시스템에서 처리하므로 여기서는 근접 공격만 담당합니다.
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
    /// 마우스 클릭 위치를 받아 상하좌우 중 가장 가까운 한 칸을 근접 공격합니다.
    /// 적중 시에만 피해와 행동 지연을 적용합니다.
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
            int damage = GetModifiedOutgoingDamage(RollMeleeDamage());
            int beforeHp = damageable.CurrentHp;
            damageable.TakeDamage(damage);
            LogDamageDealt("Melee", damageable, damage, beforeHp);
            StartMeleeCooldown();
            OnAttackPerformed?.Invoke(dir);
            float actionDelay = GetModifiedMeleeTimeCost();
            _player.ActionDelay?.StartDelay(actionDelay);
            CompletePlayerAction(actionDelay);
        }
    }

    private void CompletePlayerAction(float actionDelay)
    {
        if (_player.Movement != null && _player.Movement.IgnoresActionDelayForMovement)
            TurnManager.Instance.OnPlayerActionCompletedWithoutEnemyTurns(actionDelay);
        else
            TurnManager.Instance.OnPlayerActionCompleted(actionDelay);
    }

    private int GetModifiedOutgoingDamage(int baseDamage)
    {
        StatusController statusController = _player.GetComponent<StatusController>();
        return statusController != null ? statusController.ModifyOutgoingDamage(baseDamage) : baseDamage;
    }

    private int RollMeleeDamage()
    {
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;

        if (equipmentController == null)
            equipmentController = FindFirstObjectByType<NYHEquipmentController>();

        return NYHPlayerAttackCalculator.RollMeleeDamage(_stats, equipmentController);
    }

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
        return statusController != null ? statusController.ModifyAttackCooldown(cooldown) : cooldown;
    }

    private float GetModifiedMeleeTimeCost()
    {
        float timeCost = Mathf.Max(minMeleeTimeCost, _player.Config.meleeTimeDealy / GetAttackSpeedFactor());
        StatusController statusController = _player.GetComponent<StatusController>();
        return statusController != null ? statusController.ModifyAttackCooldown(timeCost) : timeCost;
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
