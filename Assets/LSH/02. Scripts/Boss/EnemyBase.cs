using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] protected int maxHp;
    [SerializeField] protected int currentHp;

    [Header("속도")]
    [SerializeField] protected float moveSpeed;

    [Header("인식했는지")]
    [SerializeField] protected bool isDetected = false;
}
