using UnityEngine;

public class DamageAndStatusTrapEffect : MonoBehaviour, ITrapEffect
{
    [SerializeField] private StatusType[] statuses;
    [SerializeField] private bool affectArea;
    [SerializeField] private int radiusOverride = -1;

    public void Execute(TrapContext context)
    {
        int radius = radiusOverride >= 0 ? radiusOverride : context.Data.radius;
        if (!affectArea || radius <= 0)
        {
            Apply(context.Activator, context);
            return;
        }

        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(context.Position, radius))
            Apply(TrapEffectUtility.GetEntityAt(cell, context.Activator), context);
    }

    private void Apply(IGridEntity entity, TrapContext context)
    {
        if (entity == null) return;

        IDamageable damageable = TrapEffectUtility.AsDamageable(entity);
        int beforeHp = damageable != null ? damageable.CurrentHp : 0;
        if (damageable != null && !damageable.IsDead)
            damageable.TakeDamage(context.Data.CalculateDamage(damageable));

        int dealtDamage = damageable != null ? Mathf.Max(0, beforeHp - damageable.CurrentHp) : 0;
        int power = context.Data.statusPower > 0 ? context.Data.statusPower : dealtDamage;
        for (int i = 0; i < statuses.Length; i++)
        {
            TrapEffectUtility.RequestStatus(
                entity,
                entity.GridPosition,
                statuses[i],
                context.Data.durationTurns,
                power,
                context.Trap);
        }
    }
}
