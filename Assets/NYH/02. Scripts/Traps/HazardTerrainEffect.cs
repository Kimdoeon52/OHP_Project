using UnityEngine;

public static class HazardTerrainEffect
{
    private const float HazardMoveCostMultiplier = 2f;
    private const int HazardSlowDurationTurns = 2;
    private const int HazardDamageDurationTurns = 2;
    private const int HazardDamagePower = 1;

    private static readonly StatusType HazardDamageStatusType = StatusType.Poison;

    public static bool IsHazardCell(Vector2Int cell)
    {
        return MapGenerator.Instance != null
            && MapGenerator.Instance.HazardCells != null
            && MapGenerator.Instance.HazardCells.Contains(cell);
    }

    public static float ModifyMoveCost(IGridEntity entity, Vector2Int targetCell, float baseCost)
    {
        if (!ShouldApplyHazard(entity, targetCell))
            return baseCost;

        return Mathf.Max(0.01f, baseCost * HazardMoveCostMultiplier);
    }

    public static void ApplyOnEnter(IGridEntity entity, Vector2Int cell)
    {
        if (!ShouldApplyHazard(entity, cell))
            return;

        TrapEffectUtility.RequestStatus(entity, cell, StatusType.Slow, HazardSlowDurationTurns, 1, null);
        TrapEffectUtility.RequestStatus(entity, cell, HazardDamageStatusType, HazardDamageDurationTurns, HazardDamagePower, null);
    }

    private static bool ShouldApplyHazard(IGridEntity entity, Vector2Int cell)
    {
        if (entity == null || !IsHazardCell(cell))
            return false;

        if (entity is Component component)
        {
            StatusController status = component.GetComponent<StatusController>();
            if (status != null && status.ShouldIgnoreHazard())
                return false;
        }

        return true;
    }
}
