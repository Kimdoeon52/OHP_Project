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
    public string date;       // 기록 시각 (yyyy-MM-dd HH:mm)
}

// JsonUtility는 최상위 List 직렬화 불가 → 래퍼.
[Serializable]
public class ScoreboardData
{
    public List<ScoreEntry> entries = new List<ScoreEntry>();
}
