using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 인벤토리 패널을 UIManager와 연결하는 어댑터입니다.
    /// 기존 인벤토리 드래그/타겟팅 정리 흐름은 유지하고, 열기/닫기 명령만 중앙 관리자로 넘깁니다.
    /// </summary>
    public class InventoryPanelToggle : ManagedUIPanel
    {
        [Header("인벤토리 패널")]
        [Tooltip("열고 닫을 인벤토리 UI 패널입니다.")]
        [SerializeField] private GameObject inventoryPanel;

        [Header("입력")]
        [Tooltip("인벤토리를 열고 닫을 키입니다.")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        [Tooltip("게임 시작 시 인벤토리 패널을 열어둘지 정합니다.")]
        [SerializeField] private bool openOnStart = true;

        [Tooltip("인벤토리 입력/드래그 상태를 관리하는 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private InventoryController inventoryController;

        [Tooltip("감정/강화/정화 주문처럼 마우스 타겟 지정 중인 상태를 관리합니다.")]
        [SerializeField] private InventoryTargetingController targetingController;

        public override bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;

        private PlayerStatsView playerStatsView;

        private void Start()
        {
            ResolveReferences();

            if (inventoryPanel == null)
            {
                Debug.LogError("InventoryPanelToggle: Inventory Panel이 연결되지 않았습니다.");
                return;
            }

            if (UIManager.Instance != null)
            {
                if (openOnStart)
                    UIManager.Instance.Open(PanelId);
                else
                    SetOpen(false);
            }
            else
            {
                SetOpen(openOnStart);
            }
        }

        private void Update()
        {
            if (inventoryPanel == null) return;

            if (Input.GetKeyDown(toggleKey))
            {
                // UIManager가 있으면 단일 패널 규칙, ESC 처리, 게임 입력 차단을 한 곳에서 유지한다.
                if (UIManager.Instance != null)
                    UIManager.Instance.Toggle(PanelId);
                else
                    SetOpen(!inventoryPanel.activeSelf);
            }
        }

        public override void OpenPanel()
        {
            SetOpen(true);
        }

        public override void ClosePanel()
        {
            SetOpen(false);
        }

        /// <summary>
        /// 다른 UI 플로우에서 인벤토리 열림 상태를 직접 제어할 때 사용합니다.
        /// 닫기 경로에서 드래그/타겟팅 상태를 먼저 정리해야 아이템이 떠 있는 상태로 남지 않습니다.
        /// </summary>
        public void SetOpen(bool value)
        {
            if (inventoryPanel == null) return;

            ResolveReferences();

            if (!value)
            {
                // 패널만 끄면 타겟팅 커서나 드래그 중인 아이템이 남을 수 있어 닫기 전에 먼저 정리한다.
                targetingController?.CancelTargeting();
                inventoryController?.SetInventoryOpen(false);
                inventoryPanel.SetActive(false);
                return;
            }

            inventoryPanel.SetActive(true);
            playerStatsView?.Refresh();
            inventoryController?.SetInventoryOpen(true);
        }

        private void ResolveReferences()
        {
            if (playerStatsView == null)
                playerStatsView = FindFirstObjectByType<PlayerStatsView>();

            if (inventoryController == null)
                inventoryController = FindFirstObjectByType<InventoryController>();

            if (targetingController == null)
                targetingController = FindFirstObjectByType<InventoryTargetingController>();
        }
    }
}
