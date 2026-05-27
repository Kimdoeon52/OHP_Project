using UnityEngine;

[CreateAssetMenu(fileName = "Kunai", menuName = "Item/Equipment/Throwable/Kunai")]
public class Kunai : ThrowableWeapon, IHitEffect
{
    public void ApplyEffect(EnemyBase enemy, int damage)
    {
        if (enemy.isDetected)
            enemy.TakeDamage(damage * 2);
        else
            enemy.TakeDamage(damage);
    }
}
