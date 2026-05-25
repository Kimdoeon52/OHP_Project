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

    public virtual void Initialize(Vector2Int startPos)
    {
        GridPosition = startPos;
        SyncWorldPosition();
    }

    public virtual void SetGridPosition(Vector2Int newPos)
    {
        GridPosition = newPos;
        SyncWorldPosition();
    }

    private void SyncWorldPosition()
    {
        transform.position = GridSystem.Instance.GridToWorld(GridPosition);
    }

    public abstract void OnTurnUpdate();
}
