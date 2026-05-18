using UnityEngine;

/// <summary>
/// 팬텀: 기본 추적 + 사망 시 분열.
/// TryStepTowardPlayer/TryStep은 이제 EnemyBase에서 상속.
/// 템플릿 메서드 기본 흐름 그대로 사용.
/// </summary>
public class GridEnemyPhantom : EnemyBase
{
    [Header("분열 설정")]
    public GameObject splitPrefab; // 생성될 프리팹
    public int splitCount = 2; // 생성될 개수
    public bool canSplit = true; // 분열 가능 여부(무한 분열 방지용)

    // OnTurnUpdate는 EnemyBase 기본 템플릿 메서드 사용 (override 불필요)

    protected override void Die()
    {
        if (canSplit && splitPrefab != null)
        {
            SpawnSplitPhantoms();
        }
        base.Die();
    }

    private void SpawnSplitPhantoms()
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        int spawnedCount = 0;

        foreach (var dir in dirs)
        {
            if (spawnedCount >= splitCount) break;
            Vector2Int nextPos = GridPosition + dir;

            if (GridSystem.Instance.IsWalkable(nextPos) && TurnManager.Instance.GetEntityAt(nextPos) == null && nextPos != Player.GridPosition)
            {
                GameObject inst = Instantiate(splitPrefab, GridSystem.Instance.GridToWorld(nextPos), Quaternion.identity);
                GridEnemyPhantom miniPhantom = inst.GetComponent<GridEnemyPhantom>();
                if (miniPhantom != null)
                {
                    miniPhantom.Initialize(nextPos);
                    miniPhantom.canSplit = false; // 분열된 애들은 분열 못하게함
                }
                spawnedCount++;
            }
        }
    }
}
