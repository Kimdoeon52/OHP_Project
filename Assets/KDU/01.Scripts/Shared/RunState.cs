using UnityEngine;

public class RunState : Singleton<RunState>
{
    [Header("시드 설정")]
    [SerializeField] private int initialSeed;
    [SerializeField] private int startDepth = 1;
    // 시작 시 시드를 랜덤으로 할지 여부
    [SerializeField] private bool randomizeSeedOnStart = true;

    public int RunSeed { get; private set; }
    public int CurrentDepth { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        RunSeed = randomizeSeedOnStart ? Random.Range(int.MinValue, int.MaxValue) : initialSeed;
        CurrentDepth = startDepth;
    }

    public void AdvanceDepth()
    {
        CurrentDepth++;
        GameEvents.OnDepthChanged?.Invoke(CurrentDepth);
    }

    public void ResetRun(int seed, int depth = 1)
    {
        RunSeed = seed;
        CurrentDepth = depth;
        StartingItemGranter.ResetForNewRun();
        GameEvents.OnDepthChanged?.Invoke(CurrentDepth);
    }
}
