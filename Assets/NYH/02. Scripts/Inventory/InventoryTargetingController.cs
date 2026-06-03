using UnityEngine;

namespace PrototypeRT
{
    public enum InventoryScrollTargetKind
    {
        // 대상 지정 주문서가 아닌 상태입니다.
        None,
        // 감정 스크롤: 감정 가능한 미감정 아이템만 대상으로 허용합니다.
        Analyze,
        // 강화 스크롤: 강화 가능한 15강 미만 아이템만 대상으로 허용합니다.
        Reinforce,
        // 정화 스크롤: 저주가 실제로 붙어 있는 아이템만 대상으로 허용합니다.
        Purify
    }

    public enum InventoryTargetingMode
    {
        // 대상 지정 중이 아닌 평상시 상태입니다.
        None,
        // 다음 좌클릭을 맵 타일로 해석해 포션을 던지는 상태입니다.
        ThrowPotion,
        // 다음 좌클릭을 인벤토리/장비 아이템 선택으로 해석하는 상태입니다.
        TargetScroll
    }

    /// <summary>
    /// 인벤토리 아이템 사용 중 "다음 마우스 클릭으로 대상 선택"이 필요한 흐름을 담당합니다.
    /// 투척 포션은 맵 타일을, 감정/강화/정화 주문서는 인벤토리 또는 장착 장비 아이템을 대상으로 삼습니다.
    /// 
    /// 단순 즉시 사용(UseImmediate)이 불가능한 타겟팅 요구 아이템의 조준 상태(State)를 관리하는 스크립트입니다.
    /// 아이템 활성화 이후 발생하는 다음 마우스 클릭 이벤트를 가로채어, 맵 상의 특정 타일(폭탄 투척)이나
    /// 인벤토리 내 다른 장비 아이템(주문서 강화 등)으로 좌표와 타겟 데이터를 전달하는 중간 브릿지 역할을 수행합니다.
    /// </summary>
    public class InventoryTargetingController : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("대상 지정 중 일반 인벤토리 클릭 처리를 잠시 막기 위한 인벤토리 컨트롤러입니다.")]
        [SerializeField] private InventoryController inventoryController;

        [Tooltip("투척 포션 조준을 시작할 때 인벤토리 패널을 닫기 위한 토글 컴포넌트입니다.")]
        [SerializeField] private InventoryPanelToggle inventoryPanelToggle;

        [Tooltip("장착 중인 장비도 주문서 대상으로 선택하기 위한 장비 컨트롤러입니다.")]
        [SerializeField] private NYHEquipmentController equipmentController;

        [Header("커서")]
        [Tooltip("투척 포션 조준 중 사용할 커서입니다. 비워두면 기본 커서를 유지합니다.")]
        [SerializeField] private Texture2D throwPotionCursor;

        [Tooltip("감정 주문서 대상 선택 중 사용할 커서입니다.")]
        [SerializeField] private Texture2D analyzeCursor;

        [Tooltip("강화 주문서 대상 선택 중 사용할 커서입니다.")]
        [SerializeField] private Texture2D reinforceCursor;

        [Tooltip("정화 주문서 대상 선택 중 사용할 커서입니다.")]
        [SerializeField] private Texture2D purifyCursor;

        private InventoryTargetingMode _mode;
        private InventoryScrollTargetKind _scrollKind;
        private InventoryItem _activeItem;
        private int _startedFrame;

        public bool IsTargeting => _mode != InventoryTargetingMode.None;

        /// <summary>
        /// InventoryController가 런타임에 자동 생성한 경우 참조를 주입합니다.
        /// 대상 지정 컨트롤러는 인벤토리의 들고 있는 아이템 상태를 되돌릴 수 있어야 합니다.
        /// </summary>
        public void Configure(InventoryController inventory)
        {
            inventoryController = inventory;
            ResolveReferences();
        }

        /// <summary>
        /// 던지는 포션 조준 모드를 시작합니다.
        /// 더블클릭한 포션은 아직 소비하지 않고, 다음 좌클릭으로 유효 타일을 고른 뒤 소비합니다.
        /// </summary>
        public bool BeginThrowPotion(InventoryItem potion)
        {
            ResolveReferences();

            // ThrowablePotion은 IThrowable을 구현하므로, Throw 대상이 아니면 조준 모드로 들어가지 않습니다.
            if (potion == null || potion.ItemData is not IThrowable)
                return false;

            // activeItem은 나중에 좌클릭 성공/우클릭 취소 때 소비하거나 원위치 복귀할 기준 아이템입니다.
            _activeItem = potion;
            _mode = InventoryTargetingMode.ThrowPotion;
            _scrollKind = InventoryScrollTargetKind.None;
            _startedFrame = Time.frameCount;
            SetCursor(throwPotionCursor);

            // 투척은 맵을 클릭해야 하므로 인벤토리 패널을 닫고 게임 화면을 보이게 합니다.
            inventoryPanelToggle?.SetOpen(false);
            return true;
        }

        /// <summary>
        /// 감정/강화/정화 주문서의 대상 지정 모드를 시작합니다.
        /// 주문서는 아직 소비하지 않고, 다음 좌클릭으로 유효한 인벤토리 또는 장비 아이템을 고릅니다.
        /// </summary>
        public bool BeginTargetScroll(InventoryItem scroll, InventoryScrollTargetKind kind)
        {
            Debug.Log("아이템 사용 중");
            ResolveReferences();

            if (scroll == null || scroll.ItemData == null || kind == InventoryScrollTargetKind.None)
                return false;

            _activeItem = scroll;
            _mode = InventoryTargetingMode.TargetScroll;
            _scrollKind = kind;
            _startedFrame = Time.frameCount;
            SetCursor(GetCursorForScroll(kind));
            return true;
        }

        /// <summary>
        /// 대상 지정 모드에서 마우스 입력을 처리합니다.
        /// 우클릭은 취소, 좌클릭은 현재 모드에 맞는 대상 선택으로 사용합니다.
        /// </summary>
        private void Update()
        {
            if (!IsTargeting)
                return;

            // 더블클릭으로 모드를 켠 바로 그 프레임의 좌클릭은 "사용 시작" 입력이므로 대상 선택으로 처리하지 않습니다.
            if (Time.frameCount == _startedFrame)
                return;

            if (Input.GetMouseButtonDown(1))
            {
                CancelTargeting();
                return;
            }

            if (!Input.GetMouseButtonDown(0))
                return;

            if (_mode == InventoryTargetingMode.ThrowPotion)
                TryCompleteThrowPotion();
            else if (_mode == InventoryTargetingMode.TargetScroll)
                TryCompleteScrollTarget();
        }

        /// <summary>
        /// 투척 포션 대상 타일을 확정하고 포션을 던집니다.
        /// 카메라/그리드가 준비되지 않은 경우에는 모드를 유지해 다음 클릭에서 다시 시도할 수 있게 합니다.
        /// </summary>
        private void TryCompleteThrowPotion()
        {
            if (_activeItem == null || _activeItem.ItemData is not IThrowable throwable)
            {
                CancelTargeting();
                return;
            }

            Camera cam = Camera.main;
            if (cam == null || GridSystem.Instance == null)
                return;

            // 마우스 화면 좌표를 월드 좌표로 바꾼 뒤, 프로젝트 공용 GridSystem으로 타일 좌표에 맞춥니다.
            Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int targetCell = GridSystem.Instance.WorldToGrid(worldPos);
            throwable.Throw(targetCell);
            // Throw 호출까지 성공한 시점에만 포션을 소비합니다.
            ConsumeActiveItem();
            ClearTargetingState();
        }

        /// <summary>
        /// 주문서 대상 아이템을 확정하고 적용합니다.
        /// 대상이 없거나 조건에 맞지 않으면 모드를 유지해서 다른 아이템을 다시 클릭할 수 있게 합니다.
        /// </summary>
        private void TryCompleteScrollTarget()
        {
            if (_activeItem == null || _activeItem.ItemData == null)
            {
                CancelTargeting();
                return;
            }

            InventoryItem target = FindTargetInventoryItem();
            if (target == null || target.ItemInstance == null)
                return;

            if (!CanUseScrollOnTarget(_scrollKind, target.ItemInstance))
                return;

            // 조건 검사는 이 컨트롤러에서 먼저 하고, 실제 상태 변경은 YJW Scroll의 Use(ItemInstance)에 위임합니다.
            _activeItem.ItemData.Use(target.ItemInstance);
            ConsumeActiveItem();
            ClearTargetingState();
        }

        /// <summary>
        /// 현재 마우스 아래의 대상 아이템을 찾습니다.
        /// 인벤토리 안 아이템을 우선하고, 없으면 장비 슬롯에 장착된 아이템을 찾습니다.
        /// </summary>
        private InventoryItem FindTargetInventoryItem()
        {
            InventoryItem inventoryTarget = FindInventoryItemUnderMouse();
            if (inventoryTarget != null && inventoryTarget != _activeItem)
                return inventoryTarget;

            return FindEquippedItemUnderMouse();
        }

        /// <summary>
        /// 인벤토리 아이템 UI 중 마우스 포인터 아래에 있는 대상을 찾습니다.
        /// 들고 있는 주문서 자신은 대상이 될 수 없으므로 제외합니다.
        /// </summary>
        private InventoryItem FindInventoryItemUnderMouse()
        {
            if (inventoryController == null)
                return null;

            InventoryItem result = null;
            foreach (InventoryItem item in inventoryController.Items)
            {
                if (item == null || item == _activeItem || !item.gameObject.activeInHierarchy)
                    continue;

                RectTransform rect = item.GetComponent<RectTransform>();
                if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition))
                    result = item;
            }

            return result;
        }

        /// <summary>
        /// 장비 슬롯 UI 중 마우스 포인터 아래에 있는 슬롯을 찾고, 그 슬롯에 장착된 InventoryItem을 반환합니다.
        /// 장착 장비도 주문서 대상으로 쓰기 위해 슬롯 View의 공개 Slot/SlotIndex 값을 사용합니다.
        /// </summary>
        private InventoryItem FindEquippedItemUnderMouse()
        {
            if (equipmentController == null)
                return null;

            NYHEquipmentSlotView[] slotViews = Resources.FindObjectsOfTypeAll<NYHEquipmentSlotView>();
            foreach (NYHEquipmentSlotView slotView in slotViews)
            {
                if (slotView == null || !slotView.gameObject.scene.IsValid() || !slotView.gameObject.activeInHierarchy)
                    continue;

                RectTransform rect = slotView.GetComponent<RectTransform>();
                if (rect == null || !RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition))
                    continue;

                if (equipmentController.TryGetEquippedInventoryItem(slotView.Slot, slotView.SlotIndex, out InventoryItem item))
                    return item;
            }

            return null;
        }

        /// <summary>
        /// 주문서 종류별 대상 조건을 검사합니다.
        /// 조건 실패 시 주문서를 소비하지 않기 위해 Use(ItemInstance) 호출 전에 여기서 먼저 걸러냅니다.
        /// </summary>
        private static bool CanUseScrollOnTarget(InventoryScrollTargetKind kind, ItemInstance target)
        {
			if (target is not EquipmentInstance equip) return false;
			if (equip.data == null) return false;

            return kind switch
            {
                InventoryScrollTargetKind.Analyze => equip.data.CanAnalyze && !equip.isAnalyzed,
                InventoryScrollTargetKind.Reinforce => equip.data.CanEnhance && equip.enhanceLevel < 15,
                InventoryScrollTargetKind.Purify => equip.data.CanCurse && equip.curse != null,
                _ => false
            };
        }

        /// <summary>
        /// 현재 사용 중인 포션/주문서를 인벤토리에서 제거합니다.
        /// 더블클릭 직후 손에 들고 있는 아이템이면 들고 있는 상태를 정리하고, 이미 그리드에 있으면 일반 제거 경로를 탑니다.
        /// </summary>
        private bool ConsumeActiveItem()
        {
            if (_activeItem == null || inventoryController == null)
                return false;

            if (inventoryController.SelectedItem == _activeItem)
                return inventoryController.TryConsumeHeldItem(_activeItem, true);

            return inventoryController.TryRemoveItem(_activeItem);
        }

        /// <summary>
        /// 대상 지정 모드를 취소합니다.
        /// 사용하려던 아이템을 아직 손에 들고 있다면 원래 칸으로 돌려놓고, 커서/상태를 기본값으로 되돌립니다.
        /// </summary>
        private void CancelTargeting()
        {
            if (inventoryController != null && inventoryController.SelectedItem == _activeItem)
                inventoryController.CancelDragAndReturnItem();

            ClearTargetingState();
        }

        /// <summary>
        /// 대상 지정 내부 상태와 커서를 초기화합니다.
        /// 성공/취소/오류 경로가 모두 같은 정리 코드를 타도록 한곳에 모았습니다.
        /// </summary>
        private void ClearTargetingState()
        {
            _mode = InventoryTargetingMode.None;
            _scrollKind = InventoryScrollTargetKind.None;
            _activeItem = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        /// <summary>
        /// 주문서 종류에 맞는 커서 텍스처를 반환합니다.
        /// 인스펙터에 텍스처가 비어 있으면 SetCursor에서 기본 커서를 유지합니다.
        /// </summary>
        private Texture2D GetCursorForScroll(InventoryScrollTargetKind kind)
        {
            return kind switch
            {
                InventoryScrollTargetKind.Analyze => analyzeCursor,
                InventoryScrollTargetKind.Reinforce => reinforceCursor,
                InventoryScrollTargetKind.Purify => purifyCursor,
                _ => null
            };
        }

        /// <summary>
        /// 커서 텍스처가 연결되어 있을 때만 Unity 커서를 변경합니다.
        /// 비어 있는 경우에는 기본 커서를 유지해 씬 세팅이 덜 되어도 동작이 깨지지 않게 합니다.
        /// </summary>
        private static void SetCursor(Texture2D cursor)
        {
            if (cursor == null)
            {
                Debug.LogWarning("InventoryTargetingController.SetCursor: cursor texture is null — 인스펙터 연결 확인 필요");
                return;
            }

            Debug.Log($"InventoryTargetingController.SetCursor: {cursor.name} 적용");
            Cursor.SetCursor(cursor, Vector2.zero, CursorMode.Auto);
        }

        /// <summary>
        /// 인스펙터 연결이 빠졌거나 런타임 자동 생성된 경우 필요한 참조를 보정합니다.
        /// 비활성 UI도 찾을 수 있어야 해서 일부 참조는 FindFirstObjectByType 기반으로 둡니다.
        /// </summary>
        private void ResolveReferences()
        {
            if (inventoryController == null)
                inventoryController = GetComponent<InventoryController>();

            if (inventoryPanelToggle == null)
                inventoryPanelToggle = FindFirstObjectByType<InventoryPanelToggle>();

            if (equipmentController == null)
                equipmentController = FindFirstObjectByType<NYHEquipmentController>();
        }
    }
}
