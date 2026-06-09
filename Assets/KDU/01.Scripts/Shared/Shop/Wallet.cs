using UnityEngine;

// 골드
public class Wallet : Singleton<Wallet>
{
    public int Balance { get; private set; }

    private void OnEnable()
    {
        GameEvents.OnGoldDropped += OnGoldAcquired;
        GameEvents.OnPlayerDied += ResetBalance;
    }

    private void OnDisable()
    {
        GameEvents.OnGoldDropped -= OnGoldAcquired;
        GameEvents.OnPlayerDied -= ResetBalance;
    }

    // 사망 시 잔액 0
    private void ResetBalance()
    {
        Balance = 0;
        GameEvents.OnGoldChanged?.Invoke(Balance);
    }

    private void Start()
    {
        // 시작 잔액을 HUD 에 1회 알림
        GameEvents.OnGoldChanged?.Invoke(Balance);
    }

    // 골드 획득
    private void OnGoldAcquired(Vector2Int pos, int amount)
    {
        if (amount <= 0)
            return;
        Balance += amount;
        GameEvents.OnGoldChanged?.Invoke(Balance);
    }

    // 잔액 충분하면 골드 차감
    public bool TrySpend(int amount)
    {
        if (amount <= 0 || Balance < amount)
            return false;
        Balance -= amount;
        GameEvents.OnGoldChanged?.Invoke(Balance);
        return true;
    }
}
