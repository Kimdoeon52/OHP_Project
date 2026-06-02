using UnityEngine;

public class RunState : Singleton<RunState>
{
    [Header("초기값")]
    [SerializeField] private int initialSeed;
    [SerializeField] private int startDepth = 1;
    [Tooltip("켜져 있으면 Awake 에서 시스템 난수로 시드 채움. 끄면 initialSeed 사용.")]
    [SerializeField] private bool randomizeSeedOnStart = true;

    public int RunSeed { get; private set; }
    public int CurrentDepth { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        RunSeed = randomizeSeedOnStart ? Random.Range(int.MinValue, int.MaxValue) : initialSeed;
        CurrentDepth = startDepth;
    }

    public void AdvanceDepth() => CurrentDepth++;

    /// <summary>씬 재시작이 아닌 같은 씬 내 런 초기화 용도. 시드는 호출자가 결정.</summary>
    public void ResetRun(int seed, int depth = 1)
    {
        RunSeed = seed;
        CurrentDepth = depth;
    }
}
