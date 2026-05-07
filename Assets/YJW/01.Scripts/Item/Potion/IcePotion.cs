using UnityEngine;

[CreateAssetMenu(fileName = "IcePotion", menuName = "Item/Potion/Ice")]
public class IcePotion : ThrowablePotion
{
    public override void Throw(Vector2Int targetPos)
    {
        base.Throw(targetPos);
        // 얼음 효과 로직 추가
    }
}
