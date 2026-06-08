using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class KeyPickup : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public KeyType KeyType => keyType;

    [SerializeField] private KeyType keyType = KeyType.SpecialRoom;

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private bool _collected;
    private bool _initialized;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        GameEvents.OnVisionUpdated += OnVisionUpdated;
    }

    private void OnDisable()
    {
        GameEvents.OnVisionUpdated -= OnVisionUpdated;
    }

    public void Initialize(Vector2Int cell, KeyType type)
    {
        GridPosition = cell;
        keyType = type;
        _initialized = true;
        transform.position = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(cell) : new Vector3(cell.x, cell.y, 0);
    }

    private void Start()
    {
        _player = FindFirstObjectByType<GridPlayer>();

        // 튜토리얼용 직접 배치
        if (!_initialized && GridSystem.Instance != null)
            GridPosition = GridSystem.Instance.WorldToGrid(transform.position);

        UpdateVisibility();
    }

    private void Update()
    {
        if (_collected || _player == null || _player.IsDead) 
            return;
        if (_player.GridPosition != GridPosition) 
            return;
        TryCollect();
    }

    private void TryCollect()
    {
        if (KeyInventory.Instance == null) 
            return;
        KeyInventory.Instance.Grant(keyType);
        _collected = true;
        Destroy(gameObject);
    }

    private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible, IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_renderer == null || VisionSystem.Instance == null)
            return;
        _renderer.enabled = VisionSystem.Instance.IsExplored(GridPosition);
    }
}
