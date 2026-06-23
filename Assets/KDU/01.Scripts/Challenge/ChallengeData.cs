using System.Collections.Generic;
using UnityEngine;

// 챌린지 1종의 표시용 메타데이터. 선택 UI가 읽음.
[CreateAssetMenu(fileName = "Challenge", menuName = "60s Dungeon/Challenge/Challenge")]
public class ChallengeData : ScriptableObject
{
    [Tooltip("챌린지 종류")]
    [SerializeField] private ChallengeType type;

    [Tooltip("표시 이름")]
    [SerializeField] private string displayName;

    [Tooltip("효과 설명")]
    [SerializeField, TextArea] private string description;

    public ChallengeType Type => type;
    public string DisplayName => displayName;
    public string Description => description;

    // 토글 라벨에서 이미 쓰고 있는 이름 키를 그대로 재사용한다.
    private static readonly Dictionary<ChallengeType, string> NameKeyById = new()
    {
        { ChallengeType.TimePressure, "msg_시간압박" },
        { ChallengeType.BossBuff, "msg_보스강화" },
        { ChallengeType.ScarceUpgrade, "msg_빈약한강화" },
        { ChallengeType.HealSeal, "msg_회복봉인" },
        { ChallengeType.FragmentSeal, "msg_파편봉인" },
        { ChallengeType.VisionLimit, "msg_시야제한" }
    };

    public string LocalizedDisplayName => LocalizationManager.GetOrDefault(NameKeyById.TryGetValue(type, out string key) ? key : "", displayName);
    // 챌린지는 ChallengeType 1:1 매핑이라 enum 이름을 그대로 설명 키로 쓴다.
    public string LocalizedDescription => LocalizationManager.GetOrDefault($"challenge_desc_{type}", description);
}
