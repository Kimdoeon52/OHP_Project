using System;
using System.Collections.Generic;

// Steam 리더보드 부가 정보(int 배열) ↔ ScoreEntry 변환 — 다른 유저 기록 상세 팝업용
public static class LeaderboardDetails
{
    private const int Version = 1;

    // 헤더 [버전, 도달 층, 처치 수, 시드, 챌린지 비트마스크, 기록 시각(unix)]
    private const int HeaderSize = 6;

    // 장비 1칸 [itemId+1(0=빈칸), 강화, 저주, 마법, 감정 여부]
    private const int SlotSize = 5;

    // 무기, 방어구, 반지0, 반지1, 아티팩트, 룬 (ScoreDetailPopup 칸 순서)
    private static readonly (EquipmentSlot slot, int index)[] Slots =
    {
        (EquipmentSlot.Weapon, 0),
        (EquipmentSlot.Armor, 0),
        (EquipmentSlot.Ring, 0),
        (EquipmentSlot.Ring, 1),
        (EquipmentSlot.Artifact, 0),
        (EquipmentSlot.Rune, 0),
    };

    public static readonly int Count = HeaderSize + SlotSize * Slots.Length;

    public static int[] Pack(ScoreResult result, List<int> challenges, List<EquipDTO> equipped, uint unixTime)
    {
        var d = new int[Count];
        d[0] = Version;
        d[1] = result.reachedDepth;
        d[2] = result.killCount;
        d[3] = result.seed;
        d[4] = ChallengeMask(challenges);
        d[5] = (int)unixTime;

        if (equipped == null)
            return d;

        foreach (EquipDTO equip in equipped)
        {
            if (equip == null || equip.item == null)
                continue;

            int slot = SlotOf((EquipmentSlot)equip.slot, equip.slotIndex);
            if (slot < 0)
                continue;

            int o = HeaderSize + slot * SlotSize;
            d[o] = equip.item.itemId + 1;
            d[o + 1] = equip.item.enhanceLevel;
            d[o + 2] = equip.item.curseType;
            d[o + 3] = equip.item.magicType;
            d[o + 4] = equip.item.isAnalyzed ? 1 : 0;
        }

        return d;
    }

    // 버전이 다르거나 개수가 부족하면 null (상세 없음)
    public static ScoreEntry Unpack(int[] d, int count, int score)
    {
        if (d == null || count < Count || d[0] != Version)
            return null;

        var entry = new ScoreEntry
        {
            finalScore = score,
            reachedDepth = d[1],
            killCount = d[2],
            seed = d[3],
            date = DateTimeOffset.FromUnixTimeSeconds((uint)d[5]).ToLocalTime().ToString("yyyy-MM-dd HH:mm")
        };

        for (int bit = 0; bit < 32; bit++)
        {
            if ((d[4] & (1 << bit)) != 0)
                entry.challenges.Add(bit);
        }

        entry.challengeCount = entry.challenges.Count;

        for (int i = 0; i < Slots.Length; i++)
        {
            int o = HeaderSize + i * SlotSize;
            if (d[o] == 0)
                continue;

            entry.equipped.Add(new EquipDTO
            {
                slot = (int)Slots[i].slot,
                slotIndex = Slots[i].index,
                item = new ItemDTO
                {
                    itemId = d[o] - 1,
                    enhanceLevel = d[o + 1],
                    curseType = d[o + 2],
                    magicType = d[o + 3],
                    isAnalyzed = d[o + 4] != 0
                }
            });
        }

        return entry;
    }

    private static int ChallengeMask(List<int> challenges)
    {
        int mask = 0;
        if (challenges == null)
            return mask;

        foreach (int t in challenges)
        {
            if (t >= 0 && t < 32)
                mask |= 1 << t;
        }

        return mask;
    }

    private static int SlotOf(EquipmentSlot slot, int index)
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i].slot == slot && Slots[i].index == index)
                return i;
        }

        return -1;
    }
}
