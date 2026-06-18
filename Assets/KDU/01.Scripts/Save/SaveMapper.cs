using System.Collections.Generic;
using UnityEngine;

// ItemInstance <-> ItemDTO 변환. SO 참조는 id/enum으로 평탄화.
public static class SaveMapper
{
    // 인스턴스 + 스택수량 -> DTO. count는 InventoryItem.Count에서 받음.
    public static ItemDTO ToDTO(ItemInstance inst, int count)
    {
        var dto = new ItemDTO();
        if (inst == null || inst.data == null)
            return dto;

        dto.itemId = inst.data.Id;
        dto.count = Mathf.Max(1, count);

        if (inst is EquipmentInstance e)
        {
            dto.enhanceLevel = e.enhanceLevel;
            dto.curseType = e.curse != null ? (int)e.curse.Type : -1;
            dto.magicType = e.magic != null ? (int)e.magic.Type : -1;
            dto.isAnalyzed = e.isAnalyzed;
        }

        if (inst is ThrowableWeaponInstance t && t.currentDurabilities != null)
            dto.durabilities = new List<int>(t.currentDurabilities);

        return dto;
    }

    // DTO -> 인스턴스. CreateInstance가 올바른 서브클래스 반환.
    public static ItemInstance FromDTO(ItemDTO dto, ItemDatabase db)
    {
        if (dto == null || db == null)
            return null;

        ItemBase so = db.GetItem(dto.itemId);
        if (so == null)
        {
            Debug.LogError($"[SaveMapper] id {dto.itemId} SO 없음 — 스킵");
            return null;
        }

        ItemInstance inst = so.CreateInstance();

        if (inst is EquipmentInstance e)
        {
            e.enhanceLevel = dto.enhanceLevel;
            e.curse = db.GetCurse(dto.curseType);
            e.magic = db.GetMagic(dto.magicType);
            e.isAnalyzed = dto.isAnalyzed;
        }

        if (inst is ThrowableWeaponInstance t)
            t.currentDurabilities = dto.durabilities != null ? new List<int>(dto.durabilities) : new List<int>();

        return inst;
    }
}
