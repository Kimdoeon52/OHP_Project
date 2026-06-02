using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 열쇠 인벤토리 (GDD 9.15). 종류별 보유 수를 카운트로 관리.
/// 해당 층에서 획득한 열쇠는 해당 층에서만 사용 가능 → MapGenerator.Generate 호출 시 초기화.
/// </summary>
public enum KeyType
{
    SpecialRoom, // 스페셜 방 열쇠
    RingChest    // 반지 상자 열쇠
}

public class KeyInventory : MonoBehaviour
{
    public static KeyInventory Instance { get; private set; }

    private readonly Dictionary<KeyType, int> _counts = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("KeyInventory 가 씬에 두 개 이상 있습니다.");
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int GetCount(KeyType type) => _counts.TryGetValue(type, out int n) ? n : 0;
    public bool Has(KeyType type) => GetCount(type) > 0;

    public void Grant(KeyType type, int amount = 1)
    {
        if (amount <= 0) return;
        _counts[type] = GetCount(type) + amount;
    }

    public bool TryConsume(KeyType type, int amount = 1)
    {
        int have = GetCount(type);
        if (have < amount) return false;
        _counts[type] = have - amount;
        return true;
    }

    /// <summary>층 변경 시 호출 — 보유 열쇠 전부 폐기 (GDD 9.15: 해당 층에서만 사용 가능).</summary>
    public void ClearAll() => _counts.Clear();
}
