using System.Collections.Generic;
using UnityEngine;

public class TrapSystem : MonoBehaviour
{
    public static TrapSystem Instance { get; private set; }

    [SerializeField, Min(1)] private int currentFloor = 1;

    private readonly Dictionary<Vector2Int, GridTrap> _traps = new();
    private readonly List<PendingDelayedAreaEffect> _pendingDelayedAreaEffects = new();

    public int CurrentFloor => currentFloor;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("TrapSystem이 씬에 두 개 이상 있습니다.");
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerActionCompleted += OnPlayerActionCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerActionCompleted -= OnPlayerActionCompleted;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetCurrentFloor(int floor)
    {
        currentFloor = Mathf.Max(1, floor);
    }

    public void Register(GridTrap trap)
    {
        if (trap == null) return;
        _traps[trap.GridPosition] = trap;
    }

    public void Unregister(GridTrap trap)
    {
        if (trap == null) return;
        if (_traps.TryGetValue(trap.GridPosition, out GridTrap current) && current == trap)
            _traps.Remove(trap.GridPosition);
    }

    public bool TryTriggerAt(Vector2Int cell, IGridEntity activator)
    {
        if (!_traps.TryGetValue(cell, out GridTrap trap) || trap == null)
            return false;

        return trap.TryTrigger(activator);
    }

    public bool TryGetTrapAt(Vector2Int cell, out GridTrap trap)
    {
        return _traps.TryGetValue(cell, out trap) && trap != null;
    }

    public bool RevealAt(Vector2Int cell)
    {
        if (!TryGetTrapAt(cell, out GridTrap trap)) return false;
        trap.Reveal();
        return true;
    }

    public int TriggerArea(Vector2Int origin, int radius, IGridEntity activator)
    {
        int triggeredCount = 0;
        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(origin, radius))
        {
            if (TryTriggerAt(cell, activator))
                triggeredCount++;
        }

        return triggeredCount;
    }

    public void ScheduleDelayedAreaEffect(TrapContext context, TrapTerrainType terrainType, StatusType statusType)
    {
        // 덫은 플레이어 이동 처리 중에 예약되고, 같은 이동의 OnPlayerActionCompleted가 곧바로 발생합니다.
        // delayTurns=1이 "밟은 즉시"가 아니라 "다음 행동 완료 후" 발동되도록 1턴을 보정합니다.
        _pendingDelayedAreaEffects.Add(new PendingDelayedAreaEffect(
            context,
            terrainType,
            statusType,
            Mathf.Max(1, context.Data.delayTurns) + 1));
    }

    private void OnPlayerActionCompleted(float timeCost)
    {
        for (int i = _pendingDelayedAreaEffects.Count - 1; i >= 0; i--)
        {
            PendingDelayedAreaEffect pending = _pendingDelayedAreaEffects[i];
            pending.remainingTurns--;
            if (pending.remainingTurns > 0)
            {
                _pendingDelayedAreaEffects[i] = pending;
                continue;
            }

            ExecuteDelayedAreaEffect(pending);
            _pendingDelayedAreaEffects.RemoveAt(i);
        }
    }

    private void ExecuteDelayedAreaEffect(PendingDelayedAreaEffect pending)
    {
        TrapContext context = pending.context;
        GameEvents.OnTrapTerrainRequested?.Invoke(new TrapTerrainRequest(
            context.Position,
            context.Data.radius,
            context.Data.durationTurns,
            pending.terrainType,
            context.Trap));

        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(context.Position, context.Data.radius))
        {
            IGridEntity entity = TrapEffectUtility.GetEntityAt(cell, context.Activator);
            if (entity == null) continue;

            TrapEffectUtility.RequestStatus(
                entity,
                cell,
                pending.statusType,
                context.Data.durationTurns,
                context.Data.statusPower,
                context.Trap);
        }
    }

    private struct PendingDelayedAreaEffect
    {
        public TrapContext context;
        public TrapTerrainType terrainType;
        public StatusType statusType;
        public int remainingTurns;

        public PendingDelayedAreaEffect(TrapContext context, TrapTerrainType terrainType, StatusType statusType, int remainingTurns)
        {
            this.context = context;
            this.terrainType = terrainType;
            this.statusType = statusType;
            this.remainingTurns = remainingTurns;
        }
    }
}
