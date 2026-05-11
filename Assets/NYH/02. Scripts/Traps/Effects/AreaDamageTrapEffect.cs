using UnityEngine;

public class AreaDamageTrapEffect : MonoBehaviour, ITrapEffect
{
    [SerializeField] private int radiusOverride = -1;
    [SerializeField, Range(0f, 1f)] private float edgeDamageMultiplier = 0.5f;
    [SerializeField] private bool damageCenter = true;

    public void Execute(TrapContext context)
    {
        int radius = radiusOverride >= 0 ? radiusOverride : context.Data.radius;
        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(context.Position, radius))
        {
            IGridEntity entity = TrapEffectUtility.GetEntityAt(cell, context.Activator);
            IDamageable damageable = TrapEffectUtility.AsDamageable(entity);
            if (damageable == null || damageable.IsDead) continue;

            bool isCenter = cell == context.Position;
            if (isCenter && !damageCenter) continue;

            int damage = context.Data.CalculateDamage(damageable);
            if (!isCenter)
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * edgeDamageMultiplier));

            damageable.TakeDamage(damage);
        }
    }
}
