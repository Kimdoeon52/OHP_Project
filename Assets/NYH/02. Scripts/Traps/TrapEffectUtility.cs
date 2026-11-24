using System.Collections.Generic;
using UnityEngine;

public static class TrapEffectUtility
{
    public static IEnumerable<Vector2Int> CellsInRadius(Vector2Int origin, int radius)
    {
        radius = Mathf.Max(0, radius);
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                var cell = new Vector2Int(origin.x + x, origin.y + y);
                if (GridSystem.Instance != null && GridSystem.Instance.IsWalkable(cell))
                    yield return cell;
            }
        }
    }

    public static IGridEntity GetEntityAt(Vector2Int cell, IGridEntity knownActivator = null)
    {
        // 함정/장판을 밟은 주체를 이미 알고 있다면 가장 먼저 사용합니다.
        // TurnManager/GridSystem 등록 타이밍 차이 때문에 같은 프레임에 못 찾는 일을 줄입니다.
        if (knownActivator != null && knownActivator.GridPosition == cell)
            return knownActivator;

        // 적과 대형 보스는 TurnManager에 등록되어 있습니다.
        // 특히 IMultiCellGridEntity 보스는 TurnManager.GetEntityAt이 OccupiedCells까지 검사합니다.
        IGridEntity turnEntity = TurnManager.Instance != null ? TurnManager.Instance.GetEntityAt(cell) : null;
        if (turnEntity != null)
            return turnEntity;

        // 플레이어는 TurnManager 목록이 아니라 GridSystem 점유 정보에 등록되어 있으므로 마지막에 여기서 찾습니다.
        // 이 fallback이 없으면 자기 발밑에 불/얼음 포션을 던졌을 때 즉시 상태이상이 걸리지 않습니다.
        return GridSystem.Instance != null ? GridSystem.Instance.GetEntityAt(cell) : null;
    }

    public static void RequestStatus(IGridEntity entity, Vector2Int targetPosition, StatusType statusType, int durationTurns, int power)
    {
        var request = new StatusRequest(entity, targetPosition, statusType, durationTurns, power);
        GameEvents.OnStatusRequested?.Invoke(request);

        if (entity is Component component)
        {
            StatusController controller = component.GetOrAddComponent<StatusController>();
            controller.ApplyStatus(request);
        }
    }
}
