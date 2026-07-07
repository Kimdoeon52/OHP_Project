using UnityEngine;
using UnityEngine.UI;

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

        private void Awake()
        {
            DisableHighlightRaycast();
        }

        public void Show(bool value)
        {
            if (highlight == null) return;

            highlight.gameObject.SetActive(value);
            if (value)
                MoveBehindItems();
        }

        public void UpdateSize(InventoryItem item)
        {
            if (highlight == null || item == null || item.ItemData == null) return;

            // 아이템이 차지하는 칸 수만큼 하이라이트 크기를 늘립니다. (회전 반영 크기, 하이라이트는 축 정렬)
            highlight.sizeDelta = new Vector2(item.RotatedWidth * ItemGrid.TileWidth, item.RotatedHeight * ItemGrid.TileHeight);
            MoveBehindItems();
        }

        public void UpdatePosition(Vector2 localPos)
        {
            if (highlight == null) return;

            highlight.localPosition = localPos;
            MoveBehindItems();
        }

        public void SetParent(ItemGrid grid)
        {
            if (highlight == null || grid == null) return;

            DisableHighlightRaycast();

            // 하이라이트를 현재 그리드 아래로 옮겨, 그리드 로컬 좌표로 바로 위치시킬 수 있게 합니다.
            highlight.SetParent(grid.GetComponent<RectTransform>(), false);
            MoveBehindItems();
        }

        private void MoveBehindItems()
        {
            if (highlight == null || highlight.parent == null) return;

            Transform gridTransform = highlight.parent;
            int targetIndex = 0;

            for (int i = 0; i < gridTransform.childCount; i++)
            {
                Transform child = gridTransform.GetChild(i);
                if (child == highlight) continue;

                if (child.GetComponent<InventoryItem>() != null)
                {
                    targetIndex = i;
                    break;
                }

                targetIndex = i + 1;
            }

            // 아이템 아이콘을 어둡게 덮지 않도록 하이라이트는 첫 아이템 바로 뒤에 둡니다.
            // 아직 아이템이 없는 빈 그리드라면 슬롯 배경 위에 보이도록 마지막에 둡니다.
            highlight.SetSiblingIndex(Mathf.Clamp(targetIndex, 0, gridTransform.childCount - 1));
            BringItemsToFront(gridTransform);
        }

        private static void BringItemsToFront(Transform gridTransform)
        {
            if (gridTransform == null) return;

            for (int i = gridTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = gridTransform.GetChild(i);
                if (child != null && child.GetComponent<InventoryItem>() != null)
                    child.SetAsLastSibling();
            }
        }

        private void DisableHighlightRaycast()
        {
            if (highlight == null) return;

            Graphic[] graphics = highlight.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic != null)
                    graphic.raycastTarget = false;
            }
        }
    }
}
