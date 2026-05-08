using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerStats.Instance.OnStatChanged += UpdateUI;
    }

    private void OnDisable()
    {
        PlayerStats.Instance.OnStatChanged -= UpdateUI;
    }

    private void UpdateUI(StatType type, int amount)
    {
        // 플레이어 스탯 관련 UI 업데이트 로직
    }
}
