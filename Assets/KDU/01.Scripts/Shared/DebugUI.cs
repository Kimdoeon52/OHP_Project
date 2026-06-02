using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text daggerText;
    [SerializeField] private TMP_Text potionText;
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private TMP_Text statusText;

    [Header("디버그 단축키")]
    [Tooltip("이 키를 누르면 스페셜 방 열쇠 1개 지급(테스트용).")]
    [SerializeField] private KeyCode debugGrantSpecialKey = KeyCode.K;

    private GridPlayer _player;

    private void OnEnable()
    {
        GameEvents.OnPlayerDied += OnPlayerDied;
        GameEvents.OnTimeUp += OnTimeUp;
        GameEvents.OnLevelComplete += OnLevelComplete;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= OnPlayerDied;
        GameEvents.OnTimeUp -= OnTimeUp;
        GameEvents.OnLevelComplete -= OnLevelComplete;
    }

    private void Start()
    {
        _player = FindFirstObjectByType<GridPlayer>();
        if (statusText != null) statusText.text = "";
    }

    private void Update()
    {
        if (timeText != null && TimeSystem.Instance != null)
            timeText.text = $"TIME {TimeSystem.Instance.TimeRemaining:0.0}s";

        if (_player == null) return;

        if (hpText != null)
            hpText.text = $"HP {_player.CurrentHp}/{_player.MaxHp}";

        if (daggerText != null)
        {
            var durabilities = _player.Combat.InventoryDurabilities;
            daggerText.text = durabilities.Count == 0
                ? "단검 0"
                : $"단검 {durabilities.Count} [{string.Join(",", durabilities)}]";
        }

        if (potionText != null)
            potionText.text = $"포션 {_player.Health.PotionCount}";

        if (keyText != null && KeyInventory.Instance != null)
            keyText.text = $"열쇠 S:{KeyInventory.Instance.GetCount(KeyType.SpecialRoom)} R:{KeyInventory.Instance.GetCount(KeyType.RingChest)}";

        if (Input.GetKeyDown(debugGrantSpecialKey) && KeyInventory.Instance != null)
            KeyInventory.Instance.Grant(KeyType.SpecialRoom);
    }

    private void OnPlayerDied()    => SetStatus("사망 - 재시작 중...");
    private void OnTimeUp()        => SetStatus("시간 초과 - 재시작 중...");
    private void OnLevelComplete() => SetStatus("클리어! - 재시작 중...");

    private void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }
}
