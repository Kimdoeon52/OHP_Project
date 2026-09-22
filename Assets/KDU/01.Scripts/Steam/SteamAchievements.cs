using System.Collections.Generic;
using Steamworks;
using UnityEngine;

// Steam 도전과제 해금 — GameEvents 구독으로 조건 판정. Steam 미연결이면 전부 no-op
public class SteamAchievements : PersistentSingleton<SteamAchievements>
{
    // 진행
    private const string BossWatcher = "ACH_BOSS_WATCHER";
    private const string BossUndeadKing = "ACH_BOSS_UNDEAD_KING";
    private const string BossHades = "ACH_BOSS_HADES";
    private const string Depth10 = "ACH_DEPTH_10";

    // 챌린지
    private const string ChallengeFirst = "ACH_CHALLENGE_FIRST";
    private const string Challenge3 = "ACH_CHALLENGE_3";
    private const string ChallengeAll = "ACH_CHALLENGE_ALL";
    private const string DailyFirst = "ACH_DAILY_FIRST";
    private const string DailyTop10 = "ACH_DAILY_TOP10";

    // 점수
    private const string Score10K = "ACH_SCORE_10K";
    private const string Score30K = "ACH_SCORE_30K";

    // 숙련
    private const string BossNoDamage = "ACH_BOSS_NO_DAMAGE";
    private const string KillsRun50 = "ACH_KILLS_RUN_50";
    private const string Residual300 = "ACH_RESIDUAL_300";

    // 탐험
    private const string SecretFirst = "ACH_SECRET_FIRST";
    private const string KeysBoth = "ACH_KEYS_BOTH";
    private const string ShopFirst = "ACH_SHOP_FIRST";

    // 누적
    private const string FirstDeath = "ACH_FIRST_DEATH";
    private const string TotalKills1000 = "ACH_TOTAL_KILLS_1000";

    // 누적 처치 Stat — 파트너 사이트 Stats 탭에 INT 로 등록 필요
    private const string StatTotalKills = "stat_total_kills";

    // 해금 문턱
    private const int FinalDepth = 15;
    private const int Depth10Threshold = 10;
    private const int Challenge3Threshold = 3;
    private const int ChallengeAllThreshold = 6;
    private const int Score10KThreshold = 10000;
    private const int Score30KThreshold = 30000;
    private const int KillsRunThreshold = 50;
    private const int ResidualThreshold = 300;
    private const int TotalKillsThreshold = 1000;
    private const int DailyTopThreshold = 10;

    private Callback<UserStatsReceived_t> _statsReceived;
    private bool _statsLoaded;

    // 보스 전투 진행 중 피격 여부
    private bool _inBossFight;
    private bool _bossFightClean;

    // 이번 층에서 획득한 열쇠 종류
    private readonly HashSet<KeyType> _keysThisFloor = new HashSet<KeyType>();

    // 아직 Stat 에 반영하지 않은 처치 수
    private int _pendingKills;

    private void OnEnable()
    {
        GameEvents.OnBossFightStarted += OnBossFightStarted;
        GameEvents.OnBossDefeated += OnBossDefeated;
        GameEvents.OnDepthChanged += OnDepthChanged;
        GameEvents.OnPlayerDamaged += OnPlayerDamaged;
        GameEvents.OnPlayerDied += OnPlayerDied;
        GameEvents.OnEnemyKilled += OnEnemyKilled;
        GameEvents.OnScoreFinalized += OnScoreFinalized;
        GameEvents.OnLeaderboardUploaded += OnLeaderboardUploaded;
        GameEvents.OnSecretDiscovered += OnSecretDiscovered;
        GameEvents.OnKeyAcquired += OnKeyAcquired;
        GameEvents.OnItemPurchased += OnItemPurchased;
        GameEvents.OnFloorReset += OnFloorReset;
    }

    private void OnDisable()
    {
        GameEvents.OnBossFightStarted -= OnBossFightStarted;
        GameEvents.OnBossDefeated -= OnBossDefeated;
        GameEvents.OnDepthChanged -= OnDepthChanged;
        GameEvents.OnPlayerDamaged -= OnPlayerDamaged;
        GameEvents.OnPlayerDied -= OnPlayerDied;
        GameEvents.OnEnemyKilled -= OnEnemyKilled;
        GameEvents.OnScoreFinalized -= OnScoreFinalized;
        GameEvents.OnLeaderboardUploaded -= OnLeaderboardUploaded;
        GameEvents.OnSecretDiscovered -= OnSecretDiscovered;
        GameEvents.OnKeyAcquired -= OnKeyAcquired;
        GameEvents.OnItemPurchased -= OnItemPurchased;
        GameEvents.OnFloorReset -= OnFloorReset;
    }

    private void Start()
    {
        if (!SteamBootstrap.IsReady)
            return;

        _statsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
        SteamUserStats.RequestCurrentStats();
    }

    private void OnUserStatsReceived(UserStatsReceived_t cb)
    {
        if (cb.m_nGameID != SteamUtils.GetAppID().m_AppId)
            return;

        _statsLoaded = cb.m_eResult == EResult.k_EResultOK;
    }

    // 도전과제 해금 — 이미 해금됐으면 무시
    public void Unlock(string apiName)
    {
        if (!SteamBootstrap.IsReady || !_statsLoaded)
            return;

        if (SteamUserStats.GetAchievement(apiName, out bool unlocked) && unlocked)
            return;

        SteamUserStats.SetAchievement(apiName);
        SteamUserStats.StoreStats();
    }

    // 런 결과 기반 해금 — 시드 직접 입력 런은 제외
    private void UnlockRun(string apiName)
    {
        ChallengeState cs = ChallengeState.Instance;
        if (cs != null && cs.HasCustomSeed)
            return;

        Unlock(apiName);
    }

    private void OnBossFightStarted()
    {
        _inBossFight = true;
        _bossFightClean = true;
    }

    private void OnBossDefeated()
    {
        if (_inBossFight && _bossFightClean)
            UnlockRun(BossNoDamage);

        _inBossFight = false;

        RunState run = RunState.Instance;
        if (run == null)
            return;

        switch (run.CurrentDepth)
        {
            case 5:
                UnlockRun(BossWatcher);
                break;
            case 10:
                UnlockRun(BossUndeadKing);
                break;
            case FinalDepth:
                UnlockRun(BossHades);
                break;
        }
    }

    private void OnDepthChanged(int depth)
    {
        if (depth >= Depth10Threshold)
            UnlockRun(Depth10);
    }

    private void OnPlayerDamaged(int amount)
    {
        if (_inBossFight)
            _bossFightClean = false;
    }

    private void OnPlayerDied()
    {
        Unlock(FirstDeath);
        FlushKills();
    }

    private void OnEnemyKilled(Vector2Int pos, float timeBonus)
    {
        _pendingKills++;
    }

    // 누적 처치 Stat 반영 — 런 종료 시 한 번만 커밋
    private void FlushKills()
    {
        if (_pendingKills <= 0 || !SteamBootstrap.IsReady || !_statsLoaded)
            return;

        if (!SteamUserStats.GetStat(StatTotalKills, out int total))
            return;

        total += _pendingKills;
        _pendingKills = 0;

        SteamUserStats.SetStat(StatTotalKills, total);
        SteamUserStats.StoreStats();

        if (total >= TotalKillsThreshold)
            Unlock(TotalKills1000);
    }

    private void OnScoreFinalized(ScoreResult result)
    {
        FlushKills();

        if (result.killCount >= KillsRunThreshold)
            UnlockRun(KillsRun50);

        if (result.residualBonus >= ResidualThreshold)
            UnlockRun(Residual300);

        if (result.finalScore >= Score10KThreshold)
            UnlockRun(Score10K);

        if (result.finalScore >= Score30KThreshold)
            UnlockRun(Score30K);

        ChallengeState cs = ChallengeState.Instance;
        if (cs != null && cs.IsDaily)
            UnlockRun(DailyFirst);

        // 챌린지 도전과제는 최종층 클리어 런만
        if (result.reachedDepth < FinalDepth)
            return;

        if (result.challengeCount >= 1)
            UnlockRun(ChallengeFirst);

        if (result.challengeCount >= Challenge3Threshold)
            UnlockRun(Challenge3);

        if (result.challengeCount >= ChallengeAllThreshold)
            UnlockRun(ChallengeAll);
    }

    private void OnLeaderboardUploaded(SteamLeaderboard.Board board, int rank)
    {
        if (board != SteamLeaderboard.Board.Daily)
            return;

        if (rank > 0 && rank <= DailyTopThreshold)
            UnlockRun(DailyTop10);
    }

    private void OnSecretDiscovered(Vector2Int cell)
    {
        Unlock(SecretFirst);
    }

    private void OnKeyAcquired(KeyType type)
    {
        _keysThisFloor.Add(type);
        if (_keysThisFloor.Count >= 2)
            Unlock(KeysBoth);
    }

    private void OnItemPurchased(ItemBase item)
    {
        Unlock(ShopFirst);
    }

    private void OnFloorReset()
    {
        _keysThisFloor.Clear();
        _inBossFight = false;
    }
}
