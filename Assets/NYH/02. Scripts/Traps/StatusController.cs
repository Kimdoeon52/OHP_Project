using System.Collections.Generic;
using UnityEngine;

public class StatusController : MonoBehaviour
{
    private readonly List<ActiveStatus> _statuses = new();
    private IDamageable _damageable;

    public bool IsCrippled => HasStatus(StatusType.Cripple);
    public bool IsBlind => HasStatus(StatusType.Blind);
    public bool IsWeak => HasStatus(StatusType.Weakness);
    public bool IsChilled => HasStatus(StatusType.Chill);
    public bool IsParalyzed => HasStatus(StatusType.Paralysis);

    private void Awake()
    {
        _damageable = GetComponent<IDamageable>();
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerActionCompleted += OnPlayerActionCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerActionCompleted -= OnPlayerActionCompleted;
    }

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

    public bool HasStatus(StatusType type)
    {
        for (int i = 0; i < _statuses.Count; i++)
        {
            if (_statuses[i].type == type && _statuses[i].remainingTurns > 0)
                return true;
        }

        return false;
    }

    public float ModifyMoveTimeCost(float baseCost)
    {
        float result = baseCost;
        if (IsCrippled) result *= 1.5f;
        if (IsChilled) result *= 1.25f;
        if (IsParalyzed) result *= 2f;
        return result;
    }

    public int ModifyOutgoingDamage(int baseDamage)
    {
        if (baseDamage <= 0) return 0;
        if (IsWeak) return Mathf.Max(1, Mathf.CeilToInt(baseDamage * 0.5f));
        return baseDamage;
    }

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
