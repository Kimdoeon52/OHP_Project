using UnityEngine;

// 상자 데이터
[CreateAssetMenu(fileName = "ChestData", menuName = "Item/Chest")]
public class ChestData : ScriptableObject
{
    public enum ChestKind { Random, Gold, Equipment, Ring }

    [Header("상자 종류")]
    [SerializeField] private ChestKind kind = ChestKind.Random;
    [SerializeField] private string chestName;

    [Header("개방에 걸리는 시간")]
    [SerializeField, Min(0f)] private float openSeconds = 1f;
    [Tooltip("열쇠 필요 여부")]
    [SerializeField] private bool requiresKey = false;
    [SerializeField] private KeyType keyType = KeyType.RingChest;

    [Header("아이템 보상")]
    [SerializeField] private ItemCategory[] itemCategories;
    [SerializeField, Min(0)] private int itemDropMin = 1;
    [SerializeField, Min(0)] private int itemDropMax = 1;
    [Tooltip("이 상자에서 제외할 아이템 ID")]
    [SerializeField] private int[] excludeItemIds = { 1002, 2003 };

    [Header("골드 보상 (골드 상자)")]
    [SerializeField, Min(0)] private int goldMin = 0;
    [SerializeField, Min(0)] private int goldMax = 0;

    [Header("스프라이트")]
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openedSprite;

    public ChestKind Kind => kind;
    public string ChestName => chestName;
    public float OpenSeconds => openSeconds;
    public bool RequiresKey => requiresKey;
    public KeyType KeyType => keyType;
    public ItemCategory[] ItemCategories => itemCategories;
    public int ItemDropMin => itemDropMin;
    public int ItemDropMax => itemDropMax;
    public int[] ExcludeItemIds => excludeItemIds;
    public int GoldMin => goldMin;
    public int GoldMax => goldMax;
    public Sprite ClosedSprite => closedSprite;
    public Sprite OpenedSprite => openedSprite;
}
