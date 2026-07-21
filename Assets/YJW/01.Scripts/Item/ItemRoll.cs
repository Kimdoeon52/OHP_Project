using System.Collections.Generic;

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

    public static CurseData Curse(DungeonRandom rng, CurseData[] pool, EquipmentSlot targetSlot)
    {
        if (pool == null || pool.Length == 0) return null;
        if (rng.Value() > 0.5f) return null;

        List<CurseData> matching = new List<CurseData>();
        foreach (CurseData curse in pool)
        {
            if (curse != null && curse.TargetSlot == targetSlot)
                matching.Add(curse);
        }
        if (matching.Count == 0) return null;
        return matching[rng.Range(0, matching.Count)];
    }

    // 저주/마법은 근접무기·방어구에만 붙는다. 투척무기 등 그 외 장비는 대상 아님.
    public static bool IsCurseEligible(ItemBase item)
    {
        return item is CloseWeapon || item is ArmorData;
    }
}
