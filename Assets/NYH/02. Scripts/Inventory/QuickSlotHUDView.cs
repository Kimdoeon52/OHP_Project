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
        // 키 입력은 1~9, 0 순서지만 배열 인덱스는 0~9라서 표시용 라벨을 따로 둡니다.
        private static readonly string[] SlotLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

        [Header("데이터 참조")]
        [Tooltip("퀵슬롯 데이터와 변경 이벤트를 제공하는 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private QuickSlotController quickSlotController;

        [Header("HUD 표시")]
        [Tooltip("퀵슬롯 1~0번에 대응하는 아이콘 Image 배열입니다. 순서는 1,2,3,4,5,6,7,8,9,0 입니다.")]
        [SerializeField] private Image[] slotIcons = new Image[10];

        [Tooltip("선택 사항입니다. 연결하면 각 슬롯에 1~0 번호를 표시합니다.")]
        [SerializeField] private TMP_Text[] slotNumberTexts = new TMP_Text[10];

        /// <summary>
        /// HUD 표시를 위한 참조와 클릭 방해 방지 설정을 준비합니다.
        /// 퀵슬롯 아이콘은 보여주기 전용이므로 raycast를 꺼서 드래그/호버 입력을 막지 않게 합니다.
        /// </summary>
        private void Awake()
        {
            // 인스펙터 연결이 빠진 경우에도 HUD가 동작하도록 씬에서 QuickSlotController를 찾습니다.
            if (quickSlotController == null)
                quickSlotController = FindFirstObjectByType<QuickSlotController>();

            DisableIconRaycasts();
        }

        /// <summary>
        /// 퀵슬롯 데이터 변경 이벤트를 구독하고, 현재 상태를 즉시 화면에 반영합니다.
        /// HUD가 꺼져 있는 동안 슬롯 내용이 바뀌었을 수 있으므로 Refresh를 바로 호출합니다.
        /// </summary>
        private void OnEnable()
        {
            if (quickSlotController != null)
                quickSlotController.OnQuickSlotsChanged += Refresh;

            // HUD가 꺼져 있는 동안 아이템이 바뀌었을 수 있으므로 켜질 때 즉시 한 번 맞춥니다.
            Refresh();
        }

        /// <summary>
        /// HUD가 꺼질 때 이벤트 구독을 해제합니다.
        /// 비활성 UI가 계속 갱신되거나 같은 이벤트를 여러 번 받는 문제를 막습니다.
        /// </summary>
        private void OnDisable()
        {
            if (quickSlotController != null)
                quickSlotController.OnQuickSlotsChanged -= Refresh;
        }

        /// <summary>
        /// QuickSlotController의 현재 슬롯 상태를 읽어 HUD 아이콘과 번호 텍스트를 다시 씁니다.
        /// 아이템이 없는 슬롯은 Image를 꺼서 빈 슬롯처럼 보이게 합니다.
        /// </summary>
        public void Refresh()
        {
            for (int i = 0; i < slotIcons.Length; i++)
            {
                // HUD는 실제 아이템을 소유하지 않고, 컨트롤러가 가진 슬롯 상태만 읽어 표시합니다.
                InventoryItem item = quickSlotController != null ? quickSlotController.GetItem(i) : null;
                Sprite icon = item != null && item.ItemData != null ? item.ItemData.Icon : null;

                if (slotIcons[i] != null)
                {
                    // sprite만 비우면 이전 아이콘 영역이 남을 수 있어, 아이콘이 없을 때 Image 자체를 끕니다.
                    slotIcons[i].sprite = icon;
                    slotIcons[i].enabled = icon != null;
                }
            }

            for (int i = 0; i < slotNumberTexts.Length && i < SlotLabels.Length; i++)
            {
                if (slotNumberTexts[i] != null)
                    // 번호 텍스트는 슬롯 내용과 무관하게 항상 고정 라벨을 보여줍니다.
                    slotNumberTexts[i].text = SlotLabels[i];
            }
        }

        /// <summary>
        /// HUD 아이콘 Image들이 마우스 입력을 가로채지 않도록 raycastTarget을 끕니다.
        /// 실제 드래그/호버 판정은 뒤에 있는 ItemGrid와 InventoryItem이 처리해야 합니다.
        /// </summary>
        private void DisableIconRaycasts()
        {
            for (int i = 0; i < slotIcons.Length; i++)
            {
                if (slotIcons[i] != null)
                    // 아이콘이 포인터를 먹으면 ItemGrid의 hover/drag 판정이 흔들릴 수 있어 표시 전용으로 둡니다.
                    slotIcons[i].raycastTarget = false;
            }
        }
    }
}
