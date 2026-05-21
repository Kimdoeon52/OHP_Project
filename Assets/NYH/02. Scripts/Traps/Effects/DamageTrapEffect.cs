using UnityEngine;

public class DamageTrapEffect : MonoBehaviour, ITrapEffect
{
    [Tooltip("범위공격 사용 여부")]
    [SerializeField] private bool affectArea;
    [Tooltip("효과 범위 지정 " +
        " -1 = TrapData.Radius 사용" +
        " 0 = 중심칸 만" +
        " 1이상 = 그 값만큼 칸 범위")]
    [SerializeField] private int radiusOverride = -1;


    public void Execute(TrapContext context)
    {
        // 범위 피해 반경을 결정한다. radiusOverride가 0 이상이면 이 컴포넌트 값을 우선 사용하고,
        // -1이면 TrapData에 설정된 radius 값을 그대로 사용한다.
        int radius = radiusOverride >= 0 ? radiusOverride : context.Data.radius;
         // 범위 피해가 꺼져 있거나 반경이 0 이하라면, 트랩을 밟은 대상에게만 피해를 준다.
        if (!affectArea || radius <= 0)
        {
            DealDamage(context.Activator, context);
            return;
        }

        // 범위 피해가 켜져 있으면 트랩 위치 기준 반경 안의 모든 격자를 검사하고,
        // 각 칸에 있는 피해 가능 대상에게 데미지를 적용한다.
        foreach (Vector2Int cell in TrapEffectUtility.CellsInRadius(context.Position, radius))
            DealDamage(TrapEffectUtility.GetEntityAt(cell, context.Activator), context);
    }

    private static void DealDamage(IGridEntity entity, TrapContext context)
    {
        // 실제 데미지 계산과 적용은 공용 유틸에 맡긴다.
        // DamageTrapEffect는 "누구에게 피해를 줄지"만 결정한다.
        TrapEffectUtility.DealDamage(entity, context.Data);
    }
}
