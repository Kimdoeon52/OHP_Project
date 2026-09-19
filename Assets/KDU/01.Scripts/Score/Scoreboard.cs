using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 최고 점수 톱30 영구 저장. run.json과 분리 — 사망해도 유지.
public static class Scoreboard
{
    private const string FileName = "scores.json";
    private const int Capacity = 30;

    private static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

    public static ScoreboardData Load()
    {
        if (!File.Exists(Path))
            return new ScoreboardData();

        try
        {
            string json = File.ReadAllText(Path);
            var data = JsonUtility.FromJson<ScoreboardData>(json);
            return data ?? new ScoreboardData();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Scoreboard] 로드 실패: {ex.Message}");
            return new ScoreboardData();
        }
    }

    // 결과 등록 → 정렬 → 톱30 유지 → 저장. 톱30 진입 순위(0-based) 반환, 미진입이면 -1.
    public static int Submit(ScoreResult result, List<EquipDTO> equipped, List<int> challenges)
    {
        ScoreEntry entry = BuildEntry(result, equipped, challenges);

        ScoreboardData data = Load();
        data.entries.Add(entry);
        data.entries.Sort((a, b) => b.finalScore.CompareTo(a.finalScore));

        if (data.entries.Count > Capacity)
            data.entries.RemoveRange(Capacity, data.entries.Count - Capacity);

        Save(data);

        int rank = data.entries.IndexOf(entry);
        return rank;
    }

    // 결과 → 기록 1줄. 저장하지 않음 (결산 UI도 같은 형식을 씀)
    public static ScoreEntry BuildEntry(ScoreResult result, List<EquipDTO> equipped, List<int> challenges)
    {
        return new ScoreEntry
        {
            finalScore = result.finalScore,
            reachedDepth = result.reachedDepth,
            challengeCount = result.challengeCount,
            seed = result.seed,
            playerName = PlayerProfile.DisplayName,
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            daily = ChallengeState.Instance != null && ChallengeState.Instance.IsDaily,
            killCount = result.killCount,
            challenges = challenges ?? new List<int>(),
            equipped = equipped ?? new List<EquipDTO>()
        };
    }

    private static void Save(ScoreboardData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(Path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Scoreboard] 저장 실패: {ex.Message}");
        }
    }

    public static void Clear()
    {
        if (File.Exists(Path))
            File.Delete(Path);
    }
}
