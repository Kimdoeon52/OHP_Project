using System.Collections.Generic;
using UnityEngine;

public class ThrowableWeaponInstance : EquipmentInstance, IThrowable
{
    public ThrowableWeapon weapon => data as ThrowableWeapon;
    public List<int> currentDurabilities = new();

    public int GetMinDamage() => weapon.MinDamage + enhanceLevel * weapon.MinEnhanceBonus;
    public int GetMaxDamage() => weapon.MaxDamage + enhanceLevel * weapon.MaxEnhanceBonus;

    public int GetDamage() => Random.Range(GetMinDamage(), GetMaxDamage() + 1);

    public string GetTooltipStats()
    {
        string duraStr = string.Join(", ", currentDurabilities);
        return $"{weapon.Name}\n" +
               $"----------\n" +
               $"{weapon.Description}\n" +
               $"착용 힘: {weapon.CanUseStrength}\n" +
               $"데미지: {GetMinDamage()}~{GetMaxDamage()}\n" +
               $"내구도: ({duraStr})";
    }

    public void Throw(Vector2Int _)
    {
        if (currentDurabilities.Count == 0)
        {
            Debug.Log("던질 무기가 없습니다!");
            return;
        }
        if (PlayerStats.Instance.Strength < weapon.CanUseStrength)
        {
            Debug.Log("힘이 부족합니다!");
            return;
        }

        int lastIndex = currentDurabilities.Count - 1;
        int thrownDurability = currentDurabilities[lastIndex];
        currentDurabilities.RemoveAt(lastIndex);

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector3 playerPos = GameManager.Instance.player.transform.position;
        Vector2 direction = ((Vector2)(mouseWorldPos - playerPos)).normalized;

        GameObject projectile = ObjectPool.Instance.Get(weapon.PoolType);
        projectile.transform.position = playerPos;

        WeaponProjectile wp = projectile.GetComponent<WeaponProjectile>();
        wp.SetInstance(this, thrownDurability);
        wp.Fly(direction);
    }

    // WeaponProjectile이 플레이어에게 픽업될 때 호출
    public void OnDaggerPickup(int durability)
    {
        int remaining = durability - 1;
        if (remaining > 0)
            currentDurabilities.Add(remaining);
    }
}
