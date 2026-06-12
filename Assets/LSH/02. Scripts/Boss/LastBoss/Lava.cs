using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// 주먹 내리찍기 패턴 후 생성되는 단일 용암 지대 오브젝트의 로직을 처리하는 클래스입니다.
/// </summary>
public class Lava : MonoBehaviour
{
    private List<Vector2Int> affectedCells = new List<Vector2Int>();
    private int damage = 5;
    private float duration = 2f;
    private float tickInterval = 0.5f;

    [SerializeField] private float fadeOutDuration = 1.0f; // LavaFin 애니메이션 재생 대기 시간

    // 용암 지대의 타격 범위 및 데미지, 지속 시간을 초기화.
    public void Init(List<Vector2Int> cells, int dmg, float dur)
    {
        affectedCells = new List<Vector2Int>(cells);
        damage = dmg;
        duration = dur;

        // 타일들의 중앙 좌표를 계산해 월드 위치로 설정합니다.
        if (GridSystem.Instance != null && cells.Count > 0)
        {
            Vector2 sum = Vector2.zero;
            foreach (var cell in cells)
            {
                sum += cell;
            }
            Vector2 averageGridPos = sum / cells.Count;
            Vector2Int centerCell = new Vector2Int(Mathf.RoundToInt(averageGridPos.x), Mathf.RoundToInt(averageGridPos.y));
            transform.position = GridSystem.Instance.GridToWorld(centerCell);
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
            if (player != null && !player.IsDead && affectedCells.Contains(player.GridPosition))
            {
                player.TakeDamage(damage);
            }
            
            await UniTask.Delay(intervalMs, cancellationToken: token);
            elapsed += tickInterval;
        }

        // 2초 뒤 애니메이션 트리거 실행
        var anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("LavaFin");
            int fadeOutMs = Mathf.RoundToInt(fadeOutDuration * 1000);
            if (fadeOutMs > 0)
            {
                await UniTask.Delay(fadeOutMs, cancellationToken: token);
            }
        }
        
        Destroy(gameObject);
    }
}
