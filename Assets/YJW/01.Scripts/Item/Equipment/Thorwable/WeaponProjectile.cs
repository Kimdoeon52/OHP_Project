using UnityEngine;

public class WeaponProjectile : MonoBehaviour
{
    [SerializeField] private PoolType poolType;
    [SerializeField] private float speed = 5f;

    private ThrowableWeaponInstance weaponInstance;
    private bool isFlying = false;
    private Vector2 flyDirection;

    public void SetInstance(ThrowableWeaponInstance instance)
    {
        weaponInstance = instance;
    }

    public void Fly(Vector2 direction)
    {
        flyDirection = direction.normalized;
        isFlying = true;
    }

    private void Update()
    {
        if (!isFlying) return;
        transform.position += (Vector3)(flyDirection * speed * Time.deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyBase enemy = collision.gameObject.GetComponent<EnemyBase>();
            int damage = weaponInstance.GetDamage();
            weaponInstance.curse?.OnAttack(ref damage);

            if (weaponInstance.weapon is IHitEffect hitEffect)
                hitEffect.ApplyEffect(enemy, damage);
            else
                enemy.TakeDamage(damage);
        }

        isFlying = false;
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        ObjectPool.Instance.Return(poolType, gameObject);
    }
}
