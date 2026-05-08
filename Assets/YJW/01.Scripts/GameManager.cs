using UnityEngine;

public class GameManager : PersistentSingleton<GameManager>
{
    public GameObject player { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        player = GameObject.FindWithTag("Player");
    }
}
