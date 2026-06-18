using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrototypeGameFlow : MonoBehaviour
{
    [SerializeField] private float restartDelay = 5f;

    private bool _gameEnded;

    private void OnEnable()
    {
        GameEvents.OnTimeUp += OnGameEnd;
        GameEvents.OnPlayerDied += OnPlayerDied;
    }

    private void OnDisable()
    {
        GameEvents.OnTimeUp -= OnGameEnd;
        GameEvents.OnPlayerDied -= OnPlayerDied;
    }

    // 타임업 = 사망과 동일 처리. 영구사망이므로 세이브 삭제 후 새 런으로 리로드.
    private void OnGameEnd()
    {
        if (_gameEnded)
            return;

        _gameEnded = true;
        SaveManager.Delete();
        StartCoroutine(RestartAfterDelay());
    }

    // 사망 = 영구사망. 세이브 삭제 → 다음 실행은 1층부터. 리로드는 사망 UI가 담당.
    private void OnPlayerDied()
    {
        SaveManager.Delete();
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        GameEvents.ClearAllEvents();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
