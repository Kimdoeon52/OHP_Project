using UnityEngine;

public class EnemyNormalBASE : MonoBehaviour
{
    [Header("보스 데이터")]
    [SerializeField] public int maxHp;
    [SerializeField] public int currentHp;
    [SerializeField] public int stepDelayMs; //이동 딜레이
}
