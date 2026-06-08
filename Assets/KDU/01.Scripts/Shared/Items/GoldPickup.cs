using System.Collections.Generic;
using UnityEngine;
using PrototypeRT;

/// <summary>
/// 바닥에 떨어진 골드. KeyPickup/ItemPickup 과 같은 표시 패턴 —
/// 시야 밖이면 렌더러 숨김, 한 번 탐험된 셀이면 계속 표시.
/// 같은 칸에서 줍기 키(기본 Space)를 누르면 GameEvents.OnGoldDropped 로 골드를 지급하고 파괴.
/// (적/상자가 쓰는 골드 이벤트 계약을 그대로 사용 — 지갑/HUD 는 OnGoldDropped 구독으로 처리.)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GoldPickup : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }

    [SerializeField, Min(0)] private int amount = 10;
    [Tooltip("같은 칸에서 이 키를 누르면 줍는다.")]
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

    /// <summary>스포너가 셀·금액 지정. 호출 안 하면 Start 에서 현재 위치로 역산.</summary>
    public void Initialize(Vector2Int cell, int goldAmount)
    {
        GridPosition = cell;
        amount = goldAmount;
        _initialized = true;
        transform.position = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);
    }

    private void Start()
    {
        if (!_initialized)
        {
            GridPosition = GridSystem.Instance != null
                ? GridSystem.Instance.WorldToGrid(transform.position)
                : Vector2Int.zero;
        }

        _player = FindFirstObjectByType<GridPlayer>();
        UpdateVisibility();
    }

    private void Update()
    {
        if (_collected || _player == null || _player.IsDead) return;
        if (_player.GridPosition != GridPosition) return;
        if (KeyBindingManager.ShouldBlockGameplayInput) return;
        if (!KeyBindingManager.AutoPickupEnabled &&
            !Input.GetKeyDown(KeyBindingManager.GetKey(KeyBindingAction.Pickup))) return;
        Collect();
    }

    private void Collect()
    {
        _collected = true;
        if (amount > 0) GameEvents.OnGoldDropped?.Invoke(GridPosition, amount);
        Destroy(gameObject);
    }

    private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible,
                                 IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_renderer == null || VisionSystem.Instance == null) return;
        _renderer.enabled = VisionSystem.Instance.IsExplored(GridPosition);
    }
}
