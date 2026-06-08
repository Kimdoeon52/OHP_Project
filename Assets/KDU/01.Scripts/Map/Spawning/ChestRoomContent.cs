using UnityEngine;

// Special/Secret 방 마다 들어갈 상자 구성
[CreateAssetMenu(fileName = "ChestRoomContent", menuName = "Item/Chest Room")]
public class ChestRoomContent : ScriptableObject
{
    [Tooltip("식별/디버그용 이름 (랜덤 상자 방 등).")]
    [SerializeField] private string label;

    [Header("상자")]
    [SerializeField] private ChestData chest;
    [SerializeField, Min(1)] private int countMin = 1;
    [SerializeField, Min(1)] private int countMax = 1;

    [Header("동봉 열쇠 (반지 상자 방 = RingChest)")]
    [Tooltip("반지 상자 방처럼 같은 층 Normal/Exit 방에 상자 열쇠를 떨굴지 (GDD 9.15).")]
    [SerializeField] private bool spawnChestKey = false;
    [SerializeField] private KeyType chestKeyType = KeyType.RingChest;
    [SerializeField, Min(1)] private int chestKeyCount = 1;

    public string Label => label;
    public ChestData Chest => chest;
    public int CountMin => countMin;
    public int CountMax => countMax;
    public bool SpawnChestKey => spawnChestKey;
    public KeyType ChestKeyType => chestKeyType;
    public int ChestKeyCount => chestKeyCount;
}
