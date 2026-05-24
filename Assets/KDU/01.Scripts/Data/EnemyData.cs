using UnityEngine;

public enum AttackPattern { Melee, Ranged }

/// <summary>
/// 적 등급: 스폰 빈도와 밸런싱에 사용
/// </summary>
public enum EnemyGrade { Common, Elite, MiniBoss, Boss }

/// <summary>
/// 이동 타입: 벽 통과 여부 등 결정
/// </summary>
public enum EnemyMoveType { Normal, WallPass, Stationary }

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

    [Header("공격력 (범위)")]
    public int attackMin = 1;
    public int attackMax = 1;
    public AttackPattern attackPattern;

    [Header("속도")]
    [Tooltip("이동 속도 (초/칸, 낮을수록 빠름)")]
    public float moveSpeed = 1f;
    [Tooltip("공격 속도 (회/s)")]
    public float attackSpeed = 1f;

    [Header("방어 (범위)")]
    public int defenseMin = 0;
    public int defenseMax = 0;

    [Header("회피 / 정확도")]
    public int evasion = 0;
    public int accuracy = 10;

    [Header("보상")]
    public float timeBonus = 3f;        // 처치 시 추가 시간 (초)
    public int expReward = 5;
    public int goldMin = 0;
    public int goldMax = 0;

    [Header("시야")]
    public float visionRange = 6f;      // 플레이어 감지 거리

    [Header("명중 시 상태이상 (선택)")]
    [Tooltip("null이면 상태이상 없음")]
    public StatusEffectData onHitEffect;

    // ── 하위호환 프로퍼티 ──
    // 기존 코드에서 data.attackDamage를 참조하던 곳 호환
    public int attackDamage => (attackMin + attackMax) / 2;
}
