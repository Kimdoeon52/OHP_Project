using TMPro;
using UnityEngine;

// 골드 잔액 HUD
public class GoldHud : MonoBehaviour
{
    [Tooltip("골드 표시 텍스트")]
    [SerializeField] private TMP_Text goldText;
    [Tooltip("접두사")]
    [SerializeField] private string prefix = " G";

    private void OnEnable()
    {
        GameEvents.OnGoldChanged += Refresh;
        if (Wallet.Instance != null)
            Refresh(Wallet.Instance.Balance);
    }

    private void OnDisable()
    {
        GameEvents.OnGoldChanged -= Refresh;
    }

    // 잔액 갱신
    private void Refresh(int balance)
    {
        if (goldText != null)
            goldText.text = balance + prefix;
    }
}
