using System.Collections.Generic;
using UnityEngine;
using PrototypeRT;

// 시작 시 인벤토리에 지급하는 아이템 (예: 비상 식량)
public class StartingItemGranter : MonoBehaviour
{
    [Tooltip("시작 지급 아이템")]
    [SerializeField] private List<ItemBase> startingItems = new();

    [Tooltip("인벤토리. 비우면 자동 탐색")]
    [SerializeField] private InventoryController inventory;

    // 이번 런에서 이미 지급했는지
    private static bool _granted;

    private void Start()
    {
        // 시작 층에서만 1회 지급
        if (_granted || RunState.Instance == null || RunState.Instance.CurrentDepth != 1)
            return;

        if (inventory == null)
            inventory = FindFirstObjectByType<InventoryController>();

        if (inventory == null)
            return;

        foreach (ItemBase item in startingItems)
        {
            if (item == null)
                continue;

            inventory.TryAddItem(item.CreateInstance());
        }

        _granted = true;
    }

    // 런 재시작 시 재지급 허용
    public static void ResetForNewRun()
    {
        _granted = false;
    }
}
