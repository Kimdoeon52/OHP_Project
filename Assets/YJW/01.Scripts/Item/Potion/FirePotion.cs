using UnityEngine;

[CreateAssetMenu(fileName = "FirePotion", menuName = "Item/Potion/Fire")]
public class FirePotion : ThrowablePotion
{
    [SerializeField] private GameObject projectilePrefab;

    public override void Throw(Vector2Int targetPos)
    {
        Vector3 playerPos = GameManager.Instance.player.transform.position;
        GameObject projectile = Object.Instantiate(projectilePrefab, playerPos, Quaternion.identity);

        // 착지했을 때 base.Throw 호출하도록 투사체에 전달
        projectile.GetComponent<Projectile>().Launch(targetPos, () => base.Throw(targetPos));
    }
    protected override void ApplyEffect(Vector2Int tile)
    {
        // 이 타일에 불 붙이기
    }
}
