using UnityEngine;


// 출구 셀
[RequireComponent(typeof(SpriteRenderer))]
public class GridExit : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }

    [SerializeField] private string areaParamName = "Area";

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private bool _triggered;
    private bool _revealed;   // 지도 스크롤로 강제 공개 — 탐험 여부와 무관하게 계속 표시.

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
        if (MapGenerator.Instance == null || string.IsNullOrEmpty(areaParamName)) 
            return;

        var animator = GetComponentInChildren<Animator>();

        if (animator == null)
            return;

        int nextArea = MapGenerator.AreaFromDepth(MapGenerator.Instance.CurrentDepth + 1);
        animator.SetInteger(areaParamName, nextArea);
    }

    private void Update()
    {
        if (_triggered || _player == null || _player.IsDead)
            return;

        if (_player.GridPosition == GridPosition)
        {
            _triggered = true;
            GameEvents.OnLevelComplete?.Invoke();
            TimeSystem.Instance.StopTimer();
        }
    }

    private void OnVisionUpdated(System.Collections.Generic.IReadOnlyCollection<Vector2Int> visible, System.Collections.Generic.IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    /// <summary>지도 스크롤 등으로 출구 위치만 강제 공개 — 탐험 게이팅을 무시하고 계속 표시.</summary>
    public void Reveal()
    {
        _revealed = true;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_renderer == null || VisionSystem.Instance == null)
            return;

        // 한 번이라도 봤거나(탐험) 지도 스크롤로 공개됐으면 계속 보임.
        _renderer.enabled = _revealed || VisionSystem.Instance.IsExplored(GridPosition);
    }
}
