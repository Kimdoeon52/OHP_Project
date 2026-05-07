using UnityEngine;

[CreateAssetMenu(fileName = "FirePotion", menuName = "Item/Potion/Fire")]
public class FirePotion : ThrowablePotion
{
    public override void Throw(Vector2Int targetPos)
    {
        base.Throw(targetPos);

        // 불꽃 효과 로직 추가
    }
}
