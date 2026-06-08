using UnityEngine;
using PrototypeRT;

// 탐색 액션
public class SearchAction : MonoBehaviour
{
    [SerializeField] private GridPlayer player;
    [Tooltip("탐색 입력 키.")]
    [SerializeField] private KeyCode searchKey = KeyCode.F;
    [Tooltip("탐색 범위")]
    [SerializeField, Min(0)] private int searchRadius = 3;

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<GridPlayer>();
    }

    private void Update()
    {
        if (player == null || player.IsDead) 
            return;

        if (KeyBindingManager.ShouldBlockGameplayInput)
            return;

        if (!Input.GetKeyDown(KeyBindingManager.GetKey(KeyBindingAction.Search)))
            return;

        GameEvents.OnSearchPerformed?.Invoke(player.GridPosition, searchRadius);
    }
}
