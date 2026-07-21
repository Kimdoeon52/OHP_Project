using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text levelText;

    private PlayerLevelSystem _levelSystem;
    private GridPlayer _player;

    private void Start()
    {
        _player = FindFirstObjectByType<GridPlayer>();
        _levelSystem = _player != null ? _player.GetComponent<PlayerLevelSystem>() : FindFirstObjectByType<PlayerLevelSystem>();
    }

    private void Update()
    {
        if (timeText != null && TimeSystem.Instance != null)
        {
            if (float.IsInfinity(TimeSystem.Instance.TimeRemaining))
            {
                timeText.text = "TIME ???";
            }
            else
            {
                timeText.text = $"TIME {TimeSystem.Instance.TimeRemaining:0.0}s";
            }
        }

        if (_player == null) return;

        if (hpText != null)
            hpText.text = $"HP {_player.CurrentHp}/{_player.MaxHp}";

        if (levelText != null && _levelSystem != null)
            levelText.text = $"LV {_levelSystem.Level}";

        if (expText != null && _levelSystem != null)
            expText.text = $"EXP {_levelSystem.ExpDisplayText}";
    }
}

