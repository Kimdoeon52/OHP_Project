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

    public void Throw(Vector2Int _)
    {
        if (PlayerStats.Instance.Strength < weapon.CanUseStrength)
        {
            Debug.Log("힘이 부족합니다!");
            return;
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector3 playerPos = GameManager.Instance.player.transform.position;
        Vector2 direction = ((Vector2)(mouseWorldPos - playerPos)).normalized;

        GameObject projectile = ObjectPool.Instance.Get(weapon.PoolType);
        projectile.transform.position = playerPos;

        WeaponProjectile wp = projectile.GetComponent<WeaponProjectile>();
        wp.SetInstance(this);
        wp.Fly(direction);
    }
}
