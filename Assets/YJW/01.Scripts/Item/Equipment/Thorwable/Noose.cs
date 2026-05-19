using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "Noose", menuName = "Item/Equipment/Throwable/Noose")]
public class Noose : ThrowableWeapon, IHitEffect
{
    [SerializeField] int effectDuration;
    void ApplyEffect(EnemyBase enemy)
    {
        enemy.TakeDamage(GetDamage());
        enemy.moveSpeed *= 0.5f;
        ReleaseEffect(enemy).Forget();
    }

    async UniTask ReleaseEffect(EnemyBase enemy)
    {
        await UniTask.Delay(effectDuration * 1000);
        enemy.moveSpeed /= 0.5f;
    }
}
