using UnityEngine;
using UnityEngine.SceneManagement;

// 최종층 클리어(OnRunCleared) 시 엔딩 씬 로드. 게임플레이 씬에 1개 배치.
public class RunClearedEndingLoader : MonoBehaviour
{
    [Tooltip("엔딩 씬 이름 — Build Settings 등록 필요")]
    [SerializeField] private string endingSceneName = "KDU_Ending";

    private void OnEnable()
    {
        GameEvents.OnRunCleared += LoadEnding;
    }

    private void OnDisable()
    {
        GameEvents.OnRunCleared -= LoadEnding;
    }

    private void LoadEnding()
    {
        if (string.IsNullOrWhiteSpace(endingSceneName))
        {
            Debug.LogError("[RunClearedEndingLoader] 엔딩 씬 이름 비었음");
            return;
        }

        // 다른 OnRunCleared 구독자(점수 확정 등)는 같은 프레임에 끝나고,
        // 실제 씬 전환은 프레임 끝에 일어나므로 직접 로드해도 안전.
        Time.timeScale = 1f;
        SceneManager.LoadScene(endingSceneName);
    }
}
