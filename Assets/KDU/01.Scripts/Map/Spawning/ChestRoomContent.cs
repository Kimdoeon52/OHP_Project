using UnityEngine;

/// <summary>
/// Special/Secret 방 한 종류의 "상자 구성" 정의 SO (GDD 6.2.1 / 6.2.2 중 상자류).
/// 예) 랜덤 상자 방(랜덤상자 2~3), 골드 상자 방(골드상자 3), 반지 상자 방(반지상자 2),
///     장비 상자 방(장비상자 1).
/// ChestSpawner 가 방 타입별 풀에서 이 SO 를 1개 뽑아 배치한다.
/// 새 상자 방 종류 = SO 1개 추가 + 풀 등록 (코드 수정 X).
/// </summary>
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
