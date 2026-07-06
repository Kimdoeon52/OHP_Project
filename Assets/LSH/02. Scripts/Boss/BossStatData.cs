using UnityEngine;

// 보스 1종의 기본/강화 체력 및 내구성. 강화 데미지는 고정 배율이라 BossBase에서 처리.
[CreateAssetMenu(fileName = "BossStat", menuName = "60s Dungeon/Boss/Boss Stat")]
public class BossStatData : ScriptableObject
{
    [Header("기본 체력")]
    [Tooltip("기본 체력")]
    public int hp = 300;

    [Tooltip("강화 체력")]
    public int enhancedHp = 400;

    [Header("내구성 (방어력) — 피격 시 랜덤 경감 범위")]
    [Tooltip("피격 시 경감할 데미지 최솟값 (기본). 보스A: 0 / 보스B: 0 / 최종보스: 0")]
    public int defenseMin = 0;

    [Tooltip("피격 시 경감할 데미지 최댓값 (기본). 보스A: 2 / 보스B: 5 / 최종보스 손·머리: 10")]
    public int defenseMax = 0;

    [Tooltip("강화 상태의 내구성 최솟값. 0이면 기본값과 동일.")]
    public int enhancedDefenseMin = 0;

    [Tooltip("강화 상태의 내구성 최댓값. 0이면 기본값과 동일.")]
    public int enhancedDefenseMax = 0;

    // isEnhanced면 강화 체력.
    public int ResolveHp(bool enhanced)
    {
        return enhanced ? enhancedHp : hp;
    }

    // isEnhanced면 강화 내구성 최솟값. enhancedDefenseMin이 0이면 기본값 사용.
    public int ResolveDefenseMin(bool enhanced)
    {
        if (enhanced && enhancedDefenseMin > 0)
            return enhancedDefenseMin;
        return defenseMin;
    }

    // isEnhanced면 강화 내구성 최댓값. enhancedDefenseMax가 0이면 기본값 사용.
    public int ResolveDefenseMax(bool enhanced)
    {
        if (enhanced && enhancedDefenseMax > 0)
            return enhancedDefenseMax;
        return defenseMax;
    }
}
