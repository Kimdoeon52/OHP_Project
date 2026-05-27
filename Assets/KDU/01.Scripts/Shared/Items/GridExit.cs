using UnityEngine;

/// <summary>
/// 출구 셀. 플레이어가 이 칸에 도달하면 OnLevelComplete 발행 + 타이머 정지.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GridExit : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }

    [Tooltip("Animator int 파라미터 이름 — 다음 층 영역(1/2/3)이 들어감. 비우면 세팅 안 함.")]
    [SerializeField] private string areaParamName = "Area";

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private bool _triggered;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        GameEvents.OnVisionUpdated += OnVisionUpdated;
    }

    private void OnDisable()
    {
        GameEvents.OnVisionUpdated -= OnVisionUpdated;
    }

    private void Start()
    {
        GridPosition = GridSystem.Instance.WorldToGrid(transform.position);
        transform.position = GridSystem.Instance.GridToWorld(GridPosition);
        _player = FindFirstObjectByType<GridPlayer>();

        ApplyAreaAnimation();
        UpdateVisibility();
    }

    private void ApplyAreaAnimation()
    {
        if (MapGenerator.Instance == null || string.IsNullOrEmpty(areaParamName)) return;
        var animator = GetComponentInChildren<Animator>();
        if (animator == null) return;
        // 포탈은 "다음 층" 으로 가는 문 — 다음 depth 의 영역으로 분기.
        int nextArea = MapGenerator.AreaFromDepth(MapGenerator.Instance.CurrentDepth + 1);
        animator.SetInteger(areaParamName, nextArea);
    }

    private void Update()
    {
        if (_triggered || _player == null || _player.IsDead) return;

        if (_player.GridPosition == GridPosition)
        {
            _triggered = true;
            GameEvents.OnLevelComplete?.Invoke();
            TimeSystem.Instance.StopTimer();
        }
    }

    private void OnVisionUpdated(System.Collections.Generic.IReadOnlyCollection<Vector2Int> visible,
                                  System.Collections.Generic.IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_renderer == null || VisionSystem.Instance == null) return;
        // 한 번이라도 봤으면 계속 보임 (출구는 위치 기억해두는 게 자연스러움)
        _renderer.enabled = VisionSystem.Instance.IsExplored(GridPosition);
    }
}
