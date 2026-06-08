using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text potionText;
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private TMP_Text statusText;

    private GridPlayer _player;

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


        if (potionText != null)
            potionText.text = $"포션 {_player.Health.PotionCount}";

        if (keyText != null && KeyInventory.Instance != null)
            keyText.text = $"열쇠 S:{KeyInventory.Instance.GetCount(KeyType.SpecialRoom)} R:{KeyInventory.Instance.GetCount(KeyType.RingChest)}";
    }

    private void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }
}

