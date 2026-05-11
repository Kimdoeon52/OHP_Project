using System.Collections.Generic;
using UnityEngine;

public class TrapTerrainController : MonoBehaviour
{
    [SerializeField] private TrapTerrainVisual visualPrefab;

    private readonly List<ActiveTerrainEffect> _activeEffects = new();

    private void OnEnable()
    {
        GameEvents.OnTrapTerrainRequested += OnTrapTerrainRequested;
        GameEvents.OnPlayerActionCompleted += OnPlayerActionCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnTrapTerrainRequested -= OnTrapTerrainRequested;
        GameEvents.OnPlayerActionCompleted -= OnPlayerActionCompleted;
    }

    private void OnTrapTerrainRequested(TrapTerrainRequest request)
    {
        var visuals = new List<TrapTerrainVisual>();
        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(request.Origin, request.Radius))
        {
            if (visualPrefab == null) continue;
            TrapTerrainVisual visual = Instantiate(visualPrefab);
            visual.Show(cell, request.TerrainType);
            visuals.Add(visual);
        }

        _activeEffects.Add(new ActiveTerrainEffect(
            request.Origin,
            request.Radius,
            request.DurationTurns > 0 ? request.DurationTurns : 1,
            request.TerrainType,
            visuals));
    }

    private void OnPlayerActionCompleted(float timeCost)
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveTerrainEffect effect = _activeEffects[i];
            ApplyTerrainTick(effect);

            effect.remainingTurns--;
            if (effect.remainingTurns <= 0)
            {
                DestroyVisuals(effect.visuals);
                _activeEffects.RemoveAt(i);
            }
            else
            {
                _activeEffects[i] = effect;
            }
        }
    }

    private void ApplyTerrainTick(ActiveTerrainEffect effect)
    {
        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(effect.origin, effect.radius))
        {
            IGridEntity entity = TrapEffectUtility.GetEntityAt(cell);
            if (entity == null) continue;

            switch (effect.terrainType)
            {
                case TrapTerrainType.Fire:
                    TrapEffectUtility.RequestStatus(entity, cell, StatusType.Burn, 1, 1, null);
                    break;
                case TrapTerrainType.Chill:
                    TrapEffectUtility.RequestStatus(entity, cell, StatusType.Chill, 1, 1, null);
                    break;
                case TrapTerrainType.Shock:
                    TrapEffectUtility.RequestStatus(entity, cell, StatusType.Shock, 1, 1, null);
                    break;
                case TrapTerrainType.Gas:
                    TrapEffectUtility.RequestStatus(entity, cell, StatusType.Poison, 1, 1, null);
                    break;
                case TrapTerrainType.Ooze:
                    TrapEffectUtility.RequestStatus(entity, cell, StatusType.Ooze, 1, 1, null);
                    break;
            }
        }
    }

    private void DestroyVisuals(List<TrapTerrainVisual> visuals)
    {
        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null)
                Destroy(visuals[i].gameObject);
        }
    }

    private struct ActiveTerrainEffect
    {
        public Vector2Int origin;
        public int radius;
        public int remainingTurns;
        public TrapTerrainType terrainType;
        public List<TrapTerrainVisual> visuals;

        public ActiveTerrainEffect(Vector2Int origin, int radius, int remainingTurns, TrapTerrainType terrainType, List<TrapTerrainVisual> visuals)
        {
            this.origin = origin;
            this.radius = radius;
            this.remainingTurns = remainingTurns;
            this.terrainType = terrainType;
            this.visuals = visuals;
        }
    }
}
