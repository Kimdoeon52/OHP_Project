using UnityEngine;
using PrototypeRT;

/// <summary>
/// 탐색 액션 — searchKey 를 누르면 플레이어 기준 체비셰프 searchRadius 안(1=3x3)의
/// 숨겨진 것들을 드러낸다. GameEvents.OnSearchPerformed 발행만 하고, 실제 공개는
/// 각 대상(GridDoor 의 Secret 문, 추후 숨겨진 함정 등)이 구독해 스스로 처리한다.
/// (DebugUI 의 L 키는 맵 전체 시크릿을 까는 디버그용 — 이건 범위 제한 정식 액션.)
/// </summary>
public class SearchAction : MonoBehaviour
{
    [SerializeField] private GridPlayer player;
    [Tooltip("탐색 입력 키.")]
    [SerializeField] private KeyCode searchKey = KeyCode.F;
    [Tooltip("탐색 범위 — 체비셰프 거리(1 = 플레이어 기준 3x3).")]
    [SerializeField, Min(0)] private int searchRadius = 1;

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<GridPlayer>();
    }

    private void Update()
    {
        if (player == null || player.IsDead) return;
        if (KeyBindingManager.ShouldBlockGameplayInput) return;
        if (!Input.GetKeyDown(KeyBindingManager.GetKey(KeyBindingAction.Search))) return;

        GameEvents.OnSearchPerformed?.Invoke(player.GridPosition, searchRadius);
    }
}
