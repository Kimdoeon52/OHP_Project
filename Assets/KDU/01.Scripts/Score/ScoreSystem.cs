using UnityEngine;

// 런 진행 중 점수 항목 누적, 종료 시 최종 점수 확정. GDD 13.3.
public class ScoreSystem : Singleton<ScoreSystem>
{
    // 챌린지당 점수 배수 밑. 활성 수만큼 거듭제곱.
    private const float ChallengeMultiplierBase = 1.5f;

    private float _residualTime;   // 각 층 잔여 시간 합
    private int _bossBonus;        // 보스 클리어 보너스 합
    private float _killBonus;      // 처치 몬스터 시간보너스 합
    private bool _finalized;

    public ScoreResult LastResult { get; private set; }

    private void OnEnable()
    {
        GameEvents.OnEnemyKilled += OnEnemyKilled;
        GameEvents.OnLevelComplete += OnFloorCleared;
        GameEvents.OnBossDefeated += OnBossDefeated;
        GameEvents.OnPlayerDied += Finalize;
        GameEvents.OnTimeUp += Finalize;
        GameEvents.OnRunCleared += Finalize;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyKilled -= OnEnemyKilled;
        GameEvents.OnLevelComplete -= OnFloorCleared;
        GameEvents.OnBossDefeated -= OnBossDefeated;
        GameEvents.OnPlayerDied -= Finalize;
        GameEvents.OnTimeUp -= Finalize;
        GameEvents.OnRunCleared -= Finalize;
    }

    private void OnEnemyKilled(Vector2Int pos, float timeBonus)
    {
        _killBonus += timeBonus;
    }

    // 출구 진입 = 층 클리어. 잔여 시간을 누적.
    private void OnFloorCleared()
    {
        if (TimeSystem.Instance != null)
            _residualTime += TimeSystem.Instance.TimeRemaining;
    }

    private void OnBossDefeated()
    {
        int depth = RunState.Instance != null ? RunState.Instance.CurrentDepth : 0;
        _bossBonus += BossBonusForDepth(depth);
    }

    private static int BossBonusForDepth(int depth)
    {
        switch (depth)
        {
            case 5: return 500;
            case 10: return 1000;
            case 15: return 2000;
            default: return 0;
        }
    }

    // 런 종료(사망/타임업/클리어) 1회만 점수 확정.
    private void Finalize()
    {
        if (_finalized)
            return;

        _finalized = true;

        int reachedDepth = RunState.Instance != null ? RunState.Instance.CurrentDepth : 0;
        int challengeCount = ChallengeState.Instance != null ? ChallengeState.Instance.ActiveCount : 0;

        var r = new ScoreResult();
        r.seed = RunState.Instance != null ? RunState.Instance.RunSeed : 0;
        r.reachedDepth = reachedDepth;
        r.depthBonus = reachedDepth * 100;
        r.residualBonus = Mathf.RoundToInt(_residualTime);
        r.bossBonus = _bossBonus;
        r.killBonus = Mathf.RoundToInt(_killBonus);
        r.baseScore = r.depthBonus + r.residualBonus + r.bossBonus + r.killBonus;
        r.challengeCount = challengeCount;
        r.multiplier = Mathf.Pow(ChallengeMultiplierBase, challengeCount);
        r.finalScore = Mathf.RoundToInt(r.baseScore * r.multiplier);

        LastResult = r;
        Scoreboard.Submit(r);
        GameEvents.OnScoreFinalized?.Invoke(r);
    }
}
