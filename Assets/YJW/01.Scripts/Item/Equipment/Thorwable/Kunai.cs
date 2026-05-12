using UnityEngine;

[CreateAssetMenu(fileName = "Kunai", menuName = "Item/Equipment/Throwable/Kunai")]
public class Kunai : ThrowableWeapon, IHitEffect
{
    void ApplyEffect(EnemyBase enemy)
    {
        if(enemy.isDetected == true)
            enemy.TakeDamage(damage * 2); // 적이 인식된 상태라면 더 큰 피해를 입힘
        else
            enemy.TakeDamage(damage); // 적이 인식되지 않은 상태라면 일반 피해
    }
}
