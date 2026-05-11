using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "FirePotion", menuName = "Item/Potion/ThrowablePotion/Fire")]
public class FirePotion : ThrowablePotion
{
    public override void Throw(Vector2Int targetPos)
    {
        Vector3 playerPos = GameManager.Instance.player.transform.position;
        GameObject projectile = ObjectPool.Instance.Get(PoolType.FirePotion);
        projectile.transform.position = playerPos;

        // 착지했을 때 base.Throw 호출하도록 투사체에 전달
        projectile.GetComponent<Projectile>().Launch(targetPos, () =>
        {
            base.Throw(targetPos);                                       // 3x3 효과 적용
            ObjectPool.Instance.Return(PoolType.FirePotion, projectile); // 착지 후 반납
        });
    }
    protected override async UniTask ApplyEffect(Vector2Int tile)
    {
        // 이 타일에 불 붙이기
        GameObject fireEffect = ObjectPool.Instance.Get(PoolType.FireEffect);
        Vector3 worldPos = GridSystem.Instance.GridToWorld(tile);
        fireEffect.transform.position = worldPos;

        // 3초 뒤에 사라지기
        // 풀 타일이 있는 경우
        //await UniTask.Delay(3000);
        //ObjectPool.Instance.Return(PoolType.FireEffect, fireEffect);
        // 풀 타일이 탄 흔적으로 사라지기

        // 풀 타일이 아닌 경우
        await UniTask.Delay(1000);
		ObjectPool.Instance.Return(PoolType.FireEffect, fireEffect);
	}

}

