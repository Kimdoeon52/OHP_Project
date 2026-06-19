using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

// 엔딩 씬 연출 재생 후(또는 스킵 시) 메인 화면 복귀. KDU_Ending 씬에 1개 배치.
public class EndingSequenceController : MonoBehaviour
{
    [Header("연출")]
    [Tooltip("타임라인 (선택). 있으면 길이만큼 대기")]
    [SerializeField] private PlayableDirector director;
    [Tooltip("타임라인 없을 때 자동 진행 시간(초)")]
    [SerializeField, Min(0f)] private float fallbackDurationSeconds = 8f;

    [Header("스킵")]
    [SerializeField] private KeyCode[] skipKeys = { KeyCode.Space, KeyCode.Escape, KeyCode.Return };
    [Tooltip("스킵 버튼 (선택)")]
    [SerializeField] private Button skipButton;

    [Header("복귀")]
    [Tooltip("로딩 씬 이름. 비우면 메인 씬 직접 로드")]
    [SerializeField] private string loadingSceneName = "Loading";
    [Tooltip("메인 화면 씬 이름. 비우면 Build Settings 0번 씬")]
    [SerializeField] private string mainMenuSceneName = "NYH_MainScene";

    private bool _finished;

    private void OnEnable()
    {
        if (skipButton != null)
            skipButton.onClick.AddListener(Skip);
    }

    private void OnDisable()
    {
        if (skipButton != null)
            skipButton.onClick.RemoveListener(Skip);
    }

    private void Start()
    {
        if (director != null)
            director.Play();

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        float target = director != null ? (float)director.duration : fallbackDurationSeconds;
        float elapsed = 0f;
        while (!_finished && elapsed < target)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Finish();
    }

    private void Update()
    {
        if (_finished)
            return;

        for (int i = 0; i < skipKeys.Length; i++)
        {
            if (Input.GetKeyDown(skipKeys[i]))
            {
                Skip();
                return;
            }
        }
    }

    // 버튼/키 스킵
    public void Skip()
    {
        if (_finished)
            return;

        if (director != null)
            director.Stop();

        Finish();
    }

    private void Finish()
    {
        if (_finished)
            return;
        _finished = true;
        ReturnToMain();
    }

    private void ReturnToMain()
    {
        Time.timeScale = 1f;

        bool hasLoading = !string.IsNullOrWhiteSpace(loadingSceneName);
        bool hasMain = !string.IsNullOrWhiteSpace(mainMenuSceneName);

        if (hasLoading && hasMain)
        {
            // 로딩 씬 경유 — LoadingSceneController가 PendingTarget을 읽어 메인 씬 로드
            MoveScene.SetPendingTarget(mainMenuSceneName);
            SceneManager.LoadScene(loadingSceneName);
        }
        else if (hasMain)
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(0); // Build Settings 첫 씬 = 타이틀
        }
    }
}
