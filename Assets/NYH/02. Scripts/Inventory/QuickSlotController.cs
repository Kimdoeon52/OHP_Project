using System;
using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 10칸 퀵슬롯 그리드를 관리하고 1~0 키 입력으로 아이템을 사용합니다.
    /// 퀵슬롯에 들어간 아이템은 일반 인벤토리에서 빠져 실제로 이 그리드에 배치된 InventoryItem입니다.
    /// </summary>
    public class QuickSlotController : MonoBehaviour
    {
        private const int SlotCount = 10;

        [Header("Quick Slot Grid")]
        [Tooltip("10x1 크기의 퀵슬롯 전용 ItemGrid입니다. ItemGrid의 Accept Only One By One Items 옵션을 켜야 합니다.")]
        [SerializeField] private ItemGrid quickSlotGrid;

        [Tooltip("켜면 시작 시 quickSlotGrid를 10x1 크기로 맞춥니다.")]
        [SerializeField] private bool configureGridSize = true;

        [Header("Action Delay")]
        [Tooltip("Safe Room 밖에서 퀵슬롯 아이템을 사용한 뒤 다음 행동까지 막는 시간입니다.")]
        [SerializeField, Min(0f)] private float useActionDelay = 1f;

        [Tooltip("플레이어가 소유한 InventoryItem 목록을 관리하는 인벤토리 컨트롤러입니다. 사용한 아이템을 목록에서도 제거합니다.")]
        [SerializeField] private InventoryController inventoryController;

        private GridPlayer _player;

        public event Action OnQuickSlotsChanged;

        public ItemGrid QuickSlotGrid => quickSlotGrid;

        private void Awake()
        {
            if (quickSlotGrid == null)
                quickSlotGrid = FindQuickSlotGrid();

            if (configureGridSize && quickSlotGrid != null)
                quickSlotGrid.ConfigureSize(SlotCount, 1);

            if (inventoryController == null)
                inventoryController = FindFirstObjectByType<InventoryController>();

            _player = FindFirstObjectByType<GridPlayer>();
        }

        private void OnEnable()
        {
            if (quickSlotGrid != null)
                quickSlotGrid.OnGridChanged += RaiseQuickSlotsChanged;

            RaiseQuickSlotsChanged();
        }

        private void OnDisable()
        {
            if (quickSlotGrid != null)
                quickSlotGrid.OnGridChanged -= RaiseQuickSlotsChanged;
        }

        private void Update()
        {
            int index = GetPressedSlotIndex();
            if (index >= 0)
                TryUseSlot(index);
        }

        public InventoryItem GetItem(int slotIndex)
        {
            if (quickSlotGrid == null || slotIndex < 0 || slotIndex >= SlotCount)
                return null;

            return quickSlotGrid.GetItemAt(slotIndex, 0);
        }

        public bool TryUseSlot(int slotIndex)
        {
            InventoryItem item = GetItem(slotIndex);
            if (item == null || item.ItemData == null) return false;

            if (!CanUseQuickSlot())
                return false;

            item.ItemData.Use();
            inventoryController?.ForgetItem(item);
            quickSlotGrid.RemoveItem(item);
            ApplyUseDelay();
            RaiseQuickSlotsChanged();
            return true;
        }

        private bool CanUseQuickSlot()
        {
            if (_player == null)
                _player = FindFirstObjectByType<GridPlayer>();

            if (_player == null || _player.Health == null || _player.Health.IsDead)
                return false;

            return _player.ActionDelay == null || _player.ActionDelay.CanAct;
        }

        private void ApplyUseDelay()
        {
            if (_player == null || _player.ActionDelay == null || useActionDelay <= 0f)
                return;

            if (TimeSystem.Instance != null && !TimeSystem.Instance.IsRunning)
                return;

            _player.ActionDelay.StartDelay(useActionDelay);
        }

        private static int GetPressedSlotIndex()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
            if (Input.GetKeyDown(KeyCode.Alpha4)) return 3;
            if (Input.GetKeyDown(KeyCode.Alpha5)) return 4;
            if (Input.GetKeyDown(KeyCode.Alpha6)) return 5;
            if (Input.GetKeyDown(KeyCode.Alpha7)) return 6;
            if (Input.GetKeyDown(KeyCode.Alpha8)) return 7;
            if (Input.GetKeyDown(KeyCode.Alpha9)) return 8;
            if (Input.GetKeyDown(KeyCode.Alpha0)) return 9;

            return -1;
        }

        private static ItemGrid FindQuickSlotGrid()
        {
            ItemGrid[] grids = Resources.FindObjectsOfTypeAll<ItemGrid>();
            foreach (ItemGrid grid in grids)
            {
                if (grid == null || !grid.gameObject.scene.IsValid()) continue;
                if (grid.AcceptOnlyOneByOneItems) return grid;
            }

            return null;
        }

        private void RaiseQuickSlotsChanged()
        {
            OnQuickSlotsChanged?.Invoke();
        }
    }
}
