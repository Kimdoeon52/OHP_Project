using System.Collections.Generic;

// 타이틀 씬에서 고른 챌린지/시드를 게임 씬으로 넘기는 정적 전달자.
// MoveScene.PendingTargetSceneName 과 같은 방식 — 씬 전환에도 정적 값은 유지됨.
public static class ChallengeSelection
{
    private static readonly List<ChallengeType> _types = new List<ChallengeType>();

    public static IReadOnlyList<ChallengeType> PendingTypes => _types;
    public static bool HasSeed { get; private set; }
    public static int Seed { get; private set; }

    // 일일 챌린지 날짜 키 (일반 런이면 null)
    public static string DailyKey { get; private set; }
    public static bool IsDaily => !string.IsNullOrEmpty(DailyKey);

    public static void Set(IEnumerable<ChallengeType> types, bool hasSeed, int seed)
    {
        _types.Clear();
        if (types != null)
            _types.AddRange(types);

        HasSeed = hasSeed;
        Seed = seed;
        DailyKey = null;
    }

    // 일일 챌린지 — 챌린지 토글 없음, 날짜 시드 고정
    public static void SetDaily(string key)
    {
        _types.Clear();
        HasSeed = true;
        Seed = DailyChallenge.SeedFor(key);
        DailyKey = key;
    }

    // 일반 새 게임 진입 시 — 남은 일일 선택(시드 포함)만 제거
    public static void ClearDaily()
    {
        if (IsDaily)
            Clear();
    }

    // 게임 씬에서 소비 후 초기화 — 다음 새 게임에 잔여 선택이 남지 않게.
    public static void Clear()
    {
        _types.Clear();
        HasSeed = false;
        Seed = 0;
        DailyKey = null;
    }
}
