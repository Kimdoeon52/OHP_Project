using UnityEngine;

public class DamageTrapEffect : MonoBehaviour, ITrapEffect
{
    [SerializeField] private bool affectArea;
    [SerializeField] private int radiusOverride = -1;

    public void Execute(TrapContext context)
    {
        int radius = radiusOverride >= 0 ? radiusOverride : context.Data.radius;
        if (!affectArea || radius <= 0)
        {
            DealDamage(context.Activator, context);
            return;
        }

        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(context.Position, radius))
            DealDamage(TrapEffectUtility.GetEntityAt(cell, context.Activator), context);
    }

    private static void DealDamage(IGridEntity entity, TrapContext context)
    {
        TrapEffectUtility.DealDamage(entity, context.Data);
    }
}
