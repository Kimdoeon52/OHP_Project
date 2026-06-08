using UnityEngine;

/// <summary>
/// 런 스코프 골드 잔액 단일 보유자 (GDD 11.4 상점 경제의 기반).
/// RunState 와 같은 수명 — 층 이동(같은 씬)엔 유지되고, 사망/새 런 시 씬 리로드로 새로 생성되어 0 으로 초기화된다.
/// (그래서 OnPlayerDied 구독·DontDestroyOnLoad 가 필요 없다.)
///
/// 계약:
///   - 획득: GameEvents.OnGoldDropped(pos, amount) 구독 → 가산. (적/상자/GoldPickup 이 줍는 순간 발행)
///   - 소비: TrySpend(amount) — 잔액 충분하면 차감 후 true. (상점 ShopStall 이 호출)
///   - 변동: 잔액이 바뀌면 GameEvents.OnGoldChanged(잔액) 발행 → HUD 가 구독.
/// 권장 배치: RunState 와 같은 GameObject.
/// </summary>
public class Wallet : Singleton<Wallet>
{
    public int Balance { get; private set; }

    private void OnEnable()
    {
        GameEvents.OnGoldDropped += OnGoldAcquired;
    }

    private void OnDisable()
    {
        GameEvents.OnGoldDropped -= OnGoldAcquired;
    }

    private void Start()
    {
        // 시작 잔액(0)을 HUD 에 한 번 알림 — 구독은 OnEnable 단계에서 이미 끝나 있다.
        GameEvents.OnGoldChanged?.Invoke(Balance);
    }

    private void OnGoldAcquired(Vector2Int pos, int amount)
    {
        if (amount <= 0) return;
        Balance += amount;
        GameEvents.OnGoldChanged?.Invoke(Balance);
    }

    /// <summary>잔액이 충분하면 차감하고 true. 부족하면 잔액을 유지하고 false (상점이 구매 가능 여부 판정에 사용).</summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0 || Balance < amount) return false;
        Balance -= amount;
        GameEvents.OnGoldChanged?.Invoke(Balance);
        return true;
    }
}
