using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TelegraphIndicator : MonoBehaviour
{
    private SpriteRenderer _renderer;

    public Vector2Int Cell { get; private set; }

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    public void Show(Vector2Int cell)
    {
        Cell = cell;
        transform.position = GridSystem.Instance.GridToWorld(cell);
        RefreshVisibility();
    }

    public void RefreshVisibility()
    {
        if (_renderer == null) return;
        _renderer.enabled = VisionSystem.Instance != null && VisionSystem.Instance.IsVisible(Cell);
    }
}
