/// <summary>
/// 아이템 강화도/저주 추첨 (DungeonRandom 사용). 상자·맵 스폰 공용.
/// 확률은 기존 ItemSpawner 와 동일. (GDD 9 "등장 강화도 확률" 확정되면 여기만 수정)
/// </summary>
public static class ItemRoll
{
    public static int Enhance(DungeonRandom rng)
    {
        float r = rng.Value();
        if (r < 0.55f) return 0;
        if (r < 0.85f) return 1;
        if (r < 0.95f) return 2;
        return 3;
    }

    public static CurseData Curse(DungeonRandom rng, CurseData[] pool)
    {
        if (pool == null || pool.Length == 0) return null;
        if (rng.Value() > 0.5f) return null;
        return pool[rng.Range(0, pool.Length)];
    }
}
