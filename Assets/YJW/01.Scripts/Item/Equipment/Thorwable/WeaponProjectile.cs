using PrototypeRT;
using UnityEngine;

public class WeaponProjectile : MonoBehaviour
{
    [SerializeField] private PoolType poolType;
    [SerializeField] private float speed = 5f;

    private ThrowableWeaponInstance weaponInstance;
    private int thrownDurability;
    private bool isFlying = false;
    private Vector2 flyDirection;

    public void SetInstance(ThrowableWeaponInstance instance, int durability)
    {
        weaponInstance = instance;
        thrownDurability = durability;
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

        Land();
    }

    private void Land()
    {
        isFlying = false;

        GroundItemController groundController = FindFirstObjectByType<GroundItemController>();
        if (groundController != null && GridSystem.Instance != null)
        {
            Vector2Int cell = GridSystem.Instance.WorldToGrid(transform.position);
            var dummy = new ThrowableWeaponInstance { data = weaponInstance.weapon };

            ThrowableWeaponInstance capturedInstance = weaponInstance;
            int capturedDurability = thrownDurability;

            groundController.DropItem(dummy, 1, cell, () =>
            {
                capturedInstance.OnDaggerPickup(capturedDurability);
                return true;
            });
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        ObjectPool.Instance.Return(poolType, gameObject);
    }
}
