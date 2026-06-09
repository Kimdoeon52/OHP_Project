using UnityEngine;

// 상점 상품 1행 (GDD 11.4). 층 인덱스 0/1/2 = 1/6/11층
[System.Serializable]
public class ShopItemEntry
{
    [Tooltip("단일 상품. oneOfSet 비어있을 때 사용")]
    public ItemBase item;

    [Tooltip("후보 풀 — 비어있지 않으면 여기서 랜덤 1개 (둘 중 하나 / 반지 풀)")]
    public ItemBase[] oneOfSet;

    [Tooltip("층별 등장확률 (1/6/11)")]
    public float[] appearChance = new float[3];

    [Tooltip("층별 개수 범위 (x=최소, y=최대)")]
    public Vector2Int[] countRange = new Vector2Int[3];

    [Tooltip("층별 가격")]
    public int[] price = new int[3];

    [Tooltip("강화 수치 (0강)")]
    public int enhanceLevel;

    // 층 인덱스 등장확률
    public float Chance(int floorIndex)
    {
        return InRange(appearChance, floorIndex) ? appearChance[floorIndex] : 0f;
    }

    // 층 인덱스 개수 (범위면 rng)
    public int Count(int floorIndex, DungeonRandom rng)
    {
        if (!InRange(countRange, floorIndex))
            return 0;
        Vector2Int r = countRange[floorIndex];
        if (r.y <= r.x)
            return Mathf.Max(0, r.x);
        return rng.Range(r.x, r.y + 1);
    }

    // 층 인덱스 가격
    public int Price(int floorIndex)
    {
        return InRange(price, floorIndex) ? price[floorIndex] : 0;
    }

    // 상품 1개 결정
    public ItemBase PickProduct(DungeonRandom rng)
    {
        if (oneOfSet != null && oneOfSet.Length > 0)
            return rng.Pick(oneOfSet);
        return item;
    }

    private static bool InRange<T>(T[] arr, int idx)
    {
        return arr != null && idx >= 0 && idx < arr.Length;
    }
}
