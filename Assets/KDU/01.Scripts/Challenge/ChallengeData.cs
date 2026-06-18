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

    [Tooltip("아이콘")]
    [SerializeField] private Sprite icon;

    public ChallengeType Type => type;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
}
