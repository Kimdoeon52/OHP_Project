using UnityEngine;
using UnityEngine.UIElements;

public class WeaponProjectile : MonoBehaviour
{
    [SerializeField] private ThrowableWeapon weapon;
    [SerializeField] private PoolType poolType;

	[SerializeField] private float speed = 5f;

	private bool isFlying = false;
	private bool hasTarget = false;
	private Vector2 flyDirection;
	private Vector3 targetWorldPos;

	public void FlyNoEnemy(Vector2 direction)
    {
		flyDirection = direction.normalized;
		hasTarget = false;
		isFlying = true;
	}

    public void FlyEnemy(Vector2 targetWorldPos)
    {
		this.targetWorldPos = targetWorldPos;
		hasTarget = true;
		isFlying = true;
	}

	private void Update()
	{
		if (!isFlying) return;

		if (hasTarget)
		{
			transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, speed * Time.deltaTime);

			if (Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
			{
				isFlying = false;
				ReturnToPool();
			}
		}
		else
		{
			transform.position += (Vector3)(flyDirection * speed * Time.deltaTime);
		}
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
                collision.gameObject.GetComponent<EnemyBase>().TakeDamage(weapon.GetDamage());
            }
        }

		isFlying = false;
		ReturnToPool();
	}

	private void ReturnToPool()
	{
		ObjectPool.Instance.Return(poolType, gameObject);
	}
}
