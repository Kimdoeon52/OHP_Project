using UnityEngine;

/// <summary>
/// 상자 한 종류의 규칙 정의 SO (GDD 9.14).
/// 4종(랜덤/골드/장비/반지)은 클래스가 아니라 이 SO 인스턴스의 파라미터로 분기한다.
/// 새 상자 = SO 1개 추가 (코드 수정 X).
/// 전리품은 YJW ID 구간 기반 <see cref="ItemCategory"/> 로 풀에서 필터링한다.
/// </summary>
[CreateAssetMenu(fileName = "ChestData", menuName = "Item/Chest")]
public class ChestData : ScriptableObject
{
    public enum ChestKind { Random, Gold, Equipment, Ring }

    [Header("식별")]
    [SerializeField] private ChestKind kind = ChestKind.Random;
    [SerializeField] private string chestName;

    [Header("개방 (GDD 9.14: 1초)")]
    [SerializeField, Min(0f)] private float openSeconds = 1f;
    [Tooltip("반지 상자처럼 열쇠가 필요한가. true면 keyType 보유 시에만 개방 가능.")]
    [SerializeField] private bool requiresKey = false;
    [SerializeField] private KeyType keyType = KeyType.RingChest;

    [Header("아이템 보상 (ID 구간 카테고리)")]
    [SerializeField] private ItemCategory[] itemCategories;
    [SerializeField, Min(0)] private int itemDropMin = 1;
    [SerializeField, Min(0)] private int itemDropMax = 1;
    [Tooltip("이 상자에서 제외할 아이템 ID. 고정 스케줄 아이템은 상자에서 안 나오게 함")]
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
