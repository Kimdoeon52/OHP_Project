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
    [Tooltip("이 상자가 줄 수 있는 카테고리. 풀에서 이 카테고리에 해당하는 아이템만 추첨.")]
    [SerializeField] private ItemCategory[] itemCategories;
    [SerializeField, Min(0)] private int itemDropMin = 1;
    [SerializeField, Min(0)] private int itemDropMax = 1;
    [Tooltip("랜덤 상자 = 강화 스크롤 제외 (GDD 9.14).")]
    [SerializeField] private bool excludeReinforceScroll = true;

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
    public bool ExcludeReinforceScroll => excludeReinforceScroll;
    public int GoldMin => goldMin;
    public int GoldMax => goldMax;
    public Sprite ClosedSprite => closedSprite;
    public Sprite OpenedSprite => openedSprite;
}
