using UnityEngine;

/// <summary>
/// Area 3 적 및 맵 테스트를 위한 디버그 이동 스크립트입니다.
/// Delete 키를 누르면 바로 Area 3(11층)으로 이동합니다.
/// </summary>
public class Area3Tester : MonoBehaviour
{
    [Header("단축키 및 목표 층 설정")]
    [Tooltip("Area 3 이동 단축키 (기본값: Delete)")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Delete;

    [Tooltip("목표 층 (Area 3 시작 층: 11층)")]
    [SerializeField] private int targetDepth = 11;

    [Header("디버그 UI")]
    [Tooltip("화면 좌측 상단에 단축키 안내 표시 여부")]
    [SerializeField] private bool showOnScreenGuide = true;

    private void Update()
    {
        if (Input.GetKeyDown(jumpKey))
        {
            JumpToTargetDepth();
        }
    }

    /// <summary>
    /// 지정한 목표 층(기본 11층 Area 3)으로 즉시 이동합니다.
    /// </summary>
    public void JumpToTargetDepth()
    {
        if (RunState.Instance != null)
        {
            Debug.Log($"<color=cyan>[Area3Tester]</color> <b>{jumpKey}</b> 키 입력 감지: {targetDepth}층 (Area 3)으로 이동을 시작합니다.");

            // FloorTransitionController의 TransitionCoroutine에서 AdvanceDepth()가 1층을 더하므로,
            // (targetDepth - 1)로 설정 후 레벨 완료 이벤트를 발생시킵니다.
            RunState.Instance.ResetRun(RunState.Instance.RunSeed, targetDepth - 1);
            GameEvents.OnLevelComplete?.Invoke();
        }
        else
        {
            Debug.LogWarning("[Area3Tester] RunState.Instance를 찾을 수 없어 층 이동을 실행할 수 없습니다.");
        }
    }

    private void OnGUI()
    {
        if (!showOnScreenGuide) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.yellow;

        GUI.Label(new Rect(15, 15, 400, 30), $"[{jumpKey}] 키: Area 3 ({targetDepth}층) 이동", style);
    }
}
