using UnityEngine;

public class GameManager : PersistentSingleton<GameManager>
{
    private GameObject _player;
    public GameObject player
    {
        get
        {
            if (_player == null)
            {
                _player = GameObject.FindWithTag("Player");
            }
            return _player;
        }
    }

    public bool isAlerted = false;   // 경보 저주 발동 여부

    protected override void Awake()
    {
        base.Awake();

        _player = GameObject.FindWithTag("Player");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
