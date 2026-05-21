using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace PrototypeRT
{
    /// <summary>
    /// 인벤토리 안에서 보이는 아이템 UI 한 개를 표현합니다.
    /// 실제 아이템 수치/크기/아이콘은 ItemData가 가지고, 이 클래스는 화면 표시와 그리드 위치만 들고 있습니다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class InventoryItem : MonoBehaviour
    {
        /// <summary>이 UI가 표시하는 아이템 데이터입니다.</summary>
        public ItemBase ItemData { get; private set; }

        /// <summary>아이템이 배치된 좌상단 칸의 X 좌표입니다.</summary>
        public int OnGridPositionX { get; set; }

        /// <summary>아이템이 배치된 좌상단 칸의 Y 좌표입니다.</summary>
        public int OnGridPositionY { get; set; }

        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        public void Set(ItemBase itemData, InventoryController inventoryController)
        {
            ItemData = itemData;

            if (_image == null) _image = GetComponent<Image>();

            // ItemData의 아이콘을 그대로 UI Image에 연결합니다.
            // 아이콘이 아직 없는 테스트 데이터라면 이미지를 비활성화해서 빈 사각형 노출을 피합니다.
            _image.sprite = itemData.Icon;
            _image.enabled = itemData.Icon != null;
            _image.raycastTarget = true;

            // ItemData의 칸 크기와 UI 크기를 맞춰 배치 계산이 어긋나지 않게 합니다.
            RectTransform rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(itemData.Width * ItemGrid.TileWidth, itemData.Height * ItemGrid.TileHeight);
        }
    }
}
