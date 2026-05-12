using Cysharp.Threading.Tasks;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

[CreateAssetMenu(fileName = "ThrowableWeapon", menuName = "Item/Equipment/Throwable/ThrowableWeapon")]
public class ThrowableWeapon : ItemBase, IThrowable
{
    public int damage { get; private set; }
    protected int CanUseStrength;

    public void Throw(Vector2Int targetPos)
    {
        if (PlayerStats.Instance.Strength < CanUseStrength)
        {
            Debug.Log("힘이 부족합니다!");
            return;
        }

        Vector2Int currentPos = GridSystem.Instance.WorldToGrid(GameManager.Instance.player.transform.position);
        Vector2Int? targetTile = FindTarget(currentPos, targetPos);

        if (targetTile.HasValue)
        {
            // 해당 타일로 무기 날리기
        }
        else
        {
            // 적 없으면 직선으로 날리기 등
        }
    }

    private Vector2Int? FindTarget(Vector2Int origin, Vector2Int direction)
    {
        // 방향에 따른 수직 벡터 계산
        // (1,0) → 수직은 (0,1) / (0,1) → 수직은 (1,0)
        Vector2Int perp = new Vector2Int(-direction.y, direction.x);

        Vector2Int[] candidates = new Vector2Int[]
        {
            origin + direction,              // 가운데 (우선순위 1)
            origin + direction + perp,       // 위 (우선순위 2)
            origin + direction - perp,       // 아래 (우선순위 3)
        };

        foreach (var tile in candidates)
        {
            if (HasEnemy(tile))
                return tile;
        }

        return null; // 적 없음
    }

    private bool HasEnemy(Vector2Int tile)
    {
        // 적 감지 로직
        return false;
    }
}
