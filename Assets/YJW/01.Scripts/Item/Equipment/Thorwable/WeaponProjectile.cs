using PrototypeRT;
using UnityEngine;

public class WeaponProjectile : MonoBehaviour
{
    [SerializeField] private PoolType poolType;
    [SerializeField] private float speed = 5f;

    private ThrowableWeaponInstance weaponInstance;
    private int thrownDurability;
    private bool returnToPool = true;
    private bool isFlying = false;
    private bool hasTarget = false;
    private Vector2 flyDirection;
    private Vector3 targetWorldPosition;

    public void SetInstance(ThrowableWeaponInstance instance, int durability)
    {
        weaponInstance = instance;
        thrownDurability = durability;
        returnToPool = true;
    }

    public void SetInstance(ThrowableWeaponInstance instance, int durability, PoolType sourcePoolType, bool pooled)
    {
        weaponInstance = instance;
        thrownDurability = durability;
        poolType = sourcePoolType;
        returnToPool = pooled;
    }

    public void Fly(Vector2 direction)
    {
        flyDirection = direction.normalized;
        hasTarget = false;
        isFlying = true;
    }

    public void FlyTo(Vector3 targetWorld)
    {
        targetWorld.z = transform.position.z;
        targetWorldPosition = targetWorld;
        flyDirection = ((Vector2)(targetWorldPosition - transform.position)).normalized;
        hasTarget = true;
        isFlying = true;
    }

    private void Update()
    {
        if (!isFlying) return;

        float moveDistance = speed * Time.deltaTime;
        if (hasTarget)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, moveDistance);
            if (Vector3.Distance(transform.position, targetWorldPosition) <= 0.001f)
                Land();
            return;
        }

        transform.position += (Vector3)(flyDirection * moveDistance);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (weaponInstance == null)
        {
            Land();
            Debug.LogWarning("WeaponProjectile에 WeaponInstance가 설정되지 않았습니다.");
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            int damage = weaponInstance.GetDamage();
            Debug.Log($"{enemy.name}한테 투척무기 맞힘");
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
        hasTarget = false;

        GroundItemController groundController = FindFirstObjectByType<GroundItemController>();
        if (groundController != null && GridSystem.Instance != null && weaponInstance != null && weaponInstance.weapon != null)
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
        if (returnToPool && ObjectPool.Instance != null)
        {
            ObjectPool.Instance.Return(poolType, gameObject);
            return;
        }

        Destroy(gameObject);
    }
}
