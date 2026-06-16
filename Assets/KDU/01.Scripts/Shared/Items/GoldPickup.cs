using System.Collections.Generic;
using UnityEngine;
using PrototypeRT;


// 바닥에 떨어진 골드
[RequireComponent(typeof(SpriteRenderer))]
public class GoldPickup : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }

    [SerializeField, Min(0)] private int amount = 10;
    [SerializeField] private KeyCode pickupKey = KeyCode.Space;

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private bool _initialized;
    private bool _collected;

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

    // 스폰할때 위치 및 금액 지정
    public void Initialize(Vector2Int cell, int goldAmount)
    {
        GridPosition = cell;
        amount = goldAmount;
        _initialized = true;
        transform.position = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(cell) : new Vector3(cell.x, cell.y, 0);
    }

    private void Start()
    {
        if (!_initialized)
        {
            GridPosition = GridSystem.Instance != null ? GridSystem.Instance.WorldToGrid(transform.position) : Vector2Int.zero;
        }

        _player = FindFirstObjectByType<GridPlayer>();
        UpdateVisibility();
    }

    private void Update()
    {
        if (_collected || _player == null || _player.IsDead) 
            return;

        if (_player.GridPosition != GridPosition) 
            return;

        if (KeyBindingManager.ShouldBlockGameplayInput) 
            return;

        if (!KeyBindingManager.AutoPickupEnabled && !KeyBindingManager.GetGameplayKeyDown(KeyBindingAction.Pickup))
            return;

        Collect();
    }

    private void Collect()
    {
        _collected = true;

        if (amount > 0) 
            GameEvents.OnGoldDropped?.Invoke(GridPosition, amount);

        GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.GoldPicked);
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
