using UnityEngine;

public class WeaponProjectile : MonoBehaviour
{
    [SerializeField] private ThrowableWeapon weapon;

    public void FlyNoEnemy()
    {

    }

    public void FlyEnemy()
    {

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Enemy"))
        {
            if(collision.gameObject.TryGetComponent(out IHitEffect hitEffect))
            {
                hitEffect.ApplyEffect(collision.gameObject.GetComponent<EnemyBase>());
            }
            else
            {
                collision.gameObject.GetComponent<EnemyBase>().TakeDamage(weapon.damage);
            }
        }
    }
}
