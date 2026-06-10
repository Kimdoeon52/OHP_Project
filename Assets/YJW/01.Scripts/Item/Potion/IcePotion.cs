using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "IcePotion", menuName = "Item/Potion/ThrowablePotion/Ice")]
public class IcePotion : ThrowablePotion
{
	[Header("얼음 장판 판정")]
	[Tooltip("얼음 포션이 착탄한 위치를 중심으로 몇 칸까지 얼음 장판 판정을 만들지 정합니다. 1이면 3x3 범위입니다.")]
	[SerializeField, Min(0)] private int iceTerrainRadius = 1;

	[Tooltip("얼음 장판 판정이 몇 번의 플레이어 행동 동안 유지될지 정합니다.")]
	[SerializeField, Min(1)] private int iceTerrainDurationTurns = 1;

	public override void Throw(Vector2Int targetPos)
	{
		Vector3 playerPos = GameManager.Instance.player.transform.position;
		GameObject projectile = ObjectPool.Instance.Get(PoolType.IcePotion);
		projectile.transform.position = playerPos;

		// 착지했을 때 base.Throw 호출하도록 투사체에 전달
		projectile.GetComponent<Projectile>().Launch(targetPos, () =>
		{
			base.Throw(targetPos);                                       // 3x3 효과 적용
			RequestIceTerrain(targetPos);
			ObjectPool.Instance.Return(PoolType.IcePotion, projectile); // 착지 후 반납
		});
	}

	private void RequestIceTerrain(Vector2Int origin)
	{
		// 얼음 이펙트만 띄우면 화면에는 얼음이 보여도 상태이상 판정은 생기지 않습니다.
		// FirePotion과 같은 장판 시스템에 등록해서 플레이어/몬스터/보스가 Chill을 받게 합니다.
		GameEvents.OnTrapTerrainRequested?.Invoke(new TrapTerrainRequest(
			origin,
			iceTerrainRadius,
			iceTerrainDurationTurns,
			TrapTerrainType.Chill,
			null));
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
