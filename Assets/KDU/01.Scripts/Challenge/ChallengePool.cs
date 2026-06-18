using System.Collections.Generic;
using UnityEngine;

// 선택 UI가 나열할 전체 챌린지 목록.
[CreateAssetMenu(fileName = "ChallengePool", menuName = "60s Dungeon/Challenge/Challenge Pool")]
public class ChallengePool : ScriptableObject
{
    [SerializeField] private List<ChallengeData> challenges = new List<ChallengeData>();

    public IReadOnlyList<ChallengeData> Challenges => challenges;

    public ChallengeData Get(ChallengeType type)
    {
        foreach (var c in challenges)
        {
            if (c != null && c.Type == type)
                return c;
        }
        return null;
    }
}
