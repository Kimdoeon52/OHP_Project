using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 격자 문. 1칸 통로(벽이 양옆으로 끊긴 입구)에만 배치된다.
/// 상태는 열림/닫힘 2가지. 닫히면 GridSystem.IsWalkable 이 false 가 되어 이동·시야를 막고,
/// 열리면 통과·시야 관통.
/// Normal: 인접 시 자동 개폐.
/// Locked: 열쇠 보유 시 인접하면 1초 후 열림(GDD 9.15), 한 번 열리면 계속 열림.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GridDoor : MonoBehaviour
{
    public enum DoorState { Closed, Opening, Open, Hidden }
    public enum DoorKind { Normal, Locked, Secret }

    public Vector2Int GridPosition { get; private set; }
    public DoorState State { get; private set; } = DoorState.Closed;
    public DoorKind Kind { get; private set; } = DoorKind.Normal;
    public bool IsOpen => State == DoorState.Open;

    [Header("스프라이트 — 옆면 문 (좌우 통로 = 세로 벽 사이)")]
    [SerializeField] private Sprite sideClosedSprite;
    [SerializeField] private Sprite sideOpenSprite;

    [Header("스프라이트 — 위/아래 문 (상하 통로 = 가로 벽 사이)")]
    [SerializeField] private Sprite topClosedSprite;
    [SerializeField] private Sprite topOpenSprite;

    [Header("스프라이트 — 잠긴 옆면 문 (좌우 통로, 비우면 일반 옆면 문으로 폴백)")]
    [SerializeField] private Sprite sideLockedClosedSprite;
    [SerializeField] private Sprite sideLockedOpenSprite;

    [Header("스프라이트 — 잠긴 위/아래 문 (상하 통로, 비우면 일반 위/아래 문으로 폴백)")]
    [SerializeField] private Sprite topLockedClosedSprite;
    [SerializeField] private Sprite topLockedOpenSprite;

    [Header("동작")]
    [Tooltip("Normal 문: 플레이어가 인접하면 자동으로 열리고, 멀어지면 닫힘.")]
    [SerializeField] private bool autoOpenWhenAdjacent = true;

    [Tooltip("Locked 문 개방 시간(초). GDD 9.15: 1초.")]
    [SerializeField] private float lockedOpenSeconds = 1f;

    [Tooltip("Secret 문 발견 시간(초). 인접한 채로 이만큼 머무르면 발견(GDD 8.6 — '조심스러운 발걸음' 특성 hook 추후).")]
    [SerializeField] private float secretDiscoverySeconds = 3f;

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private bool _horizontalPassage;
    private float _openingProgress;
    private float _secretAdjacentTime;

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

    /// <summary>맵 생성기가 호출 — 셀 위치·통로 방향·문 종류를 지정하고 격자에 등록.</summary>
    public void Initialize(Vector2Int cell, bool horizontalPassage, DoorKind kind = DoorKind.Normal)
    {
        GridPosition = cell;
        _horizontalPassage = horizontalPassage;
        Kind = kind;
        if (kind == DoorKind.Secret) State = DoorState.Hidden;
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
        if (state != DoorState.Opening) _openingProgress = 0f;
        ApplyState();
        // 열리면 너머가 보이도록 즉시 시야 갱신.
        if (IsOpen && VisionSystem.Instance != null && _player != null)
            VisionSystem.Instance.UpdateVision(_player.GridPosition);
    }

    private void ApplyState()
    {
        if (_renderer == null) return;
        // 방향별로 닫힘/열림 스프라이트가 다름 (옆면 문과 위/아래 문은 닫힘·열림 모습이 서로 반대).
        // Opening 중에는 닫힘 스프라이트 유지(진행바 등 별도 표시는 후속 작업).
        // Locked 문은 잠긴 문 전용 슬롯(있으면) 우선, 없으면 일반 슬롯으로 폴백.
        bool showOpen = State == DoorState.Open;
        Sprite normal = _horizontalPassage
            ? (showOpen ? sideOpenSprite : sideClosedSprite)
            : (showOpen ? topOpenSprite : topClosedSprite);

        Sprite s = normal;
        if (Kind == DoorKind.Locked)
        {
            Sprite locked = _horizontalPassage
                ? (showOpen ? sideLockedOpenSprite : sideLockedClosedSprite)
                : (showOpen ? topLockedOpenSprite : topLockedClosedSprite);
            if (locked != null) s = locked;
        }
        if (s != null) _renderer.sprite = s;
    }

    private void Update()
    {
        if (_player == null || _player.IsDead) return;

        int dist = Mathf.Abs(_player.GridPosition.x - GridPosition.x)
                 + Mathf.Abs(_player.GridPosition.y - GridPosition.y);
        bool adjacent = dist <= 1;

        switch (Kind)
        {
            case DoorKind.Normal:
                if (!autoOpenWhenAdjacent) return;
                SetState(adjacent ? DoorState.Open : DoorState.Closed);
                break;

            case DoorKind.Locked:
                UpdateLocked(adjacent);
                break;

            case DoorKind.Secret:
                UpdateSecret(adjacent);
                break;
        }
    }

    private void UpdateSecret(bool adjacent)
    {
        if (State != DoorState.Hidden) return; // 발견 후엔 그대로 열림 유지

        if (!adjacent) { _secretAdjacentTime = 0f; return; }

        _secretAdjacentTime += Time.deltaTime;
        if (_secretAdjacentTime >= secretDiscoverySeconds) Discover();
    }

    /// <summary>Secret 문 강제 발견 — 디버그/특성 트리거용. 봉인 wall 제거 후 열림 상태로 전환.</summary>
    public void Discover()
    {
        if (Kind != DoorKind.Secret || State != DoorState.Hidden) return;
        if (GridSystem.Instance != null
            && GridSystem.Instance.Tilemaps != null
            && GridSystem.Instance.Tilemaps.wall != null)
        {
            GridSystem.Instance.Tilemaps.wall.SetTile(new Vector3Int(GridPosition.x, GridPosition.y, 0), null);
        }
        SetState(DoorState.Open);
    }

    private void UpdateLocked(bool adjacent)
    {
        if (State == DoorState.Open) return; // 이미 열림 — 계속 열림 유지(닫히지 않음).

        if (!adjacent)
        {
            if (State == DoorState.Opening) SetState(DoorState.Closed);
            return;
        }

        var inv = KeyInventory.Instance;
        bool hasKey = inv != null && inv.Has(KeyType.SpecialRoom);
        if (!hasKey)
        {
            if (State == DoorState.Opening) SetState(DoorState.Closed);
            return;
        }

        if (State != DoorState.Opening) SetState(DoorState.Opening);

        _openingProgress += Time.deltaTime;
        if (_openingProgress >= lockedOpenSeconds)
        {
            if (inv.TryConsume(KeyType.SpecialRoom)) SetState(DoorState.Open);
            else SetState(DoorState.Closed);
        }
    }

    private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible,
                                 IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_renderer == null) return;
        // Secret 문 발견 전엔 시야와 무관하게 숨김 — 벽 타일이 대신 보임.
        if (State == DoorState.Hidden) { _renderer.enabled = false; return; }
        if (VisionSystem.Instance == null) return;
        // 한 번이라도 본 문은 계속 보임 (위치 기억).
        _renderer.enabled = VisionSystem.Instance.IsExplored(GridPosition);
    }
}
