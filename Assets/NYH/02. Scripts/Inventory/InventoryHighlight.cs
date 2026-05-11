using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 인벤토리에서 마우스가 가리키는 아이템/배치 위치를 표시하는 하이라이트 UI입니다.
    /// 크기와 위치만 외부에서 받아 적용하므로, 실제 배치 규칙은 ItemGrid와 InventoryController에 남아 있습니다.
    /// </summary>
    public class InventoryHighlight : MonoBehaviour
    {
        [Tooltip("선택/배치 가능 영역을 보여줄 RectTransform입니다. 보통 테두리 이미지 하나를 연결합니다.")]
        [SerializeField] private RectTransform highlight;

        public void Show(bool value)
        {
            if (highlight != null) highlight.gameObject.SetActive(value);
        }

        public void UpdateSize(InventoryItem item)
        {
            if (highlight == null || item == null || item.ItemData == null) return;

            // 아이템이 차지하는 칸 수만큼 하이라이트 크기를 늘립니다.
            highlight.sizeDelta = new Vector2(item.ItemData.Width * ItemGrid.TileWidth, item.ItemData.Height * ItemGrid.TileHeight);
        }

        public void UpdatePosition(Vector2 localPos)
        {
            if (highlight != null) highlight.localPosition = localPos;
        }

        public void SetParent(ItemGrid grid)
        {
            if (highlight == null || grid == null) return;

            // 하이라이트를 현재 그리드 아래로 옮겨, 그리드 로컬 좌표로 바로 위치시킬 수 있게 합니다.
            highlight.SetParent(grid.GetComponent<RectTransform>(), false);
            highlight.SetAsLastSibling();
        }
    }
}
