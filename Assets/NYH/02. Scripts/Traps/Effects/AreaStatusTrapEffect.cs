using UnityEngine;

public class AreaStatusTrapEffect : MonoBehaviour, ITrapEffect
{
    [SerializeField] private StatusType statusType;
    [SerializeField] private bool affectEmptyCells = true;
    [SerializeField] private int radiusOverride = -1;

    public void Execute(TrapContext context)
    {
        int radius = radiusOverride >= 0 ? radiusOverride : context.Data.radius;
        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(context.Position, radius))
        {
            IGridEntity entity = TrapEffectUtility.GetEntityAt(cell, context.Activator);
            if (entity != null || affectEmptyCells)
            {
                TrapEffectUtility.RequestStatus(
                    entity,
                    cell,
                    statusType,
                    context.Data.durationTurns,
                    context.Data.statusPower,
                    context.Trap);
            }
        }
    }
}
