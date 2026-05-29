using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class EntityBase : MonoBehaviour, IGridEntity
{
    public Vector2Int GridPosition { get; protected set; }

    protected SpriteRenderer SpriteRenderer { get; private set; }

    protected virtual void Awake()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private bool _initialized = false;

    public virtual void Initialize(Vector2Int startPos)
    {
        if (_initialized) return;
        _initialized = true;

        GridPosition = startPos;
        if (GridSystem.Instance != null)
        {
            GridSystem.Instance.RegisterOccupancy(startPos, this);
        }
        SyncWorldPosition();
    }
    protected virtual void OnDestroy()
    {
        if (GridSystem.Instance != null)
        {
            GridSystem.Instance.UnRegisterOccupancy(GridPosition);
        }
    }

    public virtual void SetGridPosition(Vector2Int newPos)
    {
        if(GridSystem.Instance != null)
        {
            GridSystem.Instance.UnRegisterOccupancy(GridPosition);
            GridSystem.Instance.RegisterOccupancy(newPos, this);
        }
        GridPosition = newPos;
        SyncWorldPosition();
    }

    private void SyncWorldPosition()
    {
        transform.position = GridSystem.Instance.GridToWorld(GridPosition);
    }

    public virtual void OnTurnUpdate() { }
}
