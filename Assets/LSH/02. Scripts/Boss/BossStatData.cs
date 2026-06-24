using UnityEngine;

// 보스 1종의 기본/강화 체력. 강화 데미지는 고정 배율이라 BossBase에서 처리.
[CreateAssetMenu(fileName = "BossStat", menuName = "60s Dungeon/Boss/Boss Stat")]
public class BossStatData : ScriptableObject
{
    [Tooltip("기본 체력")]
    public int hp = 300;

    [Tooltip("강화 체력")]
    public int enhancedHp = 400;

    // isEnhanced면 강화 체력.
    public int ResolveHp(bool enhanced)
    {
        return enhanced ? enhancedHp : hp;
    }
}
