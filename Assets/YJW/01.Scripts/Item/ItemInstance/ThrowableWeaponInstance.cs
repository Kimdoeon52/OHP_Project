using UnityEngine;

public class ThrowableWeaponInstance : EquipmentInstance, IThrowable
{
    public ThrowableWeapon weapon => data as ThrowableWeapon;

    public int GetMinDamage() => weapon.MinDamage + enhanceLevel * weapon.MinEnhanceBonus;
    public int GetMaxDamage() => weapon.MaxDamage + enhanceLevel * weapon.MaxEnhanceBonus;

    public int GetDamage() => Random.Range(GetMinDamage(), GetMaxDamage() + 1);

    public string GetTooltipStats() =>
        $"{weapon.Name}\n" +
        $"----------\n" +
        $"{weapon.Description}\n" +
        $"착용 힘: {weapon.CanUseStrength}\n" +
        $"데미지: {GetMinDamage()}~{GetMaxDamage()}";

    public void Throw(Vector2Int targetPos)
    {
        if (PlayerStats.Instance.Strength < weapon.CanUseStrength)
        {
            Debug.Log("힘이 부족합니다!");
            return;
        }

        Vector2Int currentPos = GridSystem.Instance.WorldToGrid(GameManager.Instance.player.transform.position);
        Vector2Int? targetTile = FindTarget(currentPos, targetPos);

        GameObject projectile = ObjectPool.Instance.Get(weapon.PoolType);
        projectile.transform.position = GameManager.Instance.player.transform.position;

        WeaponProjectile wp = projectile.GetComponent<WeaponProjectile>();
        wp.SetInstance(this);

        if (targetTile.HasValue)
            wp.FlyEnemy(targetPos);
        else
            wp.FlyNoEnemy(targetPos);
    }

    private Vector2Int? FindTarget(Vector2Int origin, Vector2Int direction)
    {
        Vector2Int perp = new Vector2Int(-direction.y, direction.x);

        Vector2Int[] candidates = new Vector2Int[]
        {
            origin + direction,
            origin + direction + perp,
            origin + direction - perp,
        };

        foreach (var tile in candidates)
        {
            if (HasEnemy(tile))
                return tile;
        }

        return null;
    }

    private bool HasEnemy(Vector2Int tile)
    {
        // TODO: 적 감지 로직
        return false;
    }
}
