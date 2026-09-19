using System;
using System.Collections.Generic;
using System.Globalization;
using Steamworks;

// 일일 시드 챌린지 — KST 날짜 키·시드·챌린지 조합·보드 이름
public static class DailyChallenge
{
    private const string KeyFormat = "yyyyMMdd";
    private const int KstOffsetHours = 9;

    // 하루 챌린지 개수 범위
    private const int MinChallenges = 1;
    private const int MaxChallenges = 4;

    // 맵 시드와 다른 난수열을 쓰기 위한 분리값
    private const int ChallengeSalt = 0x51A17;

    // Steam 서버 시각 기준 KST 현재 시각 (Steam 미연결이면 false)
    public static bool TryGetNowKst(out DateTime nowKst)
    {
        nowKst = default;
        if (!SteamBootstrap.IsReady)
            return false;

        uint unix = SteamUtils.GetServerRealTime();
        nowKst = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime.AddHours(KstOffsetHours);
        return true;
    }

    // 오늘(KST) 날짜 키
    public static bool TryGetTodayKey(out string key)
    {
        key = null;
        if (!TryGetNowKst(out DateTime now))
            return false;

        key = now.ToString(KeyFormat, CultureInfo.InvariantCulture);
        return true;
    }

    // 다음 KST 자정까지 남은 시간
    public static TimeSpan Remaining(DateTime nowKst)
    {
        return nowKst.Date.AddDays(1) - nowKst;
    }

    public static string BoardName(string key) => $"daily_{key}";

    // 날짜 키 → 고정 시드 (FNV-1a)
    public static int SeedFor(string key)
    {
        uint h = 2166136261;
        foreach (char c in key)
            h = (h ^ c) * 16777619;

        return (int)h;
    }

    // 날짜 키 → 그날의 챌린지 조합. 같은 날은 모든 플레이어가 같은 조합
    public static List<ChallengeType> ChallengesFor(string key)
    {
        var picked = new List<ChallengeType>();
        if (string.IsNullOrEmpty(key))
            return picked;

        var rng = new DungeonRandom();
        rng.Init(SeedFor(key) ^ ChallengeSalt);

        var pool = new List<ChallengeType>((ChallengeType[])Enum.GetValues(typeof(ChallengeType)));
        int count = Math.Min(rng.Range(MinChallenges, MaxChallenges + 1), pool.Count);

        for (int i = 0; i < count; i++)
        {
            int index = rng.Range(0, pool.Count);
            picked.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return picked;
    }

    // 날짜 키 → 표시용 날짜
    public static string DisplayDate(string key)
    {
        if (DateTime.TryParseExact(key, KeyFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
            return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return key;
    }

    // 런 키가 지금 업로드 가능한지 — 당일, 또는 자정 직후 유예 시간 이내의 전날
    public static bool IsUploadable(string runKey, DateTime nowKst, int graceHours)
    {
        string today = nowKst.ToString(KeyFormat, CultureInfo.InvariantCulture);
        if (runKey == today)
            return true;

        string yesterday = nowKst.AddDays(-1).ToString(KeyFormat, CultureInfo.InvariantCulture);
        return runKey == yesterday && nowKst.TimeOfDay.TotalHours < graceHours;
    }
}
