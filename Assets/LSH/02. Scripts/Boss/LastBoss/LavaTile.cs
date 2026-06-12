using UnityEngine;
using Cysharp.Threading.Tasks;

// 주먹 내리찍기 패턴 후 생성되는 용암 타일

public class LavaTile : MonoBehaviour
{
    private Vector2Int gridPos;
    private int damage = 5;
    private float duration = 2f;
    private float tickInterval = 0.5f;
    public void Init(Vector2Int pos, int dmg, float dur)
    {
        gridPos = pos;
        damage = dmg;
        duration = dur;

        if (GridSystem.Instance != null)
        {
            transform.position = GridSystem.Instance.GridToWorld(gridPos);
        }

        LavaRoutineAsync().Forget();
    }

    private async UniTaskVoid LavaRoutineAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();
        float elapsed = 0f;
        int intervalMs = Mathf.RoundToInt(tickInterval * 1000);

        while (elapsed < duration)
        {
            var player = FindFirstObjectByType<GridPlayer>();
            if (player != null && !player.IsDead && player.GridPosition == gridPos)
            {
                player.TakeDamage(damage);
            }
            
            await UniTask.Delay(intervalMs, cancellationToken: token);
            elapsed += tickInterval;
        }
        
        Destroy(gameObject);
    }
}
