using UnityEngine;
using UnityEngine.UI;

namespace PrototypeRT
{
    [RequireComponent(typeof(Image))]
    public class InventoryItem : MonoBehaviour
    {
        public ItemData ItemData { get; private set; }
        public int OnGridPositionX { get; set; }
        public int OnGridPositionY { get; set; }

        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        public void Set(ItemData itemData, InventoryController inventoryController)
        {
            ItemData = itemData;

            if (_image == null) _image = GetComponent<Image>();
            _image.sprite = itemData.ItemIcon;
            _image.enabled = itemData.ItemIcon != null;
            _image.raycastTarget = true;

            // 아이템 데이터의 칸 크기와 UI 크기를 맞춰 배치 계산이 어긋나지 않게 한다.
            RectTransform rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(itemData.Width * ItemGrid.TileWidth, itemData.Height * ItemGrid.TileHeight);
        }
    }
}
