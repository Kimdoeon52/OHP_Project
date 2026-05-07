using UnityEngine;

public class BossBase : MonoBehaviour
{
    [SerializeField] protected int MaxHp;
    [SerializeField] protected int CurrentHp;

    protected virtual void Start()
    {
    }
}
