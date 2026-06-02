using UnityEngine;
using UnityEngine.UI;

namespace PrototypeRT
{
    /// <summary>
    /// 인벤토리 안에서 보이는 아이템 UI 한 개를 표현합니다.
    /// 실제 아이템 수치/크기/아이콘은 ItemData가 가지고, 이 클래스는 화면 표시와 그리드 위치만 들고 있습니다.
    /// 
    /// 인벤토리 슬롯에 그려질 단일 아이템의 시각적 요소를 담당합니다.
    /// 공격력 등의 원본 데이터는 ItemData 인스턴스가 보관하며, 본 스크립트는 
    /// 해당 아이템이 인벤토리의 어느 좌표에 어떤 크기의 이미지로 출력될지만 집중적으로 관리합니다.
    /// </summary>
    [RequireComponent(typeof(Image))] // 이 스크립트가 제대로 작동하려면 화면에 그림을 그리기 위한 Image 컴포넌트가 반드시 필요하다는 뜻입니다.
    public class InventoryItem : MonoBehaviour
    {
        /// <summary>
        /// 이 UI가 표시하는 아이템의 개별 상태입니다. 강화/저주/감정 여부는 여기에 저장됩니다.
        /// 
        /// 동일한 아이템이라도 개별적으로 부여된 강화 수치나 저주 상태 등 
        /// 해당 객체만의 고유한 런타임 상태 데이터를 보관하는 속성입니다.
        /// </summary>
        public ItemInstance ItemInstance { get; private set; }

        /// <summary>
        /// 이 UI가 표시하는 아이템 데이터입니다.
        /// 
        /// ItemInstance에 포함된 아이템의 원본(Base) 데이터를 즉시 반환하는 편의용 프로퍼티입니다.
        /// </summary>
        public ItemBase ItemData => ItemInstance != null ? ItemInstance.data : null;

        /// <summary>
        /// 아이템이 배치된 좌상단 칸의 X 좌표입니다.
        /// 
        /// 인벤토리 그리드 상에서 이 아이템이 차지하는 영역의 가로축 시작 인덱스(0부터 시작)를 나타냅니다.
        /// </summary>
        public int OnGridPositionX { get; set; }

        /// <summary>
        /// 아이템이 배치된 좌상단 칸의 Y 좌표입니다.
        /// 
        /// 인벤토리 그리드 상에서 이 아이템이 차지하는 영역의 세로축 시작 인덱스(0부터 시작)를 나타냅니다.
        /// </summary>
        public int OnGridPositionY { get; set; }

        // 유니티에서 이미지를 화면에 띄워주기 위해 사용하는 도구입니다.
        private Image _image;

        // 게임이 실행되고 이 아이템이 처음 만들어질 때 딱 한 번 자동으로 실행되는 함수입니다.
        private void Awake()
        {
            // 자신에게 붙어있는 Image 컴포넌트를 찾아서 _image 변수에 저장해둡니다. 나중에 써먹기 위해서입니다.
            _image = GetComponent<Image>();
        }

        /// <summary>
        /// 이미 생성된 ItemInstance를 UI에 연결합니다.
        /// 드랍 아이템처럼 강화/저주/감정 상태가 있는 아이템은 이 경로로 들어와야 상태가 보존됩니다.
        /// 
        /// 획득한 아이템의 데이터를 기반으로 UI 아이콘과 크기를 초기화하는 역할을 수행합니다.
        /// </summary>
        public void Set(ItemInstance itemInstance, InventoryController inventoryController)
        {
            // 받아온 아이템 상태 정보를 내 것으로 저장합니다.
            ItemInstance = itemInstance;
            
            // 그 상태 정보에서 원본 아이템 데이터가 뭔지 빼옵니다.
            ItemBase itemData = ItemData;
            
            // 만약 원본 데이터가 비어있다면 화면에 그릴 게 없으므로 에러 메시지를 띄우고 함수를 끝냅니다.
            if (itemData == null)
            {
                Debug.LogWarning("InventoryItem: ItemInstance에 ItemBase 데이터가 없어 표시할 수 없습니다.");
                return;
            }

            // 만약 아까 저장해둔 _image가 어쩌다 보니 비어있다면 다시 한번 찾아줍니다.
            if (_image == null) _image = GetComponent<Image>();

            // ItemData의 아이콘을 그대로 UI Image에 연결합니다.
            // 아이콘이 아직 없는 테스트 데이터라면 이미지를 비활성화해서 빈 사각형 노출을 피합니다.
            _image.sprite = itemData.Icon; // 원본 데이터에 있는 아이콘 그림을 내 이미지로 설정합니다.
            _image.enabled = itemData.Icon != null; // 그림이 있으면 화면에 보이고, 없으면 숨깁니다.
            _image.raycastTarget = true; // 마우스 클릭을 감지할 수 있도록 켜줍니다.
            _image.preserveAspect = true; // 그림이 찌그러지지 않고 원본 비율을 유지하도록 합니다.

            // ItemData의 칸 크기와 UI 크기를 맞춰 배치 계산이 어긋나지 않게 합니다.
            // 
            // 아이템의 타일 크기(예: 1x1, 2x2)에 맞춰 실제 RectTransform의 화면상 픽셀 크기를 동기화합니다.
            RectTransform rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(itemData.Width * ItemGrid.TileWidth, itemData.Height * ItemGrid.TileHeight);
        }

        /// <summary>
        /// 이 아이템 UI가 마우스 클릭을 막을지(true) 통과시킬지(false) 결정하는 함수입니다.
        /// </summary>
        public void SetRaycastTarget(bool value)
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_image == null) return;

            // 손에 든 아이템이 마우스 클릭을 먹으면(가로채면), 그 밑에 있는 장비 슬롯이나 퀵슬롯이 클릭을 받지 못합니다.
            // 그래서 마우스로 아이템을 집어 들었을 때는 클릭을 통과시키게 끄고(false), 다시 내려놨을 때는 켜줍니다(true).
            _image.raycastTarget = value;
        }

    }
}
