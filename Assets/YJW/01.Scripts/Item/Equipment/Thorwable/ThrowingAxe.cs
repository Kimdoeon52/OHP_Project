using Cysharp.Threading.Tasks;
using UnityEngine;

public class ThrowingAxe : ThrowableWeapon, IHitEffect
{
    [SerializeField] int duration;
    [SerializeField] int minDamage;
    [SerializeField] int maxDamage;

    void ApplyEffect(EnemyBase enemy)
    {
        enemy.TakeDamage(damage);

        for (int i = 0; i < duration; i++)
        {
            BleedingDamgae(enemy).Forget();
        }
    }

    private async UniTask BleedingDamgae(EnemyBase enemy)
    {
        int bleedingDamage = Random.Range(minDamage, maxDamage + 1);
        enemy.TakeDamage(bleedingDamage);
        await UniTask.Delay(1000);
    }
}
