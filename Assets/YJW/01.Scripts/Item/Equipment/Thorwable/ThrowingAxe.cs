using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "ThrowingAxe", menuName = "Item/Equipment/Throwable/ThrowingAxe")]
public class ThrowingAxe : ThrowableWeapon, IHitEffect
{
    [SerializeField] int duration;
    [SerializeField] int minBleedingDamage;
    [SerializeField] int maxBleedingDamage;

    public void ApplyEffect(EnemyBase enemy, int damage)
    {
        enemy.TakeDamage(damage);
        BleedingDamgae(enemy).Forget();
    }

    private async UniTask BleedingDamgae(EnemyBase enemy)
    {
        for (int i = 0; i < duration; i++)
        {
            int bleedingDamage = Random.Range(minBleedingDamage, maxBleedingDamage + 1);
            enemy.TakeDamage(bleedingDamage);
            await UniTask.Delay(1000);
        }
    }
}
