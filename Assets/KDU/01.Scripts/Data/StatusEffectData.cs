using UnityEngine;

/// <summary>
/// 상태이상 효과 정의 SO.
/// 적의 onHitEffect에 할당하거나, 함정/아이템에서 사용.
/// 기존 NYH의 StatusType enum과 StatusController를 재활용.
/// </summary>
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

    [Header("이동속도 증가량 (디버프 시 양수)")]
    [Tooltip("초/칸 기준으로 더해지는 값. 0이면 이동속도 영향 없음")]
    public float moveSpeedPenalty = 0f;
}
