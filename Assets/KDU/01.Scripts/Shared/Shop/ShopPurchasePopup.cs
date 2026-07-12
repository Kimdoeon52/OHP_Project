using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrototypeRT;

// 구매 확인 팝업 — 아이템 정보·가격 표시 후 구매/취소
public class ShopPurchasePopup : MonoBehaviour
{
    public bool IsOpen => panel != null && panel.activeSelf;

    [Header("패널")]
    [SerializeField] private GameObject panel;

    [Header("상품 표시")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text messageText;

    [Header("버튼")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button cancelButton;

    private ShopStall _stall;
    private InventoryController _inventory;
    private GridPlayer _player;

    private void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuy);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);
        if (panel != null)
            panel.SetActive(false);
    }

    // 진열대 상품으로 팝업 열기
    public void Open(ShopStall stall)
    {
        if (stall == null || stall.ItemSO == null)
            return;

        _stall = stall;
        ItemBase so = stall.ItemSO;

        if (iconImage != null)
        {
            iconImage.sprite = so.Icon;
            iconImage.enabled = so.Icon != null;
        }
        // SO 대신 감정완료 인스턴스 툴팁 사용 (미감정 표시 방지, 이름은 툴팁에 포함)
        SetText(infoText, stall.BuildInstance().GetToolTipStats());
        SetText(priceText, $"{stall.Price} {LocalizationManager.GetOrDefault("msg_골드", "Gold")}");
        SetText(messageText, "");

        if (panel != null)
            panel.SetActive(true);
    }

    // 진열대 칸을 벗어나면 팝업 자동 닫기 — 세이프룸 이탈 후 잔존 방지.
    private void Update()
    {
        if (!IsOpen || _stall == null)
            return;

        if (_player == null)
            _player = FindFirstObjectByType<GridPlayer>();
        if (_player == null)
            return;

        if (_player.IsDead || _player.GridPosition != _stall.GridPosition)
            Close();
    }

    public void Close()
    {
        _stall = null;
        if (panel != null)
            panel.SetActive(false);
    }

    // 구매: 인벤 추가 성공 후 골드 차감 (가득 시 골드 보존)
    private void OnBuy()
    {
        if (_stall == null)
            return;

        Wallet wallet = Wallet.Instance;
        if (wallet == null)
        {
            SetText(messageText, LocalizationManager.GetOrDefault("msg_지갑없음", "No Wallet"));
            return;
        }

        if (wallet.Balance < _stall.Price)
        {
            SetText(messageText, LocalizationManager.GetOrDefault("msg_골드부족", "Not Enough Gold"));
            return;
        }

        if (_inventory == null)
            _inventory = FindFirstObjectByType<InventoryController>();
        if (_inventory == null)
        {
            SetText(messageText, LocalizationManager.GetOrDefault("msg_인벤토리없음", "No Inventory"));
            return;
        }

        ItemInstance inst = _stall.BuildInstance();
        if (inst == null || !_inventory.TryAddItem(inst))
        {
            SetText(messageText, LocalizationManager.GetOrDefault("msg_인벤토리가득", "Inventory Full"));
            return;
        }

        wallet.TrySpend(_stall.Price);
        GameEvents.OnItemPurchased?.Invoke(_stall.ItemSO);
        _stall.MarkSold();
        Close();
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
