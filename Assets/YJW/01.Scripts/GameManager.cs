using UnityEngine;

public class GameManager : PersistentSingleton<GameManager>
{
    public GameObject player { get; private set; }

    public bool isAlerted = false;   // 경보 저주 발동 여부

    protected override void Awake()
    {
        base.Awake();

        player = GameObject.FindWithTag("Player");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
