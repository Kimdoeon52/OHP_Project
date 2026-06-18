using System.Collections.Generic;

// 타이틀 씬에서 고른 챌린지/시드를 게임 씬으로 넘기는 정적 전달자.
// MoveScene.PendingTargetSceneName 과 같은 방식 — 씬 전환에도 정적 값은 유지됨.
public static class ChallengeSelection
{
    private static readonly List<ChallengeType> _types = new List<ChallengeType>();

    public static IReadOnlyList<ChallengeType> PendingTypes => _types;
    public static bool HasSeed { get; private set; }
    public static int Seed { get; private set; }

    public static void Set(IEnumerable<ChallengeType> types, bool hasSeed, int seed)
    {
        _types.Clear();
        if (types != null)
            _types.AddRange(types);

        HasSeed = hasSeed;
        Seed = seed;
    }

    // 게임 씬에서 소비 후 초기화 — 다음 새 게임에 잔여 선택이 남지 않게.
    public static void Clear()
    {
        _types.Clear();
        HasSeed = false;
        Seed = 0;
    }
}
