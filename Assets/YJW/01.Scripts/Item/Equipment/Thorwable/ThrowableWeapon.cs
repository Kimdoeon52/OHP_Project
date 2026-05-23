using UnityEngine;

[CreateAssetMenu(fileName = "ThrowableWeapon", menuName = "Item/Equipment/Throwable/ThrowableWeapon")]
public class ThrowableWeapon : ItemBase, IThrowable
{
    [SerializeField] private int minDamage;
    [SerializeField] private int maxDamage;
    [SerializeField] protected int CanUseStrength;
    [SerializeField] private PoolType poolType;
    [SerializeField] float attackSpeed;
    [SerializeField] private int durability;
    [SerializeField] public int grade;
    [SerializeField] public bool isAnalyzed;

    public void Throw(Vector2Int targetPos)
    {
        if (PlayerStats.Instance.Strength < CanUseStrength)
        {
            Debug.Log("힘이 부족합니다!");
            return;
        }

        Vector2Int currentPos = GridSystem.Instance.WorldToGrid(GameManager.Instance.player.transform.position);
        Vector2Int? targetTile = FindTarget(currentPos, targetPos);


        GameObject projectile = ObjectPool.Instance.Get(poolType);
		projectile.transform.position = GameManager.Instance.player.transform.position;


        WeaponProjectile weapon = projectile.GetComponent<WeaponProjectile>();

        if (targetTile.HasValue)
        {
            weapon.FlyEnemy(targetPos);
		}
        else
        {
            weapon.FlyNoEnemy(targetPos);
        }
    }


    private Vector2Int? FindTarget(Vector2Int origin, Vector2Int direction)
    {
        // 방향에 따른 수직 벡터 계산
        // (1,0) → 수직은 (0,1) / (0,1) → 수직은 (1,0)
        Vector2Int perp = new Vector2Int(-direction.y, direction.x);

        Vector2Int[] candidates = new Vector2Int[]
        {
            origin + direction,              // 가운데 (우선순위 1)
            origin + direction + perp,       // 위 (우선순위 2)
            origin + direction - perp,       // 아래 (우선순위 3)
        };

        foreach (var tile in candidates)
        {
            if (HasEnemy(tile))
                return tile;
        }

        return null; // 적 없음
    }

    private bool HasEnemy(Vector2Int tile)
    {
        // 적 감지 로직
        return false;
    }

    public int GetDamage()
    {
        return Random.Range(minDamage, maxDamage + 1);
    }
}
