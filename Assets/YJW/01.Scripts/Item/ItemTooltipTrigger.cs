using PrototypeRT;
using UnityEngine;
using UnityEngine.EventSystems;

// InventoryItem과 같은 GO에 붙여서 마우스 hover 시 툴팁을 표시
[RequireComponent(typeof(InventoryItem))]
public class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private InventoryItem _inventoryItem;

    private void Awake()
    {
        _inventoryItem = GetComponent<InventoryItem>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("툴팁 OnPointerEnter 호출됨");

        if (_inventoryItem.ItemInstance == null)
        {
            Debug.Log("ItemInstance가 null");
            return;
        }

        string text = _inventoryItem.ItemInstance.GetToolTipStats();
        Debug.Log($"툴팁 텍스트: '{text}'");
        ToolTipController.Instance.Show(text);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ToolTipController.Instance.Hide();
    }
}
