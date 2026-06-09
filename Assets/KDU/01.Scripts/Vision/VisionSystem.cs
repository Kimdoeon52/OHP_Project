using System.Collections.Generic;
using UnityEngine;

public class VisionSystem : Singleton<VisionSystem>
{

    [SerializeField] private GameConfig config;

    private readonly HashSet<Vector2Int> _visible = new();
    private readonly HashSet<Vector2Int> _explored = new();

    public IReadOnlyCollection<Vector2Int> Visible => _visible;
    public IReadOnlyCollection<Vector2Int> Explored => _explored;

    public int VisionRadiusDelta { get; set; } = 0;

    private int VisionRadius
    {
        get
        {
            int baseRadius = config != null ? config.visionRadius : 5;
            // Eagle Eye 특성은 기존 VisionRadiusDelta와 더해지는 보너스로만 처리한다.
            // 포션/층 효과 같은 임시 시야 변화와 특성 보너스가 서로 덮어쓰지 않게 하기 위해서다.
            TraitController traitController = TraitController.FindAvailableInstance();
            int traitBonus = traitController != null ? traitController.GetVisionBonus() : 0;
            return Mathf.Max(1, baseRadius + VisionRadiusDelta + traitBonus);
        }
    }

    protected override void Awake()
    {
        base.Awake();
    }

    public void UpdateVision(Vector2Int playerPos)
    {
        _visible.Clear();

        int r = VisionRadius;
        int r2 = r * r;

        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                if (dx * dx + dy * dy > r2) 
                    continue;

                var cell = new Vector2Int(playerPos.x + dx, playerPos.y + dy);
                if (!GridSystem.Instance.IsInBounds(cell)) 
                    continue;

                if (!HasLineOfSight(playerPos, cell))
                    continue;

                _visible.Add(cell);
                _explored.Add(cell);
            }
        }

        GameEvents.OnVisionUpdated?.Invoke(_visible, _explored);
    }

    public bool IsVisible(Vector2Int pos) => _visible.Contains(pos);
    public bool IsExplored(Vector2Int pos) => _explored.Contains(pos);

    public void ResetForNewFloor()
    {
        _visible.Clear();
        _explored.Clear();
        VisionRadiusDelta = 0;
        GameEvents.OnVisionUpdated?.Invoke(_visible, _explored);
    }

    private bool HasLineOfSight(Vector2Int from, Vector2Int to)
    {
        int x = from.x, y = from.y;
        int dx = Mathf.Abs(to.x - from.x), dy = Mathf.Abs(to.y - from.y);
        int sx = from.x < to.x ? 1 : -1;
        int sy = from.y < to.y ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x == to.x && y == to.y) 
                return true;

            if (!(x == from.x && y == from.y) && !GridSystem.Instance.IsWalkable(new Vector2Int(x, y)))
                return false;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 < dx) { err += dx; y += sy; }
        }
    }
}
