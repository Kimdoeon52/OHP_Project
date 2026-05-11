using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 아이템의 큰 분류입니다.
    /// 장착/소비/판매 같은 시스템에서 1차 필터로 사용합니다.
    /// </summary>
    public enum ItemType
    {
        Loot,      // 전리품
        Weapon,    // 무기
        Armor,     // 갑옷
        Consumable // 소모품
    }

    /// <summary>
    /// 장착 가능한 아이템이 들어갈 슬롯입니다.
    /// ItemType이 Weapon이어도 EquipmentSlot이 Weapon이 아니면 장착 로직에서 제외될 수 있습니다.
    /// </summary>
    public enum EquipmentSlot
    {
        None,          // 장착 불가
        Weapon,        // 무기
        Armor,         // 갑옷
        Ring,          // 반지
        Artifact,      // 유물
        Rune           // 룬
    }

    /// <summary>
    /// 아이템 하나의 정적 데이터를 담는 ScriptableObject입니다.
    /// 인벤토리 UI 크기, 아이콘, 분류, 기본 수치를 코드 수정 없이 인스펙터에서 조정할 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData", menuName = "Prototype RT/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("인벤토리와 로그에서 사용할 아이템 이름입니다.")]
        [SerializeField] private string itemName = "New Item";

        [Tooltip("인벤토리에서 차지하는 가로 칸 수입니다.")]
        [SerializeField, Min(1)] private int width = 1;

        [Tooltip("인벤토리에서 차지하는 세로 칸 수입니다.")]
        [SerializeField, Min(1)] private int height = 1;

        [Header("이미지")]
        [Tooltip("인벤토리와 월드 드랍에 기본으로 표시할 아이콘입니다.")]
        [SerializeField] private Sprite itemIcon;

        [Tooltip("무기로 장착했을 때 플레이어 손에 표시할 스프라이트입니다. 비워두면 Item Icon을 대신 사용합니다.")]
        [SerializeField] private Sprite weaponWorldSprite;

        [Header("아이템 분류")]
        [Tooltip("아이템의 종류입니다. 장착 가능한 무기는 Weapon으로 설정해야 합니다.")]
        [SerializeField] private ItemType itemType = ItemType.Loot;

        [Tooltip("장착 슬롯입니다. 무기로 장착하려면 Weapon으로 설정해야 합니다.")]
        [SerializeField] private EquipmentSlot equipmentSlot = EquipmentSlot.None;

        [Header("수치")]
        [Tooltip("판매 시 얻는 골드 값입니다. 판매 시스템 테스트에 사용됩니다.")]
        [SerializeField, Min(0)] private int sellPrice = 1;

        [Tooltip("무기로 장착했을 때 기본 공격력에 더해지는 값입니다.")]
        [SerializeField] private int attackBonus;

        public string ItemName => itemName;
        public int Width => width;
        public int Height => height;
        public Sprite ItemIcon => itemIcon;
        public Sprite WeaponWorldSprite => weaponWorldSprite;
        public ItemType ItemType => itemType;
        public EquipmentSlot EquipmentSlot => equipmentSlot;
        public int SellPrice => sellPrice;
        public int AttackBonus => attackBonus;

        /// <summary>
        /// 월드에 표시할 스프라이트를 반환합니다.
        /// 무기 전용 스프라이트가 없으면 인벤토리 아이콘을 대신 사용해 테스트 데이터도 바로 보이게 합니다.
        /// </summary>
        public Sprite GetWorldSprite()
        {
            // 인벤토리 아이콘과 손에 든 무기 이미지를 분리하되, 테스트 데이터가 덜 채워져도 바로 볼 수 있게 아이콘을 fallback으로 쓴다.
            return weaponWorldSprite != null ? weaponWorldSprite : itemIcon;
        }
    }
}
