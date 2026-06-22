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
            await UniTask.Delay(1000);

            if (enemy == null || enemy.IsDead)
                return;

            int bleedingDamage = Random.Range(minBleedingDamage, maxBleedingDamage + 1);
            int beforeHp = enemy.CurrentHp;
            enemy.TakeDamage(bleedingDamage);
            int finalDamage = Mathf.Max(0, beforeHp - enemy.CurrentHp);
            PublishBleedingDamageText(enemy, finalDamage);
        }
    }

    private static void PublishBleedingDamageText(EnemyBase enemy, int damage)
    {
        if (enemy == null || damage <= 0)
            return;

        // 출혈은 EnemyBase.TakeDamage를 직접 호출하므로, 투척 투사체 쪽 보강만으로는 숫자가 뜨지 않는다.
        GameEvents.OnCombatTextRequested?.Invoke(
            new CombatTextRequest(enemy.transform.position, damage.ToString(), damage, CombatTextType.Damage));
    }
}
