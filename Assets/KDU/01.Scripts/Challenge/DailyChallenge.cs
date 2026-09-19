using System;
using System.Globalization;
using Steamworks;

// 일일 시드 챌린지 — KST 날짜 키·시드·보드 이름
public static class DailyChallenge
{
    private const string KeyFormat = "yyyyMMdd";
    private const int KstOffsetHours = 9;

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
