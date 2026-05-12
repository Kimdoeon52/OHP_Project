using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] protected int maxHp;
    [SerializeField] public int currentHp;

    [Header("속도")]
    [SerializeField] public float moveSpeed;

    [Header("인식했는지")]
    [SerializeField] public bool isDetected = false;

    public virtual void TakeDamage(int damage)
    {
        currentHp -= damage;
        if (currentHp <= 0)
        {
            //Die();
        }
    }
}
