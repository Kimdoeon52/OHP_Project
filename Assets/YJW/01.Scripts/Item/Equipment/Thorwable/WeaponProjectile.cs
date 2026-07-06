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

        if (TryGetDamageableTarget(other, out IDamageable damageable))
        {
            int damage = weaponInstance.GetDamage();
            Debug.Log($"{other.name}한테 투척무기 맞힘");

            EnemyBase enemy = damageable as EnemyBase;
            int beforeHp = damageable.CurrentHp;
            if (weaponInstance.weapon is IHitEffect hitEffect && enemy != null)
                hitEffect.ApplyEffect(enemy, damage);
            else
                damageable.TakeDamage(damage);

            int finalDamage = Mathf.Max(0, beforeHp - damageable.CurrentHp);
            if (finalDamage > 0 && damageable is not BossBase)
                PublishDamageText(damageable, finalDamage);
        }

        Land();
    }

    private static bool TryGetDamageableTarget(Collider2D hitCollider, out IDamageable damageable)
    {
        damageable = null;

        if (hitCollider == null)
            return false;

        // 몬스터 콜라이더가 자식 오브젝트에 붙어 있으면 GetComponent만으로는 IDamageable을 찾지 못한다.
        damageable = hitCollider.GetComponent<IDamageable>();
        if (damageable == null)
            damageable = hitCollider.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return false;

        // 투척무기는 적/보스 피격만 처리한다. 플레이어 등 다른 IDamageable과 충돌해 오작동하지 않게 막는다.
        return damageable is EnemyBase || damageable is BossBase;
    }

    private static void PublishDamageText(IDamageable damageable, int damage)
    {
        if (damageable == null || damage <= 0)
            return;

        // BossBase는 내부에서 이미 데미지 텍스트를 띄우므로 일반 대상만 여기서 보강한다.
        Vector3 worldPosition = damageable is Component component ? component.transform.position : Vector3.zero;
        GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(worldPosition, damage.ToString(), damage, CombatTextType.Damage));
    }

    private void Land()
    {
        isFlying = false;
        hasTarget = false;

        // 내구도 1짜리는 던지면 그대로 깨지므로 바닥에 줍기용으로 남기지 않음
        GroundItemController groundController = thrownDurability > 1 ? FindFirstObjectByType<GroundItemController>() : null;
        if (groundController != null && GridSystem.Instance != null && weaponInstance != null && weaponInstance.weapon != null)
        {
            Vector2Int cell = GridSystem.Instance.WorldToGrid(transform.position);
            var dummy = new ThrowableWeaponInstance { data = weaponInstance.weapon };

            ThrowableWeaponInstance capturedInstance = weaponInstance;
            int capturedDurability = thrownDurability;

            groundController.DropItem(dummy, 1, cell, () =>
            {
                if (capturedInstance.OnDaggerPickup(capturedDurability))
                    RestoreToInventoryIfDetached(capturedInstance);

                return true;
            });
        }

        ReturnToPool();
    }

    // 다 던진 뒤 슬롯이 사라진 상태로 마지막 한 자루를 주운 경우, 인벤토리에 새 슬롯을 만들어 돌려줌
    private static void RestoreToInventoryIfDetached(ThrowableWeaponInstance instance)
    {
        InventoryController inventory = FindFirstObjectByType<InventoryController>();
        if (inventory == null)
            return;

        foreach (InventoryItem item in inventory.Items)
            if (item.ItemInstance == instance)
                return;

        inventory.TryAddItem(instance);
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
