using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 인벤토리 패널을 키 입력으로 열고 닫는 간단한 토글 컴포넌트입니다.
    /// 패널을 닫기 전에 InventoryController에 먼저 알려서 드래그 중인 아이템을 원래 위치로 되돌립니다.
    /// </summary>
    public class InventoryPanelToggle : MonoBehaviour
    {
        [Header("인벤토리 패널")]
        [Tooltip("열고 닫을 인벤토리 UI 패널입니다. ItemGrid가 들어 있는 패널 오브젝트를 연결합니다.")]
        [SerializeField] private GameObject inventoryPanel;

        [Header("입력")]
        [Tooltip("인벤토리를 열고 닫을 키입니다.")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        [Tooltip("게임 시작 시 인벤토리 패널이 열려 있을지 정합니다. 꺼두면 시작할 때 닫힙니다.")]
        [SerializeField] private bool openOnStart = true;

        [Tooltip("인벤토리 입력/드래그 상태를 관리하는 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private InventoryController inventoryController;

        [Tooltip("감정/강화/정화 주문서처럼 마우스 대상 지정 중인 상태를 관리합니다. 인벤토리를 닫을 때 자동 취소하기 위해 사용합니다.")]
        [SerializeField] private InventoryTargetingController targetingController;

        public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;

        private PlayerStatsView playerStatsView;

        private void Start()
        {
            if (playerStatsView == null)
                playerStatsView = FindFirstObjectByType<PlayerStatsView>();

            if (inventoryController == null)
                inventoryController = FindFirstObjectByType<InventoryController>();

            if (targetingController == null)
                targetingController = FindFirstObjectByType<InventoryTargetingController>();

            if (inventoryPanel == null)
                Debug.LogError("InventoryPanelToggle: Inventory Panel이 연결되지 않았습니다.");
            else
                SetOpen(openOnStart);
        }

        private void Update()
        {
            if (inventoryPanel == null) return;
            if (KeyBindingManager.ShouldBlockGameplayInput) return;

            if (Input.GetKeyDown(KeyBindingManager.GetKey(KeyBindingAction.Inventory)))
            {
                // 지금 상태의 반대값을 먼저 계산합니다.
                // 이렇게 해두면 "열기 처리"와 "닫기 처리"를 SetOpen 하나로 모을 수 있습니다.
                bool nextOpen = !inventoryPanel.activeSelf;
                SetOpen(nextOpen);
            }
        }

        /// <summary>
        /// 다른 UI 플로우에서 인벤토리 열림 상태를 직접 제어할 때 사용합니다.
        /// 키 입력으로 여닫을 때와 코드로 여닫을 때 같은 정리 로직을 타도록 이 함수에 모아둡니다.
        /// </summary>
        public void SetOpen(bool value)
        {
            if (inventoryPanel == null) return;

            if (!value)
            {
                // 주문서 대상 지정 중에 패널만 꺼지면 커서가 감정/강화/정화 아이콘으로 남습니다.
                // 닫기 경로에서 먼저 취소시켜 반투명 표시와 마우스 커서를 함께 원복합니다.
                targetingController?.CancelTargeting();

                // 닫을 때는 패널을 비활성화하기 전에 컨트롤러에 먼저 알려야 합니다.
                // 그래야 마우스에 들려 있던 아이템을 원래 칸으로 돌려놓고 입력을 차단할 수 있습니다.
                inventoryController?.SetInventoryOpen(false);
                inventoryPanel.SetActive(false);
                return;
            }

            // 열 때는 패널을 먼저 활성화한 뒤 컨트롤러 입력을 다시 켭니다.
            // 패널이 켜진 상태에서 하이라이트/그리드 참조를 잡는 쪽이 안전합니다.
            inventoryPanel.SetActive(true);
            playerStatsView?.Refresh();
            inventoryController?.SetInventoryOpen(true);
        }
    }
}
