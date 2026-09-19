using System;
using System.Collections.Generic;

// 리더보드 1줄.
[Serializable]
public class ScoreEntry
{
    public int finalScore;
    public int reachedDepth;
    public int challengeCount;
    public int seed;
    public string playerName; // 기록 당시 이름 (구버전 기록은 비어 있음)
    public string date;       // 기록 시각 (yyyy-MM-dd HH:mm)
    public bool daily;        // 일일 챌린지 런 (세계 순위 비교 제외)

    public int killCount;                                  // 총 처치 수
    public List<int> challenges = new List<int>();         // 활성 ChallengeType
    public List<EquipDTO> equipped = new List<EquipDTO>(); // 종료 시점 장착 장비
}

// JsonUtility는 최상위 List 직렬화 불가 → 래퍼.
[Serializable]
public class ScoreboardData
{
    public List<ScoreEntry> entries = new List<ScoreEntry>();
}
