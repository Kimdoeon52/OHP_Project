using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 인벤토리 아이템 더블클릭 사용 흐름을 담당합니다.
    /// InventoryController는 클릭/드래그만 관리하고, 아이템 종류별 사용 분기는 이 클래스에 모읍니다.
    /// 
    /// 인벤토리 내에서 아이템을 더블클릭했을 때, 해당 아이템의 유형(포션, 장비, 주문서 등)을 판별하여 
    /// 각 아이템 성격에 맞는 후속 처리(즉시 사용, 장착, 대상 지정 모드 전환)를 분기해주는 컨트롤러입니다.
    /// </summary>
    public class InventoryItemUseController : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("아이템을 들고 있는 상태와 인벤토리 배치를 관리하는 컨트롤러입니다.")]
        [SerializeField] private InventoryController inventoryController;

        [Tooltip("투척 포션과 대상 지정 주문서의 후속 클릭 처리를 담당하는 컨트롤러입니다.")]
        [SerializeField] private InventoryTargetingController targetingController;

        [Tooltip("더블클릭 장비 자동 장착에 사용할 장비 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private NYHEquipmentController equipmentController;

        /// <summary>
        /// InventoryController가 런타임에 자동으로 붙인 경우 참조를 주입할 때 사용합니다.
        /// 씬에 직접 배치해도 되고, 자동 생성되어도 같은 초기화 경로를 타게 합니다.
        /// 
        /// 게임 초기화 시점에 인벤토리 조작에 필요한 핵심 컨트롤러(InventoryController 등) 간의 
        /// 의존성을 안전하게 주입하고 연결 상태를 점검하는 역할을 합니다.
        /// </summary>
        public void Configure(InventoryController inventory, InventoryTargetingController targeting)
        {
            inventoryController = inventory;
            targetingController = targeting;
            ResolveReferences();
        }

        /// <summary>
        /// 더블클릭된 아이템을 종류별 사용 흐름으로 보냅니다.
        /// 이 함수는 실제 클릭 판정은 하지 않고, 이미 "사용하려는 아이템"으로 확정된 InventoryItem만 처리합니다.
        /// 
        /// 전달된 아이템의 ItemBase 데이터를 분석하여, 소비형 아이템은 즉시 효과를 적용하고, 
        /// 장비는 자동 장착 프로세스로 넘기며, 대상 지정이 필요한 투척/주문서류는 타겟팅 모드로 진입시킵니다.
        /// </summary>
        public bool TryUseDoubleClickedItem(InventoryItem item)
        {
            ResolveReferences();

            if (item == null || item.ItemData == null || inventoryController == null)
                return false;

            if (targetingController != null && targetingController.IsTargeting)
                return false;

            ItemBase itemData = item.ItemData;

            if (itemData is ThrowablePotion)
                return targetingController != null && targetingController.BeginThrowPotion(item);

            if (itemData is PotionData)
                return UseImmediateItem(item);

            if (IsEquipment(itemData))
                return TryAutoEquip(item);

            if (itemData is AnalyzeScroll)
                return targetingController != null && targetingController.BeginTargetScroll(item, InventoryScrollTargetKind.Analyze);

            if (itemData is ReinforceScroll)
                return targetingController != null && targetingController.BeginTargetScroll(item, InventoryScrollTargetKind.Reinforce);

            if (itemData is PurifyScroll)
                return targetingController != null && targetingController.BeginTargetScroll(item, InventoryScrollTargetKind.Purify);

            if (itemData.ItemType == ItemType.Scroll)
                return UseImmediateItem(item);

            return false;
        }

        /// <summary>
        /// 대상 선택 없이 바로 효과가 나는 아이템을 사용하고 손에 든 아이템 UI를 제거합니다.
        /// 
        /// 즉시 회복 포션이나 지도 스크롤처럼 추가적인 대상 지정이 필요 없는 아이템의 효과(Use)를 즉각 발동시키고,
        /// 성공 시 인벤토리 그리드 목록에서 해당 아이템을 완전히 소모 처리합니다.
        /// </summary>
        private bool UseImmediateItem(InventoryItem item)
        {
            item.ItemData.Use();
            return inventoryController.TryConsumeOneItem(item);
        }

        /// <summary>
        /// 더블클릭한 장비를 자동 장착합니다.
        /// 기존 장비가 있다면 먼저 그 장비가 인벤토리로 돌아갈 공간이 있는지 확인해, 교체 중 장비가 사라지는 일을 막습니다.
        /// 
        /// 타겟 슬롯에 이미 착용 중인 장비가 있을 경우, 인벤토리에 여유 공간이 충분한지 사전 검증합니다.
        /// 교체가 안전하다고 판단될 때만 장비를 스왑하여 아이템이 증발하는 논리적 오류를 방지합니다.
        /// </summary>
        private bool TryAutoEquip(InventoryItem item)
        {
            if (equipmentController == null)
                return false;

            if (!equipmentController.TryGetTargetSlotFor(item.ItemData, out EquipmentSlot slot, out int slotIndex))
                return false;

            if (equipmentController.TryGetEquippedInventoryItem(slot, slotIndex, out InventoryItem replacedItem)
                && replacedItem != null
                && !inventoryController.CanPlaceInDefaultGrid(replacedItem))
            {
                Debug.LogWarning("InventoryItemUseController: 교체된 장비를 넣을 인벤토리 빈칸이 없어 장착을 취소합니다.");
                return false;
            }

            if (!equipmentController.TryEquipFromInventory(item, slot, slotIndex, out replacedItem))
                return false;

            if (!inventoryController.TryConsumeHeldItem(item, false))
            {
                equipmentController.Unequip(slot, slotIndex, out _);
                return false;
            }

            item.gameObject.SetActive(false);

            if (replacedItem != null && !inventoryController.TryPlaceInDefaultGrid(replacedItem))
            {
                inventoryController.HoldItem(replacedItem, null, 0, 0);
            }

            return true;
        }

        /// <summary>
        /// 현재 아이템을 장비로 볼 수 있는지 판별합니다.
        /// 
        /// 해당 아이템이 플레이어의 슬롯(머리, 무기, 방어구 등)에 장착 가능한 Equipment 타입인지 데이터 레벨에서 검사합니다.
        /// </summary>
        private static bool IsEquipment(ItemBase itemData)
        {
            if (itemData == null)
                return false;

            // 타입이 아예 Equipment라고 적혀 있거나, 혹은 들어갈 슬롯(머리, 무기 등)이 지정되어 있다면 장비로 인정합니다.
            return itemData.ItemType == ItemType.Equipment || itemData.Slot != EquipmentSlot.None;
        }

        /// <summary>
        /// 인스펙터 연결이 빠졌거나 런타임 자동 생성된 경우 필요한 참조를 씬에서 보정합니다.
        /// 혹시라도 게임 개발자가 컨트롤러들을 깜빡하고 연결 안 해놨을까 봐, 스스로 씬 전체를 뒤져서 필요한 시스템들을 찾아 연결하는 방어용 함수입니다.
        /// </summary>
        private void ResolveReferences()
        {
            // 만약 못 찾은 컨트롤러가 있으면 GetComponent(자신에게 붙어있는지 확인)나 FindFirstObjectByType(게임 전체에서 찾기)으로 연결해 줍니다.
            if (inventoryController == null)
                inventoryController = GetComponent<InventoryController>();

            if (targetingController == null)
                targetingController = GetComponent<InventoryTargetingController>();

            if (equipmentController == null)
                equipmentController = FindFirstObjectByType<NYHEquipmentController>();
        }
    }
}
