using UnityEngine;

// 상점 상품표 (GDD 11.4). 인스펙터로 입력
[CreateAssetMenu(fileName = "ShopTableData", menuName = "StolenTime/Shop Table")]
public class ShopTableData : ScriptableObject
{
    [SerializeField] private ShopItemEntry[] entries;

    public ShopItemEntry[] Entries => entries;

    // 상점 층 → 인덱스 (1→0, 6→1, 11→2). 그 외 -1
    public int FloorIndex(int depth)
    {
        return depth switch
        {
            1 => 0,
            6 => 1,
            11 => 2,
            _ => -1,
        };
    }
}
