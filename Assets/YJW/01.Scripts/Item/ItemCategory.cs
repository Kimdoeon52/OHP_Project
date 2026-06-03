/// <summary>
/// 아이템 ID 구간 → 카테고리 매핑 (상자 전리품 / 맵 드랍 분기용).
/// ID 규칙(YJW 지정):
///   1001~2000 스크롤 / 2001~3000 포션 / 3001~4000 투척무기 /
///   4001~5000 방어구 / 5001~6000 근접무기 /
///   6001 횃불 / 6002 생명파편 / 6003~6005 음식 / 7001~8000 반지.
/// 전체 목록: Assets/YJW/01.Scripts/Item/ITEM_IDS.md
/// </summary>
public enum ItemCategory
{
    Unknown,
    Scroll,       // 1001~2000
    Potion,       // 2001~3000
    ThrowWeapon,  // 3001~4000
    Armor,        // 4001~5000
    MeleeWeapon,  // 5001~6000
    Torch,        // 6001
    LifeShard,    // 6002
    Food,         // 6003~6005
    Ring,         // 7001~8000
}

public static class ItemCategories
{
    public static ItemCategory Of(ItemBase item)
    {
        return item == null ? ItemCategory.Unknown : FromId(item.Id);
    }

    public static ItemCategory FromId(int id)
    {
        if (id >= 1001 && id <= 2000) return ItemCategory.Scroll;
        if (id >= 2001 && id <= 3000) return ItemCategory.Potion;
        if (id >= 3001 && id <= 4000) return ItemCategory.ThrowWeapon;
        if (id >= 4001 && id <= 5000) return ItemCategory.Armor;
        if (id >= 5001 && id <= 6000) return ItemCategory.MeleeWeapon;
        if (id == 6001) return ItemCategory.Torch;
        if (id == 6002) return ItemCategory.LifeShard;
        if (id >= 6003 && id <= 6005) return ItemCategory.Food;
        if (id >= 7001 && id <= 8000) return ItemCategory.Ring;
        return ItemCategory.Unknown;
    }
}
