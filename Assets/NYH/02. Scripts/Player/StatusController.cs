using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 독, 출혈, 화상, 둔화, 약화 같은 임시 상태이상을 관리한다.
/// 영구 스탯은 NYHPlayerStats, 레벨/경험치는 PlayerLevelSystem이 담당하고,
/// 이 클래스는 "몇 턴 동안 적용되는 효과"만 보관하고 처리한다.
/// </summary>
public class StatusController : MonoBehaviour
{
    // 현재 이 오브젝트에 걸려 있는 상태 목록.
    // 같은 StatusType이 다시 들어오면 새로 추가하지 않고 지속시간/위력만 더 강한 값으로 갱신한다.
    private readonly List<ActiveStatus> _statuses = new();

    // 독/화상/출혈처럼 턴마다 피해를 주는 상태에서 피해를 적용할 대상.
    // 플레이어와 적 모두 IDamageable을 구현하면 이 컨트롤러를 재사용할 수 있다.
    private IDamageable _damageable;

    /// <summary>이동 시간 증가 상태. GridPlayerMovement에서 이동 비용 계산 시 사용한다.</summary>
    public bool IsCrippled => HasStatus(StatusType.Cripple);

    /// <summary>실명 상태. 공격 명중률/시야 패널티 등으로 확장할 수 있다.</summary>
    public bool IsBlind => HasStatus(StatusType.Blind);

    /// <summary>약화 상태. GridPlayerCombat에서 주는 피해를 감소시키는 데 사용한다.</summary>
    public bool IsWeak => HasStatus(StatusType.Weakness);

    /// <summary>냉기 상태. 이동 시간 증가 효과로 사용한다.</summary>
    public bool IsChilled => HasStatus(StatusType.Chill);

    /// <summary>마비 상태. 현재는 이동 시간 크게 증가로 처리한다.</summary>
    public bool IsParalyzed => HasStatus(StatusType.Paralysis);

    /// <summary>
    /// 피해 적용이 필요한 상태를 위해 IDamageable 참조를 캐싱한다.
    /// </summary>
    private void Awake()
    {
        _damageable = GetComponent<IDamageable>();
    }

    /// <summary>
    /// 플레이어가 행동을 완료할 때마다 상태 지속시간과 턴 피해를 처리한다.
    /// </summary>
    private void OnEnable()
    {
        GameEvents.OnPlayerActionCompleted += OnPlayerActionCompleted;
    }

    /// <summary>
    /// 비활성화 시 이벤트 구독을 해제해 중복 처리와 누수를 막는다.
    /// </summary>
    private void OnDisable()
    {
        GameEvents.OnPlayerActionCompleted -= OnPlayerActionCompleted;
    }

    /// <summary>
    /// 상태이상을 적용한다.
    /// 이미 같은 종류의 상태가 있다면 지속시간과 위력 중 더 큰 값을 유지한다.
    /// </summary>
    public void ApplyStatus(StatusRequest request)
    {
        if (request.DurationTurns <= 0 && request.Power <= 0) return;

        for (int i = 0; i < _statuses.Count; i++)
        {
            ActiveStatus current = _statuses[i];
            if (current.type != request.StatusType) continue;

            current.remainingTurns = Mathf.Max(current.remainingTurns, Mathf.Max(1, request.DurationTurns));
            current.power = Mathf.Max(current.power, Mathf.Max(1, request.Power));
            _statuses[i] = current;
            return;
        }

        _statuses.Add(new ActiveStatus(
            request.StatusType,
            Mathf.Max(1, request.DurationTurns),
            Mathf.Max(1, request.Power)));
    }

    /// <summary>
    /// 지정한 상태가 현재 활성화되어 있는지 확인한다.
    /// remainingTurns가 0 이하인 상태는 없는 것으로 취급한다.
    /// </summary>
    public bool HasStatus(StatusType type)
    {
        for (int i = 0; i < _statuses.Count; i++)
        {
            if (_statuses[i].type == type && _statuses[i].remainingTurns > 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 이동 행동의 시간 비용을 상태이상에 따라 보정한다.
    /// GridPlayerMovement.TryMove에서 이동 한 번의 totalCost를 계산할 때 호출한다.
    /// </summary>
    public float ModifyMoveTimeCost(float baseCost)
    {
        float result = baseCost;
        if (IsCrippled) result *= 1.5f;
        if (IsChilled) result *= 1.25f;
        if (IsParalyzed) result *= 2f;
        return result;
    }

    /// <summary>
    /// 플레이어/적이 주는 피해량을 상태이상에 따라 보정한다.
    /// 현재 Weakness는 최종 피해를 절반으로 줄인다.
    /// </summary>
    public int ModifyOutgoingDamage(int baseDamage)
    {
        if (baseDamage <= 0) return 0;
        if (IsWeak) return Mathf.Max(1, Mathf.CeilToInt(baseDamage * 0.5f));
        return baseDamage;
    }

    /// <summary>
    /// 플레이어 행동 완료 시 모든 상태를 한 턴 진행한다.
    /// 먼저 턴 효과를 적용하고, 이후 지속시간을 1 감소시킨다.
    /// </summary>
    private void OnPlayerActionCompleted(float timeCost)
    {
        for (int i = _statuses.Count - 1; i >= 0; i--)
        {
            ActiveStatus status = _statuses[i];
            ApplyTurnEffect(status);

            status.remainingTurns--;
            if (status.type == StatusType.Bleed)
                status.power = Mathf.Max(1, status.power - 1);

            if (status.remainingTurns <= 0)
                _statuses.RemoveAt(i);
            else
                _statuses[i] = status;
        }
    }

    /// <summary>
    /// 턴마다 발동하는 상태 효과를 적용한다.
    /// 독/출혈/화상/부식/점액은 power만큼 피해를 준다.
    /// </summary>
    private void ApplyTurnEffect(ActiveStatus status)
    {
        if (_damageable == null || _damageable.IsDead) return;

        switch (status.type)
        {
            case StatusType.Poison:
            case StatusType.Bleed:
            case StatusType.Burn:
            case StatusType.Corrosion:
            case StatusType.Ooze:
                _damageable.TakeDamage(Mathf.Max(1, status.power));
                break;
        }
    }

    /// <summary>
    /// 런타임 중 활성화된 상태 하나의 데이터.
    /// type은 상태 종류, remainingTurns는 남은 행동 턴 수, power는 피해량/강도 값이다.
    /// </summary>
    private struct ActiveStatus
    {
        public StatusType type;
        public int remainingTurns;
        public int power;

        public ActiveStatus(StatusType type, int remainingTurns, int power)
        {
            this.type = type;
            this.remainingTurns = remainingTurns;
            this.power = power;
        }
    }
}
