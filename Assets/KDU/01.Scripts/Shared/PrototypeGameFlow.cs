using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        if (_gameEnded) 
            return;

        _gameEnded = true;
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        GameEvents.ClearAllEvents();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
