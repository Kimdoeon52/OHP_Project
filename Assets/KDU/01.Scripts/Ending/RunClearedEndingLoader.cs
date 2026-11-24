using PrototypeRT;
using UnityEngine;
using UnityEngine.SceneManagement;

// 최종층 클리어 시 결산 팝업을 닫으면 엔딩 씬 로드 (결산 패널 없으면 즉시). 게임플레이 씬에 1개 배치.
public class RunClearedEndingLoader : MonoBehaviour
{
    private const string InputBlockSource = "RunCleared";

    [Tooltip("엔딩 씬 이름 — Build Settings 등록 필요")]
    [SerializeField] private string endingSceneName = "KDU_Ending";

    private bool _cleared;

    private void OnEnable()
    {
        GameEvents.OnRunCleared += OnRunCleared;
        GameEvents.OnRunResultClosed += LoadEnding;
    }

    private void OnDisable()
    {
        GameEvents.OnRunCleared -= OnRunCleared;
        GameEvents.OnRunResultClosed -= LoadEnding;
        KeyBindingManager.SetUiInputBlock(InputBlockSource, false);
    }

    // 클리어 — 타이머·게임 입력 정지 후 결산 대기
    private void OnRunCleared()
    {
        _cleared = true;
        TimeSystem.Instance?.StopTimer();
        KeyBindingManager.SetUiInputBlock(InputBlockSource, true);

        if (!RunResultPanel.IsActive)
            LoadEnding();
    }

    private void LoadEnding()
    {
        if (!_cleared)
            return;

        if (string.IsNullOrWhiteSpace(endingSceneName))
        {
            Debug.LogError("[RunClearedEndingLoader] 엔딩 씬 이름 비었음");
            return;
        }

        _cleared = false;
        KeyBindingManager.SetUiInputBlock(InputBlockSource, false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(endingSceneName);
    }
}
