using UnityEngine;

[CreateAssetMenu(fileName = "IcePotion", menuName = "Item/Potion/Ice")]
public class IcePotion : ThrowablePotion
{
    protected override void ApplyEffect(Vector2Int tile)
    {
        // 이 타일에 얼음 효과 주기

    }
}
