using UnityEngine;

public class ThrowablePotion : PotionData, IThrowable
{
    public virtual void Throw(Vector2Int targetPos) 
    {
        // 포션을 targetPos로 던지는 로직
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int tile = targetPos + new Vector2Int(x, y);
                ApplyEffect(tile);
            }
        }
    }

    protected virtual void ApplyEffect(Vector2Int tile) { }

    public override void Use()
    {
        // 던지는 포션은 Use()로 사용하지 않음
        Debug.LogWarning("던지는 포션은 Throw()를 사용하세요!");
    }
}
