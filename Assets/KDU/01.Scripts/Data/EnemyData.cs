using UnityEngine;

public enum AttackPattern { Melee, Ranged }

public enum EnemyGrade { Common, Elite }

public enum EnemyMoveType { Normal, WallPass, Stationary }

// 처치 시 확률 드롭 아이템 한 종류
[System.Serializable]
public class ItemDrop
{
    public ItemBase item;
    [Range(0f, 1f)] public float chance = 1f;
    [Min(1)] public int count = 1;
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "60s Dungeon/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string id;
    public string displayName;
    public Sprite sprite;

    [Header("등급/이동 타입")]
    public EnemyGrade grade = EnemyGrade.Common;
    public EnemyMoveType moveType = EnemyMoveType.Normal;
    public bool hasShield = false;

    [Header("체력")]
    public int maxHp = 1;

    [Header("공격력")]
    public int attackMin = 1;
    public int attackMax = 1;
    public AttackPattern attackPattern;

    [Header("속도")]
    public float moveSpeed = 1f;
    [Tooltip("공격 속도")]
    public float attackSpeed = 1f;

    [Header("방어")]
    public int defenseMin = 0;
    public int defenseMax = 0;

    [Header("회피 / 정확도")]
    public int evasion = 0;
    public int accuracy = 10;

    [Header("보상")]
    public float timeBonus = 3f;
    public int expReward = 5;

    [Header("드롭 — 골드")]
    [Range(0f, 1f)] public float goldDropChance = 1f;
    public int goldMin = 0;
    public int goldMax = 0;

    [Header("드롭 — 아이템")]
    public ItemDrop[] itemDrops;

    [Header("시야")]
    public float visionRange = 6f;

    [Header("명중 시 상태이상")]
    public StatusEffectData onHitEffect;

    public int attackDamage => (attackMin + attackMax) / 2;
}
