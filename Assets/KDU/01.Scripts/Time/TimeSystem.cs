using UnityEngine;

public class TimeSystem : MonoBehaviour
{
    public static TimeSystem Instance { get; private set; }

    [SerializeField] private GameConfig config;

    public float TimeRemaining { get; private set; }
    public bool IsRunning { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("TimeSystem이 씬에 두 개 이상 있음");
            return;
        }
        Instance = this;
        TimeRemaining = config != null ? config.startTime : 60f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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
