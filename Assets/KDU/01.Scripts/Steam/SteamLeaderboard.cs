using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

// Steam 리더보드 — 일일(KST 날짜별)/전체 보드 업로드·조회
public class SteamLeaderboard : PersistentSingleton<SteamLeaderboard>
{
    // 랭킹 한 줄
    public struct Entry
    {
        public int rank;
        public CSteamID user;
        public int score;
        public int depth;
        public bool isMe;
        public ScoreEntry detail;   // 상세 팝업용, 구버전 기록이면 null
    }

    // 보드 종류
    public enum Board { Daily, Global }

    // 조회 범위
    public enum Scope { Top, AroundMe, Friends }

    // 순위 표시 상한 (밖이면 NA)
    public const int MaxRank = 100;

    public const string GlobalBoardName = "global_alltime";

    // 자정 넘긴 일일 런 업로드 허용 시간
    private const int GraceHours = 1;

    // 내 순위 조회 시 앞뒤 인원 기본값
    public const int DefaultAroundRange = 5;

    private readonly Dictionary<string, SteamLeaderboard_t> _boards = new Dictionary<string, SteamLeaderboard_t>();

    // 진행 중 CallResult 보관 — GC로 취소되지 않게
    private readonly HashSet<object> _inFlight = new HashSet<object>();

    private void OnEnable()
    {
        GameEvents.OnScoreFinalized += OnScoreFinalized;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreFinalized -= OnScoreFinalized;
    }

    // 런 종료 — 일일 런은 날짜 보드, 무작위 시드 일반 런은 전체 보드
    private void OnScoreFinalized(ScoreResult result)
    {
        ChallengeState cs = ChallengeState.Instance;
        if (cs == null || !SteamBootstrap.IsReady)
            return;

        int[] details = LeaderboardDetails.Pack(result, SaveManager.BuildChallenges(), SaveManager.BuildEquipped(), SteamUtils.GetServerRealTime());

        if (cs.IsDaily)
        {
            if (!DailyChallenge.TryGetNowKst(out DateTime now))
                return;

            if (!DailyChallenge.IsUploadable(cs.DailyKey, now, GraceHours))
            {
                Debug.Log($"[Leaderboard] {cs.DailyKey} 보드 마감 — 업로드 생략");
                return;
            }

            Upload(DailyChallenge.BoardName(cs.DailyKey), Board.Daily, result.finalScore, details);
            return;
        }

        // 시드 직접 입력 런은 기록 제외
        if (cs.HasCustomSeed)
            return;

        Upload(GlobalBoardName, Board.Global, result.finalScore, details);
    }

    // 최고 기록만 유지하며 업로드
    public void Upload(string boardName, Board board, int score, int[] details)
    {
        WithBoard(boardName, handle =>
        {
            SteamAPICall_t call = SteamUserStats.UploadLeaderboardScore(handle, ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, score, details, details.Length);
            Call<LeaderboardScoreUploaded_t>(call, (r, ioFail) =>
            {
                if (ioFail || r.m_bSuccess == 0)
                {
                    Debug.LogWarning($"[Leaderboard] 업로드 실패 ({boardName})");
                    return;
                }

                GameEvents.OnLeaderboardUploaded?.Invoke(board, r.m_nGlobalRankNew);
            });
        });
    }

    // Top 조회 결과 기준 이 점수의 순위 (내 최고 기록 포함 비교), MaxRank 밖이면 0
    public static int RankForScore(List<Entry> top, int score)
    {
        int above = 0;
        foreach (Entry e in top)
        {
            if (e.score > score)
                above++;
        }

        int rank = above + 1;
        return rank > MaxRank ? 0 : rank;
    }

    // 랭킹 조회 — Top은 1~MaxRank, AroundMe는 내 앞뒤 aroundRange명 (0이면 나만). 실패 시 null
    public void Download(string boardName, Scope scope, int aroundRange, Action<List<Entry>> onDone)
    {
        WithBoard(boardName, handle =>
        {
            SteamAPICall_t call;
            switch (scope)
            {
                case Scope.AroundMe:
                    call = SteamUserStats.DownloadLeaderboardEntries(handle, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, -aroundRange, aroundRange);
                    break;
                case Scope.Friends:
                    call = SteamUserStats.DownloadLeaderboardEntries(handle, ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends, 0, 0);
                    break;
                default:
                    call = SteamUserStats.DownloadLeaderboardEntries(handle, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 1, MaxRank);
                    break;
            }

            Call<LeaderboardScoresDownloaded_t>(call, (r, ioFail) => onDone?.Invoke(ioFail ? null : ReadEntries(r)));
        }, () => onDone?.Invoke(null));
    }

    private static List<Entry> ReadEntries(LeaderboardScoresDownloaded_t r)
    {
        var list = new List<Entry>(r.m_cEntryCount);
        var details = new int[LeaderboardDetails.Count];
        CSteamID me = SteamUser.GetSteamID();

        for (int i = 0; i < r.m_cEntryCount; i++)
        {
            Array.Clear(details, 0, details.Length);
            if (!SteamUserStats.GetDownloadedLeaderboardEntry(r.m_hSteamLeaderboardEntries, i, out LeaderboardEntry_t e, details, details.Length))
                continue;

            ScoreEntry detail = LeaderboardDetails.Unpack(details, e.m_cDetails, e.m_nScore);

            // 친구 외 유저는 닉네임이 캐시에 없을 수 있음 → 요청
            SteamFriends.RequestUserInformation(e.m_steamIDUser, true);

            list.Add(new Entry
            {
                rank = e.m_nGlobalRank,
                user = e.m_steamIDUser,
                score = e.m_nScore,
                depth = detail != null ? detail.reachedDepth : 0,
                isMe = e.m_steamIDUser == me,
                detail = detail
            });
        }

        return list;
    }

    // 보드 핸들 확보 (캐시 → 없으면 생성)
    private void WithBoard(string boardName, Action<SteamLeaderboard_t> onReady, Action onFail = null)
    {
        if (!SteamBootstrap.IsReady || string.IsNullOrEmpty(boardName))
        {
            onFail?.Invoke();
            return;
        }

        if (_boards.TryGetValue(boardName, out SteamLeaderboard_t cached))
        {
            onReady(cached);
            return;
        }

        SteamAPICall_t call = SteamUserStats.FindOrCreateLeaderboard(boardName, ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending, ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
        Call<LeaderboardFindResult_t>(call, (r, ioFail) =>
        {
            if (ioFail || r.m_bLeaderboardFound == 0)
            {
                Debug.LogWarning($"[Leaderboard] 보드 찾기 실패 ({boardName})");
                onFail?.Invoke();
                return;
            }

            _boards[boardName] = r.m_hSteamLeaderboard;
            onReady(r.m_hSteamLeaderboard);
        });
    }

    // CallResult 생성·보관·해제
    private void Call<T>(SteamAPICall_t handle, Action<T, bool> onDone) where T : struct
    {
        CallResult<T> result = null;
        result = CallResult<T>.Create((r, ioFail) =>
        {
            _inFlight.Remove(result);
            onDone(r, ioFail);
        });

        _inFlight.Add(result);
        result.Set(handle);
    }
}
