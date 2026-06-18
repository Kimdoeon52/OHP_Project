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

        // 골드 획득량 특성은 드랍 생성량이 아니라 실제 지갑에 들어오는 시점에 곱한다.
        // 이렇게 하면 이미 떨어진 골드 오브젝트 구조를 바꾸지 않아도 배율 효과만 안전하게 적용된다.
        TraitController traitController = TraitController.FindAvailableInstance();
        float multiplier = traitController != null ? traitController.GetGoldMultiplier() : 1f;
        int finalAmount = Mathf.Max(0, Mathf.RoundToInt(amount * multiplier));
        Balance += finalAmount;
        GameEvents.OnGoldChanged?.Invoke(Balance);

        // 탐욕 특성 배율까지 적용된 실제 획득량을 보여줘야 HUD 증가량과 플로팅 텍스트가 어긋나지 않는다.
        Vector3 worldPosition = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(pos)
            : new Vector3(pos.x, pos.y, 0f);
        GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(
            worldPosition,
            $"+{finalAmount}G",
            finalAmount,
            CombatTextType.Gold));
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
