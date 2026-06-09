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
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text messageText;

    [Header("버튼")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button cancelButton;

    private ShopStall _stall;
    private InventoryController _inventory;

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
        SetText(nameText, so.Name);
        SetText(infoText, so.GetToolTipStats());
        SetText(priceText, $"{stall.Price} 골드");
        SetText(messageText, "");

        if (panel != null)
            panel.SetActive(true);
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
            SetText(messageText, "지갑 없음");
            return;
        }

        if (wallet.Balance < _stall.Price)
        {
            SetText(messageText, "골드 부족");
            return;
        }

        if (_inventory == null)
            _inventory = FindFirstObjectByType<InventoryController>();
        if (_inventory == null)
        {
            SetText(messageText, "인벤토리 없음");
            return;
        }

        ItemInstance inst = _stall.BuildInstance();
        if (inst == null || !_inventory.TryAddItem(inst))
        {
            SetText(messageText, "인벤토리 가득");
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
