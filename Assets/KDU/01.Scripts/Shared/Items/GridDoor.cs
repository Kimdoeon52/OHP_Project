using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 격자 문. 1칸 통로(벽이 양옆으로 끊긴 입구)에만 배치된다.
/// 상태는 열림/닫힘 2가지. 닫히면 GridSystem.IsWalkable 이 false 가 되어 이동·시야를 막고,
/// 열리면 통과·시야 관통. 기본 동작은 플레이어 인접 시 자동 개폐(끄면 Open/Close 로 외부 제어).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GridDoor : MonoBehaviour
{
    public enum DoorState { Closed, Open }

    public Vector2Int GridPosition { get; private set; }
    public DoorState State { get; private set; } = DoorState.Closed;
    public bool IsOpen => State == DoorState.Open;

    [Header("스프라이트 — 옆면 문 (좌우 통로 = 세로 벽 사이)")]
    [SerializeField] private Sprite sideClosedSprite;
    [SerializeField] private Sprite sideOpenSprite;

    [Header("스프라이트 — 위/아래 문 (상하 통로 = 가로 벽 사이)")]
    [SerializeField] private Sprite topClosedSprite;
    [SerializeField] private Sprite topOpenSprite;

    [Header("동작")]
    [Tooltip("플레이어가 인접하면 자동으로 열리고, 멀어지면 닫힘.")]
    [SerializeField] private bool autoOpenWhenAdjacent = true;

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private bool _horizontalPassage; // true = 좌우 통로(옆면 문), false = 상하 통로(위/아래 문)

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

    /// <summary>맵 생성기가 호출 — 셀 위치·통로 방향을 지정하고 격자에 등록.</summary>
    public void Initialize(Vector2Int cell, bool horizontalPassage)
    {
        GridPosition = cell;
        _horizontalPassage = horizontalPassage;
        transform.position = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);

        if (GridSystem.Instance != null) GridSystem.Instance.RegisterDoor(cell, this);
        ApplyState();
    }

    private void Start()
    {
        _player = FindFirstObjectByType<GridPlayer>();
        UpdateVisibility();
    }

    private void OnDestroy()
    {
        if (GridSystem.Instance != null && GridSystem.Instance.GetDoorAt(GridPosition) == this)
            GridSystem.Instance.UnregisterDoor(GridPosition);
    }

    public void Open() => SetState(DoorState.Open);
    public void Close() => SetState(DoorState.Closed);

    public void SetState(DoorState state)
    {
        if (State == state) return;
        State = state;
        ApplyState();
        // 열리면 너머가 보이도록 즉시 시야 갱신.
        if (IsOpen && VisionSystem.Instance != null && _player != null)
            VisionSystem.Instance.UpdateVision(_player.GridPosition);
    }

    private void ApplyState()
    {
        if (_renderer == null) return;
        // 방향별로 닫힘/열림 스프라이트가 다름 (옆면 문과 위/아래 문은 닫힘·열림 모습이 서로 반대).
        Sprite s = _horizontalPassage
            ? (IsOpen ? sideOpenSprite : sideClosedSprite)
            : (IsOpen ? topOpenSprite : topClosedSprite);
        if (s != null) _renderer.sprite = s;
    }

    private void Update()
    {
        if (!autoOpenWhenAdjacent || _player == null || _player.IsDead) return;

        int dist = Mathf.Abs(_player.GridPosition.x - GridPosition.x)
                 + Mathf.Abs(_player.GridPosition.y - GridPosition.y);
        SetState(dist <= 1 ? DoorState.Open : DoorState.Closed);
    }

    private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible,
                                 IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_renderer == null || VisionSystem.Instance == null) return;
        // 한 번이라도 본 문은 계속 보임 (위치 기억).
        _renderer.enabled = VisionSystem.Instance.IsExplored(GridPosition);
    }
}
