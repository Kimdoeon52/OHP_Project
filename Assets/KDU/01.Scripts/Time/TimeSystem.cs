using UnityEngine;

public class TimeSystem : Singleton<TimeSystem>
{
    [SerializeField] private GameConfig config;

    public float TimeRemaining { get; private set; }
    public bool IsRunning { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        TimeRemaining = config != null ? config.startTime : 60f;
    }

    public void StartTimer() => IsRunning = true;
    public void StopTimer() => IsRunning = false;

    public void ResetForNewFloor()
    {
        TimeRemaining = config != null ? config.startTime : 60f;
        IsRunning = false;
        GameEvents.OnTimeChanged?.Invoke(TimeRemaining);
    }

    private void Update()
    {
        if (!IsRunning) return;

        TimeRemaining -= Time.deltaTime;
        GameEvents.OnTimeChanged?.Invoke(TimeRemaining);

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            IsRunning = false;
            GameEvents.OnTimeUp?.Invoke();
        }
    }

    public void ConsumeTime(float seconds)
    {
        if (!IsRunning) return;

        TimeRemaining = Mathf.Max(0f, TimeRemaining - seconds);
        GameEvents.OnTimeChanged?.Invoke(TimeRemaining);

        if (TimeRemaining <= 0f)
        {
            IsRunning = false;
            GameEvents.OnTimeUp?.Invoke();
        }
    }

    // 적 처치 보너스 등 시간 추가
    public void AddTime(float seconds)
    {
        TimeRemaining += seconds;
        GameEvents.OnTimeChanged?.Invoke(TimeRemaining);
    }
}
