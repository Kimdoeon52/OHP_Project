using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "FirePotion", menuName = "Item/Potion/ThrowablePotion/Fire")]
public class FirePotion : ThrowablePotion
{
    [Header("불 장판 판정")]
    [Tooltip("불 포션이 착탄한 위치를 중심으로 몇 칸까지 불 장판 판정을 만들지 정합니다. 1이면 3x3 범위입니다.")]
    [SerializeField, Min(0)] private int fireTerrainRadius = 1;

    [Tooltip("불 장판 판정이 몇 번의 플레이어 행동 동안 유지될지 정합니다. 현재 불 이펙트 수명이 짧아서 기본값은 1턴입니다.")]
    [SerializeField, Min(1)] private int fireTerrainDurationTurns = 1;

    public override void Throw(Vector2Int targetPos)
    {
        GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.PotionThrow);
        Vector3 playerPos = GameManager.Instance.player.transform.position;
        GameObject projectile = ObjectPool.Instance.Get(PoolType.FirePotion);
        projectile.transform.position = playerPos;

        // 착지했을 때 base.Throw 호출하도록 투사체에 전달
        projectile.GetComponent<Projectile>().Launch(targetPos, () =>
        {
            GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.PotionImpactFire);
            base.Throw(targetPos);                                       // 3x3 효과 적용
            RequestFireTerrain(targetPos);                               // 기존 함정 지형 시스템에 불 장판 판정을 알려 화상 상태이상을 재사용
            ObjectPool.Instance.Return(PoolType.FirePotion, projectile); // 착지 후 반납
        });
    }

    private void RequestFireTerrain(Vector2Int origin)
    {
        // 불 이펙트만 띄우면 화면에는 불이 보여도 TrapTerrainController가 바닥 상태를 모릅니다.
        // 기존 Fire terrain -> Burn 상태이상 경로를 쓰기 위해 착탄 지점을 불 장판으로 등록합니다.
        // 장판 등록 후에는 생성 즉시, 밟고 들어올 때, 플레이어 행동 완료 틱마다 Burn 판정이 처리됩니다.
        GameEvents.OnTrapTerrainRequested?.Invoke(new TrapTerrainRequest(
            origin,
            fireTerrainRadius,
            fireTerrainDurationTurns,
            TrapTerrainType.Fire,
            null));
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

