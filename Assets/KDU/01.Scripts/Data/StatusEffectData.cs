using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffectData", menuName = "60s Dungeon/Status Effect Data")]
public class StatusEffectData : ScriptableObject
{
    [Header("상태이상 종류")]
    public StatusType statusType = StatusType.Poison;

    [Header("지속 시간 (턴)")]
    [Min(1)]
    public int durationTurns = 3;

    [Header("틱 데미지 / 효과 강도")]
    [Min(0)]
    public int power = 1;

    [Header("이동속도 증가량")]
    public float moveSpeedPenalty = 0f;
}
