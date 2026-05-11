using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "IcePotion", menuName = "Item/Potion/ThrowablePotion/Ice")]
public class IcePotion : ThrowablePotion
{
	public override void Throw(Vector2Int targetPos)
	{
		Vector3 playerPos = GameManager.Instance.player.transform.position;
		GameObject projectile = ObjectPool.Instance.Get(PoolType.IcePotion);
		projectile.transform.position = playerPos;

		// 착지했을 때 base.Throw 호출하도록 투사체에 전달
		projectile.GetComponent<Projectile>().Launch(targetPos, () =>
		{
			base.Throw(targetPos);                                       // 3x3 효과 적용
			ObjectPool.Instance.Return(PoolType.IcePotion, projectile); // 착지 후 반납
		});
	}
	protected override async UniTask ApplyEffect(Vector2Int tile)
	{
		// 이 타일에 얼음 생성
		GameObject iceEffect = ObjectPool.Instance.Get(PoolType.IceEffect);
		Vector3 worldPos = GridSystem.Instance.GridToWorld(tile);
		iceEffect.transform.position = worldPos;

		// 3초 뒤에 사라지기
		await UniTask.Delay(3000);
		ObjectPool.Instance.Return(PoolType.IceEffect, iceEffect);
	}
}
