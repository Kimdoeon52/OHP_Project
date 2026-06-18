using System.Collections.Generic;
using UnityEngine;

public class ThrowableWeaponInstance : EquipmentInstance, IThrowable
{
    public ThrowableWeapon weapon => data as ThrowableWeapon;
    public List<int> currentDurabilities = new();

    // 저격의 반지가 장착된 동안 모든 투척 무기에 더해지는 유효 강화 보너스
    public static int snipeBonus = 0;

    public int GetMinDamage() => weapon.MinDamage + (enhanceLevel + snipeBonus) * weapon.MinEnhanceBonus;
    public int GetMaxDamage() => weapon.MaxDamage + (enhanceLevel + snipeBonus) * weapon.MaxEnhanceBonus;

    public int GetDamage() => Random.Range(GetMinDamage(), GetMaxDamage() + 1);

    public override string GetToolTipStats()
    {
        int count = currentDurabilities.Count;
        string enhanceStr = enhanceLevel > 0 ? $" <color=yellow>+{enhanceLevel}</color>" : "";
        string countStr = count > 0 ? $" ×{count}" : "";
        string duraStr = count > 0 ? string.Join(" / ", currentDurabilities) : "-";

        return $"{weapon.Name}{enhanceStr}{countStr}\n" +
               $"─────────────\n" +
               $"{weapon.Description}\n" +
               $"\n" +
               $"데미지   {GetMinDamage()}~{GetMaxDamage()}\n" +
               $"힘 요구   {weapon.CanUseStrength}\n" +
               $"내구도   {duraStr}";
    }

    public void Throw(Vector2Int targetCell)
    {
        if (currentDurabilities.Count == 0)
        {
            Debug.Log("던질 무기가 없습니다!");
            return;
        }
        if (GetPlayerStrength() < weapon.CanUseStrength)
        {
            Debug.Log("힘이 부족합니다!");
            return;
        }

        int lastIndex = currentDurabilities.Count - 1;
        int thrownDurability = currentDurabilities[lastIndex];
        currentDurabilities.RemoveAt(lastIndex);

        Vector3 playerPos = GetPlayerWorldPosition();
        Vector3 targetWorldPos = GetTargetWorldPosition(targetCell);
        GameObject projectile = ObjectPool.Instance != null ? ObjectPool.Instance.Get(weapon.PoolType) : null;
        bool pooledProjectile = projectile != null;
        if (projectile == null)
            projectile = CreateFallbackProjectile();

        if (projectile == null)
        {
            currentDurabilities.Add(thrownDurability);
            Debug.LogWarning($"ThrowableWeaponInstance: {weapon.PoolType} 투사체 프리팹/풀을 찾지 못해 투척을 취소합니다.");
            return;
        }

        projectile.transform.position = playerPos;

        WeaponProjectile wp = projectile.GetComponent<WeaponProjectile>();
        if (wp == null)
            wp = projectile.AddComponent<WeaponProjectile>();

        wp.SetInstance(this, thrownDurability, weapon.PoolType, pooledProjectile);
        wp.FlyTo(targetWorldPos);
        GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.DaggerThrow);
    }

    // WeaponProjectile이 플레이어에게 픽업될 때 호출. 내구도가 남아 복구됐으면 true.
    public bool OnDaggerPickup(int durability)
    {
        int remaining = durability - 1;
        if (remaining <= 0)
            return false;

        currentDurabilities.Add(remaining);
        return true;
    }

    private static int GetPlayerStrength()
    {
        if (NYHPlayerStats.Instance != null)
            return NYHPlayerStats.Instance.Strength;

        return PlayerStats.Instance != null ? PlayerStats.Instance.Strength : 0;
    }

    private static Vector3 GetPlayerWorldPosition()
    {
        if (GameManager.Instance != null && GameManager.Instance.player != null)
            return GameManager.Instance.player.transform.position;

        GridPlayer player = Object.FindFirstObjectByType<GridPlayer>();
        return player != null ? player.transform.position : Vector3.zero;
    }

    private static Vector3 GetTargetWorldPosition(Vector2Int targetCell)
    {
        if (GridSystem.Instance != null)
            return GridSystem.Instance.GridToWorld(targetCell);

        if (Camera.main == null)
            return Vector3.zero;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        return mouseWorldPos;
    }

    private GameObject CreateFallbackProjectile()
    {
        GameObject projectile = new GameObject($"{weapon.Name}_Projectile");

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = weapon.Icon;
        renderer.sortingOrder = 20;

        Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.radius = 0.18f;

        projectile.AddComponent<WeaponProjectile>();
        projectile.SetActive(true);
        return projectile;
    }
}
