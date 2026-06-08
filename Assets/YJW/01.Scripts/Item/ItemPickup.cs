using System.Collections.Generic;
using UnityEngine;
using PrototypeRT;

/// <summary>
/// 바닥에 떨어진 아이템 픽업. 열쇠(KeyPickup)와 같은 표시 패턴 —
/// 시야 밖이면 렌더러 숨김, 한 번 탐험된 셀이면 계속 표시.
/// 단 자동 획득이 아니라, 플레이어가 아이템과 '같은 칸'에서 줍기 키(기본 Space)를 누르면
/// 인벤토리에 담는다. 인벤토리가 가득 차면 줍지 못하고 바닥에 남는다.
/// 스폰 측(상자/스포너)이 itemSO·enhanceLevel·curse 를 채워두면 Start 에서 인스턴스를 구성한다.
/// (itemSO 없이 item 을 직접 주입한 경우 그 인스턴스를 그대로 사용.)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ItemPickup : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }

    [Header("아이템")]
    public ItemBase itemSO;
    public int enhanceLevel;
    public CurseData curse;   // SO라서 인스펙터에서 드래그 가능, 없으면 None
    public ItemInstance item;

    [Header("줍기")]
    [Tooltip("같은 칸에서 이 키를 누르면 줍는다.")]
    [SerializeField] private KeyCode pickupKey = KeyCode.Space;

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private PrototypeRT.InventoryController _inventory;
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

    /// <summary>스포너가 셀 위치를 지정. 호출 안 하면 Start 에서 현재 위치로 역산.</summary>
    public void Initialize(Vector2Int cell)
    {
        GridPosition = cell;
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

        if (itemSO != null)
        {
            item = itemSO.CreateInstance();
            if (item is EquipmentInstance equip)
            {
                equip.enhanceLevel = enhanceLevel;
                equip.curse = curse;
            }
        }
        ApplyIcon();

        _player = FindFirstObjectByType<GridPlayer>();
        _inventory = FindFirstObjectByType<PrototypeRT.InventoryController>();

        UpdateVisibility();
    }

    private void Update()
    {
        if (_collected || _player == null || _player.IsDead) return;
        if (_player.GridPosition != GridPosition) return;
        if (KeyBindingManager.ShouldBlockGameplayInput) return;
        if (!KeyBindingManager.AutoPickupEnabled &&
            !Input.GetKeyDown(KeyBindingManager.GetKey(KeyBindingAction.Pickup))) return;
        TryCollect();
    }

    private void TryCollect()
    {
        if (item == null) return;
        if (_inventory == null) _inventory = FindFirstObjectByType<PrototypeRT.InventoryController>();
        if (_inventory == null) return;

        // 인벤토리가 가득 차면 false — 바닥에 남겨둔다.
        if (!_inventory.TryAddItem(item)) return;

        _collected = true;
        Destroy(gameObject);
    }

    private void ApplyIcon()
    {
        if (_renderer == null) return;
        Sprite icon = itemSO != null ? itemSO.Icon
                    : (item != null && item.data != null ? item.data.Icon : null);
        if (icon != null) _renderer.sprite = icon;
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
