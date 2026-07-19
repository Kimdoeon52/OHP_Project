using Cysharp.Threading.Tasks;
using UnityEngine;

public class ThrowablePotion : PotionData, IThrowable
{
    public virtual bool Throw(Vector2Int targetPos)
    {
        // 포션을 targetPos로 던지는 로직
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int tile = targetPos + new Vector2Int(x, y);

                // 벽이나 막힌 칸에는 불/얼음 이펙트가 붙지 않도록 막는다.
                if (GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(tile))
                    continue;

                ApplyEffect(tile);
            }
        }

        return true;
    }

	protected virtual UniTask ApplyEffect(Vector2Int tile)
	{
		return UniTask.CompletedTask; // UniTask를 직접 반환
	}

	public override void Use()
    {
        // 던지는 포션은 Use()로 사용하지 않음
        Debug.LogWarning("던지는 포션은 Throw()를 사용하세요!");
    }
}
