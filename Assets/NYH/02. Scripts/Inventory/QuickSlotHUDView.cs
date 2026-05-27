using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrototypeRT
{
    /// <summary>
    /// 게임 화면 HUD에 퀵슬롯 아이콘과 1~0 번호를 표시합니다.
    /// 실제 슬롯 데이터는 QuickSlotController가 들고, 이 클래스는 표시만 갱신합니다.
    /// </summary>
    public class QuickSlotHUDView : MonoBehaviour
    {
        private static readonly string[] SlotLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

        [Header("Source")]
        [SerializeField] private QuickSlotController quickSlotController;

        [Header("HUD Images")]
        [Tooltip("퀵슬롯 1~0번에 대응하는 아이콘 Image 배열입니다. 길이는 10을 권장합니다.")]
        [SerializeField] private Image[] slotIcons = new Image[10];

        [Tooltip("선택 사항입니다. 연결하면 각 슬롯에 1~0 번호를 표시합니다.")]
        [SerializeField] private TMP_Text[] slotNumberTexts = new TMP_Text[10];

        private void Awake()
        {
            if (quickSlotController == null)
                quickSlotController = FindFirstObjectByType<QuickSlotController>();
        }

        private void OnEnable()
        {
            if (quickSlotController != null)
                quickSlotController.OnQuickSlotsChanged += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (quickSlotController != null)
                quickSlotController.OnQuickSlotsChanged -= Refresh;
        }

        public void Refresh()
        {
            for (int i = 0; i < slotIcons.Length; i++)
            {
                InventoryItem item = quickSlotController != null ? quickSlotController.GetItem(i) : null;
                Sprite icon = item != null && item.ItemData != null ? item.ItemData.Icon : null;

                if (slotIcons[i] != null)
                {
                    slotIcons[i].sprite = icon;
                    slotIcons[i].enabled = icon != null;
                }
            }

            for (int i = 0; i < slotNumberTexts.Length && i < SlotLabels.Length; i++)
            {
                if (slotNumberTexts[i] != null)
                    slotNumberTexts[i].text = SlotLabels[i];
            }
        }
    }
}
