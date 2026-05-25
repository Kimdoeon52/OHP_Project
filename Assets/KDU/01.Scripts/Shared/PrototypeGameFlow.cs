using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 종료(사망/시간초과) 감지 → 5초 후 씬 재시작.
/// 정적 GameEvents 구독 정리 후 SceneManager.LoadScene.
/// 층 클리어(OnLevelComplete) 는 FloorTransitionController 가 처리하므로 여기서 구독하지 않음.
/// </summary>
public class PrototypeGameFlow : MonoBehaviour
{
    [SerializeField] private float restartDelay = 5f;

    private bool _gameEnded;

    private void OnEnable()
    {
        GameEvents.OnPlayerDied += OnGameEnd;
        GameEvents.OnTimeUp += OnGameEnd;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= OnGameEnd;
        GameEvents.OnTimeUp -= OnGameEnd;
    }

    private void OnGameEnd()
    {
        if (_gameEnded) return;
        _gameEnded = true;
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        // 정적 이벤트는 씬 전환으로 안 정리되므로 직접 비움
        GameEvents.ClearAllEvents();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
