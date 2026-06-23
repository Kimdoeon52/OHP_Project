using System.Collections.Generic;
using UnityEngine;
using PrototypeRT;

// 진열대 1칸 = 상품 1개. 같은 칸에서 구매키 → 구매 팝업
[RequireComponent(typeof(SpriteRenderer))]
public class ShopStall : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public ItemBase ItemSO => itemSO;
    public int Price => price;
    public int EnhanceLevel => enhanceLevel;

    [Header("상품")]
    [SerializeField] private ItemBase itemSO;
    [SerializeField] private int price;
    [SerializeField] private int enhanceLevel;

    [Tooltip("가격 라벨 (월드)")]
    [SerializeField] private TMPro.TMP_Text priceLabel;

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private ShopPurchasePopup _popup;
    private bool _sold;

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

    // 스포너가 셀·상품 지정
    public void Initialize(Vector2Int cell, ItemBase product, int productPrice, int productEnhance)
    {
        GridPosition = cell;
        itemSO = product;
        price = productPrice;
        enhanceLevel = productEnhance;
        transform.position = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);
        ApplyIcon();
        ApplyPriceLabel();
    }

    private void Start()
    {
        if (GridPosition == Vector2Int.zero && GridSystem.Instance != null)
            GridPosition = GridSystem.Instance.WorldToGrid(transform.position);

        _player = FindFirstObjectByType<GridPlayer>();
        _popup = FindFirstObjectByType<ShopPurchasePopup>();
        ApplyIcon();
        ApplyPriceLabel();
        UpdateVisibility();
    }

    private void Update()
    {
        if (_sold || itemSO == null || _player == null || _player.IsDead)
            return;

        // 같은 칸만 (인접 허용 시 옆 진열대 팝업이 잘못 뜸)
        if (_player.GridPosition != GridPosition)
            return;
        if (KeyBindingManager.ShouldBlockGameplayInput)
            return;
        if (_popup != null && _popup.IsOpen)
            return;
        if (!Input.GetKeyDown(KeyBindingManager.GetKey(KeyBindingAction.Buy)))
            return;

        if (_popup == null)
            _popup = FindFirstObjectByType<ShopPurchasePopup>();
        if (_popup == null)
        {
            Debug.LogWarning("[ShopStall] ShopPurchasePopup 씬에 없음 — 팝업 배선 필요");
            return;
        }
        _popup.Open(this);
    }

    // 구매 인스턴스 생성 (ItemPickup 패턴)
    public ItemInstance BuildInstance()
    {
        if (itemSO == null)
            return null;

        ItemInstance inst = itemSO.CreateInstance();
        if (inst is EquipmentInstance equip)
            equip.enhanceLevel = enhanceLevel;
        else if (inst is PotionInstance potion && potion.count <= 0)
            potion.count = 1;
        else if (inst is ScrollInstance scroll && scroll.count <= 0)
            scroll.count = 1;
        return inst;
    }

    // 판매 완료
    public void MarkSold()
    {
        _sold = true;
        Destroy(gameObject);
    }

    private void ApplyIcon()
    {
        if (_renderer != null && itemSO != null && itemSO.Icon != null)
            _renderer.sprite = itemSO.Icon;
    }

    private void ApplyPriceLabel()
    {
        if (priceLabel != null)
            priceLabel.text = price.ToString();
    }

    private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible, IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_renderer == null || VisionSystem.Instance == null)
            return;

        bool show = VisionSystem.Instance.IsExplored(GridPosition);
        _renderer.enabled = show;
        if (priceLabel != null)
            priceLabel.enabled = show;
    }
}
