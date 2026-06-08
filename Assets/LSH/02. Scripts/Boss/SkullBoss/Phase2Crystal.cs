using UnityEngine;
using Cysharp.Threading.Tasks;

// 플레이어의 길을 막고 10초(또는 지정된 수명) 뒤에 다시 걸을 수 있게 만들며 사라지는 크리스탈
public class Phase2Crystal : MonoBehaviour
{
    [SerializeField] private float defaultLifetimeSec = 10f; // 기본 수명 10초

    private SpriteRenderer sr;
    private float lifetimeSec;
    private Vector2Int gridPos;
    private bool hasGridPos = false;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        lifetimeSec = defaultLifetimeSec;
    }

    public void Setup(Color tint, float scaleMultiplier, float lifetime)
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = tint;
        if (scaleMultiplier > 0f) transform.localScale *= scaleMultiplier;
        if (lifetime > 0f) lifetimeSec = lifetime;
    }

    public void SetupWithGridBlock(Color tint, float scaleMultiplier, float lifetime, Vector2Int pos)
    {
        Setup(tint, scaleMultiplier, lifetime);

        gridPos = pos;
        hasGridPos = true;

        if (GridSystem.Instance != null)
        {
            GridSystem.Instance.BlockCell(gridPos);
        }
    }

    private void Start()
    {
        RunLifetimeAsync().Forget();
    }

    private async UniTask RunLifetimeAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();
        try
        {
            await UniTask.Delay((int)(lifetimeSec * 1000), cancellationToken: token);
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            ReleaseGridBlock();
            Destroy(gameObject);
        }
    }

    private void ReleaseGridBlock()
    {
        if (hasGridPos && GridSystem.Instance != null)
        {
            GridSystem.Instance.UnblockCell(gridPos);
            hasGridPos = false;
        }
    }

    private void OnDestroy()
    {
        ReleaseGridBlock();
    }
}
