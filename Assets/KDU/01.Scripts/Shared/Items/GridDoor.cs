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
    public enum DoorKind { Normal, Locked, Secret, SafeExit }

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

    [Header("스프라이트 — Safe 출구 옆면 문 (좌우 통로, 비우면 일반 옆면 문으로 폴백)")]
    [SerializeField] private Sprite sideSafeExitClosedSprite;
    [SerializeField] private Sprite sideSafeExitOpenSprite;

    [Header("스프라이트 — Safe 출구 위/아래 문 (상하 통로, 비우면 일반 위/아래 문으로 폴백)")]
    [SerializeField] private Sprite topSafeExitClosedSprite;
    [SerializeField] private Sprite topSafeExitOpenSprite;

    [Header("동작")]
    [Tooltip("Normal 문: 플레이어가 인접하면 자동으로 열리고, 멀어지면 닫힘.")]
    [SerializeField] private bool autoOpenWhenAdjacent = true;

    [Tooltip("Locked 문 개방 시간(초). GDD 9.15: 1초.")]
    [SerializeField] private float lockedOpenSeconds = 1f;

    [Header("Secret 패시브 발견 (근처에 있으면 매초 확률로 공개)")]
    [Tooltip("패시브 발견 범위 — 체비셰프 거리(1 = 플레이어 기준 3x3). F 탐색(즉시)과 별개.")]
    [SerializeField, Min(0)] private int passiveDiscoveryRange = 1;
    [Tooltip("범위 안에 머무는 동안 1초마다 발견 확률(0~1). 성공 시 즉시 발견.")]
    [SerializeField, Range(0f, 1f)] private float passiveDiscoveryChancePerSecond = 0.15f;

    [Header("씬 직접 배치(튜토리얼 등) — MapGenerator 없이 자체 초기화")]
    [Tooltip("켜면 Initialize 가 안 불렸을 때 Start 에서 현재 위치·아래 설정으로 자체 초기화.")]
    [SerializeField] private bool selfInitInScene = false;
    [Tooltip("자체 초기화 시 문 종류.")]
    [SerializeField] private DoorKind sceneKind = DoorKind.Secret;
    [Tooltip("자체 초기화 시 통로 방향 — true=좌우 통로(세로 벽 사이), false=상하 통로.")]
    [SerializeField] private bool sceneHorizontalPassage = true;
    [Tooltip("자체 초기화 SafeExit 전용 — 안전방 '바깥'(나가는 쪽) 방향. 이 방향 이웃 셀을 바깥 셀로 등록해 " +
             "플레이어가 나가 멀어지면 영구 잠금. 예: 안전방이 왼쪽이면 (1,0).")]
    [SerializeField] private Vector2Int sceneSafeExitOutsideDir = Vector2Int.right;

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private bool _horizontalPassage;
    private float _openingProgress;
    private float _secretRollTimer;
    private DungeonRandom _discoveryRng;
    private bool _initialized;

    // SafeExit 전용 — 플레이어가 outsideCell 에 도달하면 _hasExited=true, 멀어지며 닫힐 때 영구 잠김.
    private Vector2Int _safeOutsideCell;
    private bool _safeOutsideCellSet;
    private bool _safeHasExited;
    private bool _safePermLocked;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        GameEvents.OnVisionUpdated += OnVisionUpdated;
        GameEvents.OnSearchPerformed += OnSearchPerformed;
    }

    private void OnDisable()
    {
        GameEvents.OnVisionUpdated -= OnVisionUpdated;
        GameEvents.OnSearchPerformed -= OnSearchPerformed;
    }

    /// <summary>탐색 액션 — center 기준 radius(체비셰프) 안의 숨은 Secret 문이면 즉시 발견.</summary>
    private void OnSearchPerformed(Vector2Int center, int radius)
    {
        if (Kind != DoorKind.Secret || State != DoorState.Hidden) return;
        int cheb = Mathf.Max(Mathf.Abs(GridPosition.x - center.x),
                             Mathf.Abs(GridPosition.y - center.y));
        if (cheb <= radius) Discover();
    }

    /// <summary>맵 생성기가 호출 — 셀 위치·통로 방향·문 종류를 지정하고 격자에 등록.</summary>
    public void Initialize(Vector2Int cell, bool horizontalPassage, DoorKind kind = DoorKind.Normal)
    {
        GridPosition = cell;
        _horizontalPassage = horizontalPassage;
        Kind = kind;
        _initialized = true;
        if (kind == DoorKind.Secret) State = DoorState.Hidden;
        EnsureDiscoveryRng();
        transform.position = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);

        if (GridSystem.Instance != null) GridSystem.Instance.RegisterDoor(cell, this);
        ApplyState();
    }

    /// <summary>SafeExit 문에 '바깥 셀' 등록 — MapGenerator 가 Initialize 직후 호출. (GDD 11.1)</summary>
    public void ConfigureSafeExit(Vector2Int outsideCell)
    {
        _safeOutsideCell = outsideCell;
        _safeOutsideCellSet = true;
    }

    private void Start()
    {
        _player = FindFirstObjectByType<GridPlayer>();

        // 씬에 직접 배치된 문(튜토리얼 등) — Initialize 가 안 불렸으면 현재 위치로 자체 초기화.
        if (!_initialized && selfInitInScene)
        {
            Vector2Int cell = GridSystem.Instance != null
                ? GridSystem.Instance.WorldToGrid(transform.position)
                : Vector2Int.zero;
            Initialize(cell, sceneHorizontalPassage, sceneKind);
            // 씬 배치 SafeExit 도 바깥 셀을 등록해야 '나가면 잠금'이 작동 (MapGenerator 대체).
            if (sceneKind == DoorKind.SafeExit)
                ConfigureSafeExit(cell + sceneSafeExitOutsideDir);
        }

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
        // Locked/SafeExit 문은 종류별 전용 슬롯(있으면) 우선, 없으면 일반 슬롯으로 폴백.
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
        else if (Kind == DoorKind.SafeExit)
        {
            Sprite safe = _horizontalPassage
                ? (showOpen ? sideSafeExitOpenSprite : sideSafeExitClosedSprite)
                : (showOpen ? topSafeExitOpenSprite : topSafeExitClosedSprite);
            if (safe != null) s = safe;
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
                UpdateSecret();
                break;

            case DoorKind.SafeExit:
                UpdateSafeExit(adjacent);
                break;
        }
    }

    private void UpdateSafeExit(bool adjacent)
    {
        // 영구 잠금 후엔 항상 닫힘 유지 — 열쇠도 무시(GDD 11.1).
        if (_safePermLocked)
        {
            if (State != DoorState.Closed) SetState(DoorState.Closed);
            return;
        }

        // 잠기기 전엔 일반 문처럼 자동 개폐.
        if (autoOpenWhenAdjacent)
            SetState(adjacent ? DoorState.Open : DoorState.Closed);

        // 플레이어가 바깥 셀에 한 번이라도 도달 → 빠져나간 것으로 간주.
        if (_safeOutsideCellSet && _player.GridPosition == _safeOutsideCell)
            _safeHasExited = true;

        // 빠져나간 적이 있고 지금 문 옆에 없으면(자연스럽게 닫힘) 영구 잠금 전환.
        if (_safeHasExited && !adjacent)
        {
            _safePermLocked = true;
            SetState(DoorState.Closed);
        }
    }

    // 패시브 발견 — 범위(체비셰프 passiveDiscoveryRange) 안에 머무는 동안 1초마다 확률 롤.
    // F 탐색(OnSearchPerformed)은 별개로 즉시 발견시킨다.
    private void UpdateSecret()
    {
        if (State != DoorState.Hidden) return; // 발견 후엔 그대로 열림 유지

        int cheb = Mathf.Max(Mathf.Abs(_player.GridPosition.x - GridPosition.x),
                             Mathf.Abs(_player.GridPosition.y - GridPosition.y));
        if (cheb > passiveDiscoveryRange) { _secretRollTimer = 0f; return; }

        EnsureDiscoveryRng();
        _secretRollTimer += Time.deltaTime;
        while (_secretRollTimer >= 1f)
        {
            _secretRollTimer -= 1f;
            if (_discoveryRng.Chance(passiveDiscoveryChancePerSecond)) { Discover(); return; }
        }
    }

    /// <summary>문별 발견 확률 롤용 RNG — 셀 좌표 시드(GridChest 와 같은 패턴). UnityEngine.Random 직접 호출 금지.</summary>
    private void EnsureDiscoveryRng()
    {
        if (_discoveryRng != null) return;
        _discoveryRng = new DungeonRandom();
        _discoveryRng.Init((GridPosition.x * 73856093) ^ (GridPosition.y * 19349663));
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
        GameEvents.OnSecretDiscovered?.Invoke(GridPosition);
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
