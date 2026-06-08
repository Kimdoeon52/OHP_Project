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

    private void UpdateVisibility()
    {
        if (_renderer == null || VisionSystem.Instance == null) 
            return;

        // 한 번이라도 봤으면 계속 보임
        _renderer.enabled = VisionSystem.Instance.IsExplored(GridPosition);
    }
}
