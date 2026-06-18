using UnityEngine;

public class PrototypeGameFlow : MonoBehaviour
{
    private bool _gameEnded;

    private void OnEnable()
    {
        GameEvents.OnTimeUp += OnTimeUp;
        GameEvents.OnPlayerDied += OnPlayerDied;
    }

    private void OnDisable()
    {
        GameEvents.OnTimeUp -= OnTimeUp;
        GameEvents.OnPlayerDied -= OnPlayerDied;
    }

    private void OnTimeUp()
    {
        if (_gameEnded)
            return;

        _gameEnded = true;
        SaveManager.Delete();
    }

    private void OnPlayerDied()
    {
        // 사망/시간초과 후 재시작 선택 UI에서 새 런을 시작하므로 기존 저장은 제거한다.
        SaveManager.Delete();
    }
}
