using System;
using System.Collections.Generic;

// 한 런의 저장 단위. 층 진입 시점에 1번 기록.
[Serializable]
public class RunSnapshot
{
    public int version = 1;          // 포맷 마이그레이션용
    public int seed;                 // RunState.RunSeed
    public int depth;                // 진입 시점 층

    public List<int> challenges = new List<int>();          // 활성 ChallengeType

    public PlayerSnapshot player = new PlayerSnapshot();
    public List<ItemDTO> inventory = new List<ItemDTO>();   // 가방 아이템
    public List<EquipDTO> equipped = new List<EquipDTO>();  // 장착 슬롯
}

[Serializable]
public class PlayerSnapshot
{
    public int currentHp;
    public int maxHealth;
    public int strength;
    public int moveSpeed;
    public int attackSpeed;
    public int accuracy;
    public int defense;
    public int evasion;
    public int hunger;
    public int level;
    public int exp;
    public int potionCount;          // GridPlayerHealth 전용 HP포션 수
}

// 인벤/장비 공용 아이템 직렬화 형태. 장비 필드는 장비일 때만 유효.
[Serializable]
public class ItemDTO
{
    public int itemId;               // ItemBase.Id
    public int count = 1;            // InventoryItem 스택 수량

    // 장비 전용
    public int enhanceLevel;
    public int curseType = -1;       // CurseType, -1=없음
    public int magicType = -1;       // MagicType, -1=없음
    public bool isAnalyzed;

    // 투척 무기 전용 (currentDurabilities)
    public List<int> durabilities;
}

[Serializable]
public class EquipDTO
{
    public int slot;                 // EquipmentSlot
    public int slotIndex;            // 반지 0/1 구분
    public ItemDTO item = new ItemDTO();
}
