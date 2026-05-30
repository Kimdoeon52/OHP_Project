using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 인벤토리 아이템 더블클릭 사용 흐름을 담당합니다.
    /// InventoryController는 클릭/드래그만 관리하고, 아이템 종류별 사용 분기는 이 클래스에 모읍니다.
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
        /// 아이템 종류에따라 사용 처리 분기
        /// </summary>
        public bool TryUseDoubleClickedItem(InventoryItem item)
        {
            ResolveReferences();

            // ItemBase가 없으면 포션/장비/스크롤 판별 자체가 불가능하므로 실패 처리합니다.
            if (item == null || item.ItemData == null || inventoryController == null)
                return false;

            // 이미 주문서 대상 지정이나 포션 조준 중이면 새 사용 흐름이 겹치지 않게 막습니다.
            if (targetingController != null && targetingController.IsTargeting)
                return false;

            ItemBase itemData = item.ItemData;

            // 던지는 포션은 즉시 Use()하지 않고, 다음 좌클릭으로 맵 타일을 고르는 모드로 넘깁니다.
            if (itemData is ThrowablePotion)
                return targetingController != null && targetingController.BeginThrowPotion(item);

            // 일반 포션은 현재 ItemBase.Use()가 즉시 효과를 적용하는 구조라 바로 사용하고 소비합니다.
            if (itemData is PotionData)
                return UseImmediateItem(item);

            // 장비는 Use()가 아니라 장비 컨트롤러를 통해 조건 검사/교체/스탯 적용을 해야 합니다.
            if (IsEquipment(itemData))
                return TryAutoEquip(item);

            // 대상 지정 주문서는 즉시 소비하지 않고, 다음 좌클릭으로 적용 대상 아이템을 고릅니다.
            if (itemData is AnalyzeScroll)
                return targetingController != null && targetingController.BeginTargetScroll(item, InventoryScrollTargetKind.Analyze);

            if (itemData is ReinforceScroll)
                return targetingController != null && targetingController.BeginTargetScroll(item, InventoryScrollTargetKind.Reinforce);

            if (itemData is PurifyScroll)
                return targetingController != null && targetingController.BeginTargetScroll(item, InventoryScrollTargetKind.Purify);

            // 지도 스크롤처럼 대상이 필요 없는 스크롤은 ItemBase.Use()만 호출하는 즉시 사용 흐름으로 둡니다.
            if (itemData.ItemType == ItemType.Scroll)
                return UseImmediateItem(item);

            return false;
        }

        /// <summary>
        /// 대상 선택 없이 바로 효과가 나는 아이템을 사용하고 손에 든 아이템 UI를 제거합니다.
        /// 현재 ItemBase.Use()가 성공 여부를 반환하지 않으므로, 호출 후 소비하는 흐름입니다.
        /// 여기서 실제로 사용 을 호출함
        /// 단 ThrowablePotion은 USE를 바로 호출 안하고 BeginThrowPotion() 에서 조준 모드로 들어감
        /// </summary>
        private bool UseImmediateItem(InventoryItem item)
        {
            item.ItemData.Use(); //여기서 사용 호출
            return inventoryController.TryConsumeHeldItem(item, true);
        }

        /// <summary>
        /// 더블클릭한 장비를 자동 장착합니다.
        /// 기존 장비가 있다면 먼저 그 장비가 인벤토리로 돌아갈 공간이 있는지 확인해, 교체 중 장비가 사라지는 일을 막습니다.
        /// </summary>
        private bool TryAutoEquip(InventoryItem item)
        {
            if (equipmentController == null)
                return false;

            // 장비 종류에 맞는 슬롯과 반지 index 등을 장비 컨트롤러 기준으로 결정합니다.
            if (!equipmentController.TryGetTargetSlotFor(item.ItemData, out EquipmentSlot slot, out int slotIndex))
                return false;

            // 교체 장비를 넣을 공간이 없으면 장착 자체를 하지 않습니다.
            // 그래야 새 장비만 장착되고 기존 장비가 갈 곳을 잃는 상황을 피합니다.
            if (equipmentController.TryGetEquippedInventoryItem(slot, slotIndex, out InventoryItem replacedItem)
                && replacedItem != null
                && !inventoryController.CanPlaceInDefaultGrid(replacedItem))
            {
                Debug.LogWarning("InventoryItemUseController: 교체된 장비를 넣을 인벤토리 빈칸이 없어 장착을 취소합니다.");
                return false;
            }

            if (!equipmentController.TryEquipFromInventory(item, slot, slotIndex, out replacedItem))
                return false;

            // 새 장비는 장착 상태로 넘어갔으므로 인벤토리 소유 목록에서는 빼되, 장비 컨트롤러가 참조해야 하니 파괴하지 않습니다.
            if (!inventoryController.TryConsumeHeldItem(item, false))
            {
                // 손 상태 정리에 실패하면 장착만 된 상태가 남지 않도록 방금 장착한 슬롯을 되돌립니다.
                equipmentController.Unequip(slot, slotIndex, out _);
                return false;
            }

            // 장착 아이콘은 장비 슬롯 View가 표시하므로, 원래 인벤토리 아이템 UI는 숨겨 보관합니다.
            item.gameObject.SetActive(false);

            if (replacedItem != null && !inventoryController.TryPlaceInDefaultGrid(replacedItem))
            {
                // 사전 검사 후에도 실패한 예외 상황에서는 장비가 사라지지 않게 손에 들려 둡니다.
                inventoryController.HoldItem(replacedItem, null, 0, 0);
            }

            return true;
        }

        /// <summary>
        /// 현재 아이템을 장비로 볼 수 있는지 판별합니다.
        /// SO의 ItemType이 누락되어도 Slot 값이 있으면 장비로 처리해 데이터 설정 실수를 일부 흡수합니다.
        /// </summary>
        private static bool IsEquipment(ItemBase itemData)
        {
            if (itemData == null)
                return false;

            return itemData.ItemType == ItemType.Equipment || itemData.Slot != EquipmentSlot.None;
        }

        /// <summary>
        /// 인스펙터 연결이 빠졌거나 런타임 자동 생성된 경우 필요한 참조를 씬에서 보정합니다.
        /// 매 사용 시 다시 호출해도 이미 값이 있으면 비용이 거의 없습니다.
        /// </summary>
        private void ResolveReferences()
        {
            if (inventoryController == null)
                inventoryController = GetComponent<InventoryController>();

            if (targetingController == null)
                targetingController = GetComponent<InventoryTargetingController>();

            if (equipmentController == null)
                equipmentController = FindFirstObjectByType<NYHEquipmentController>();
        }
    }
}
