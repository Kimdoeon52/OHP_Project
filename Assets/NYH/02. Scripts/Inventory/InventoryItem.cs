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
        /// <summary>이 UI가 표시하는 아이템의 개별 상태입니다. 강화/저주/감정 여부는 여기에 저장됩니다.</summary>
        public ItemInstance ItemInstance { get; private set; }

        /// <summary>이 UI가 표시하는 아이템 데이터입니다.</summary>
        public ItemBase ItemData => ItemInstance != null ? ItemInstance.data : null;

        /// <summary>아이템이 배치된 좌상단 칸의 X 좌표입니다.</summary>
        public int OnGridPositionX { get; set; }

        /// <summary>아이템이 배치된 좌상단 칸의 Y 좌표입니다.</summary>
        public int OnGridPositionY { get; set; }

        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        /// <summary>
        /// ScriptableObject 원본 데이터만 있는 아이템을 UI에 연결합니다.
        /// 내부에서는 YJW ItemInstance를 새로 만들어 강화/저주/감정 상태를 담을 수 있게 합니다.
        /// </summary>
        //public void Set(ItemBase itemData, InventoryController inventoryController)
        //{
        //    Set(CreateDefaultInstance(itemData), inventoryController);
        //}

        /// <summary>
        /// 이미 생성된 YJW ItemInstance를 UI에 연결합니다.
        /// 드랍 아이템처럼 강화/저주/감정 상태가 있는 아이템은 이 경로로 들어와야 상태가 보존됩니다.
        /// </summary>
        public void Set(ItemInstance itemInstance, InventoryController inventoryController)
        {
            ItemInstance = itemInstance;
            ItemBase itemData = ItemData;
            if (itemData == null)
            {
                Debug.LogWarning("InventoryItem: ItemInstance에 ItemBase 데이터가 없어 표시할 수 없습니다.");
                return;
            }

            if (_image == null) _image = GetComponent<Image>();

            // ItemData의 아이콘을 그대로 UI Image에 연결합니다.
            // 아이콘이 아직 없는 테스트 데이터라면 이미지를 비활성화해서 빈 사각형 노출을 피합니다.
            _image.sprite = itemData.Icon;
            _image.enabled = itemData.Icon != null;
            _image.raycastTarget = true;
            _image.preserveAspect = true;

            // ItemData의 칸 크기와 UI 크기를 맞춰 배치 계산이 어긋나지 않게 합니다.
            RectTransform rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(itemData.Width * ItemGrid.TileWidth, itemData.Height * ItemGrid.TileHeight);
        }

        public void SetRaycastTarget(bool value)
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_image == null) return;

            // 손에 든 아이템이 raycast를 먹으면 장비 슬롯/퀵슬롯이 클릭을 받지 못한다.
            _image.raycastTarget = value;
        }

        /// <summary>
        /// ItemBase 종류에 맞는 YJW ItemInstance 구현체를 만듭니다.
        /// 장비/투척 무기는 전용 Instance가 데미지·방어력·투척 계산을 갖고 있으므로 가능한 한 구체 타입으로 생성합니다.
        /// </summary>
        //public static ItemInstance CreateDefaultInstance(ItemBase itemData)
        //{
        //    if (itemData == null)
        //        return null;

        //    ItemInstance instance = itemData switch
        //    {
        //        CloseWeapon => new WeaponInstance(),
        //        ArmorData => new ArmorInstance(),
        //        ThrowableWeapon => new ThrowableWeaponInstance(),
        //        _ => new ItemInstance()
        //    };

        //    instance.data = itemData;
        //    instance.enhanceLevel = 0;
        //    instance.curse = null;
        //    instance.isAnalyzed = false;
        //    return instance;
        //}
    }
}
