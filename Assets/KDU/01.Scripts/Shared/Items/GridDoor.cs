using UnityEngine;
using System.Collections.Generic;

// 격자 문
[RequireComponent(typeof(SpriteRenderer))]
public class GridDoor : MonoBehaviour
{
    public enum DoorState { Closed, Opening, Open, Hidden }
    public enum DoorKind { Normal, Locked, Secret, SafeExit }

    public Vector2Int GridPosition { get; private set; }
    public DoorState State { get; private set; } = DoorState.Closed;
    public DoorKind Kind { get; private set; } = DoorKind.Normal;
    public bool IsOpen => State == DoorState.Open;

    [Header("스프라이트 — 옆면 문")]
    [SerializeField] private Sprite sideClosedSprite;
    [SerializeField] private Sprite sideOpenSprite;

    [Header("스프라이트 — 위/아래 문")]
    [SerializeField] private Sprite topClosedSprite;
    [SerializeField] private Sprite topOpenSprite;

    [Header("스프라이트 — 잠긴 옆면 문")]
    [SerializeField] private Sprite sideLockedClosedSprite;
    [SerializeField] private Sprite sideLockedOpenSprite;

    [Header("스프라이트 — 잠긴 위/아래 문")]
    [SerializeField] private Sprite topLockedClosedSprite;
    [SerializeField] private Sprite topLockedOpenSprite;

    [Header("스프라이트 — Safe 출구 옆면 문")]
    [SerializeField] private Sprite sideSafeExitClosedSprite;
    [SerializeField] private Sprite sideSafeExitOpenSprite;

    [Header("스프라이트 — Safe 출구 위/아래 문")]
    [SerializeField] private Sprite topSafeExitClosedSprite;
    [SerializeField] private Sprite topSafeExitOpenSprite;

    [Header("동작")]
    [Tooltip("Normal 문")]
    [SerializeField] private bool autoOpenWhenAdjacent = true;

    [Tooltip("Locked 문 열때 걸리는 시간")]
    [SerializeField] private float lockedOpenSeconds = 1f;

    [Header("Secret 패시브 발견")]
    [SerializeField, Min(0)] private int passiveDiscoveryRange = 3;
    [SerializeField, Range(0f, 1f)] private float passiveDiscoveryChancePerSecond = 0.15f;

    [Header("튜토리얼용 직접 배치 설정")]
    [SerializeField] private bool selfInitInScene = false;
    [SerializeField] private DoorKind sceneKind = DoorKind.Secret;
    [SerializeField] private bool sceneHorizontalPassage = true;
    [SerializeField] private Vector2Int sceneSafeExitOutsideDir = Vector2Int.right;

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private bool _horizontalPassage;
    private float _openingProgress;
    private float _secretRollTimer;
    private DungeonRandom _discoveryRng;
    private bool _initialized;

    // SafeExit 전용
    private Vector2Int _safeOutsideCell;
    private bool _safeOutsideCellSet;
    private bool _safePermLocked;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        GameEvents.OnVisionUpdated += OnVisionUpdated;
        GameEvents.OnSearchPerformed += OnSearchPerformed;
        GameEvents.OnBossFightStarted += OnBossFightStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnVisionUpdated -= OnVisionUpdated;
        GameEvents.OnSearchPerformed -= OnSearchPerformed;
        GameEvents.OnBossFightStarted -= OnBossFightStarted;
    }

    // 보스 연출 종료 시 세이프 출구 영구 잠금 — AttackFloor 밖(세이프룸) 복귀 차단
    private void OnBossFightStarted()
    {
        if (Kind != DoorKind.SafeExit)
            return;

        _safePermLocked = true;
        SetState(DoorState.Closed);
    }

    // 탐색
    private void OnSearchPerformed(Vector2Int center, int radius)
    {
        if (Kind != DoorKind.Secret || State != DoorState.Hidden) return;
        int cheb = Mathf.Max(Mathf.Abs(GridPosition.x - center.x),
                             Mathf.Abs(GridPosition.y - center.y));
        if (cheb <= radius) Discover();
    }

    // 문 초기화
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

    public void ConfigureSafeExit(Vector2Int outsideCell)
    {
        _safeOutsideCell = outsideCell;
        _safeOutsideCellSet = true;
    }

    private void Start()
    {
        _player = FindFirstObjectByType<GridPlayer>();

        // 튜토리얼용 초기화
        if (!_initialized && selfInitInScene)
        {
            Vector2Int cell = GridSystem.Instance != null
                ? GridSystem.Instance.WorldToGrid(transform.position)
                : Vector2Int.zero;
            Initialize(cell, sceneHorizontalPassage, sceneKind);

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
        DoorState previousState = State;

        State = state;
        if (state != DoorState.Opening) _openingProgress = 0f;
        ApplyState();

        PlayDoorOpenedSfx(previousState, state);

        // 문 열리면 시야 갱신
        if (IsOpen && VisionSystem.Instance != null && _player != null)
            VisionSystem.Instance.UpdateVision(_player.GridPosition);
    }

    private void PlayDoorOpenedSfx(DoorState previousState, DoorState currentState)
    {
        if (previousState == DoorState.Open || currentState != DoorState.Open)
            return;

        GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.DoorOpened);
    }

    private void ApplyState()
    {
        if (_renderer == null) return;
        // 방향별로 닫힘/열림 스프라이트가 다름
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
        // 한번 나오면 다시 못들어가게 막기
        if (_safePermLocked)
        {
            if (State != DoorState.Closed) SetState(DoorState.Closed);
            return;
        }

        // 바깥 셀에 도달하는 즉시 잠금 — 걸침 창 제거(적 진입/재진입 차단)
        if (_safeOutsideCellSet && _player.GridPosition == _safeOutsideCell)
        {
            _safePermLocked = true;
            SetState(DoorState.Closed);
            return;
        }

        // 잠기기 전엔 일반 문처럼 자동 개폐.
        if (autoOpenWhenAdjacent)
            SetState(adjacent ? DoorState.Open : DoorState.Closed);
    }

    // 자동 발견
    private void UpdateSecret()
    {
        if (State != DoorState.Hidden) 
            return; // 발견 후엔 그대로 열림 유지

        int cheb = Mathf.Max(Mathf.Abs(_player.GridPosition.x - GridPosition.x),
                             Mathf.Abs(_player.GridPosition.y - GridPosition.y));
        if (cheb > passiveDiscoveryRange) 
        { 
            _secretRollTimer = 0f; 
            return; 
        }

        EnsureDiscoveryRng();
        _secretRollTimer += Time.deltaTime;
        float chance = Mathf.Clamp01(passiveDiscoveryChancePerSecond * SearchChanceMultiplier());
        while (_secretRollTimer >= 1f)
        {
            _secretRollTimer -= 1f;
            if (_discoveryRng.Chance(chance))
            {
                Discover();
                return;
            }
        }
    }

    // 조심스러운 발걸음 특성 — 패시브 탐색 확률 배율
    private float SearchChanceMultiplier()
    {
        TraitController trait = TraitController.FindAvailableInstance();
        return trait != null ? trait.GetCarefulStepSearchMultiplier() : 1f;
    }

    private void EnsureDiscoveryRng()
    {
        if (_discoveryRng != null) 
            return;

        _discoveryRng = new DungeonRandom();
        _discoveryRng.Init((GridPosition.x * 73856093) ^ (GridPosition.y * 19349663));
    }

    public void Discover()
    {
        if (Kind != DoorKind.Secret || State != DoorState.Hidden)
            return;

        if (GridSystem.Instance != null && GridSystem.Instance.Tilemaps != null && GridSystem.Instance.Tilemaps.wall != null)
        {
            GridSystem.Instance.Tilemaps.wall.SetTile(new Vector3Int(GridPosition.x, GridPosition.y, 0), null);
        }

        SetState(DoorState.Open);
        GameEvents.OnSecretDiscovered?.Invoke(GridPosition);

        if (GridSystem.Instance != null)
        {
            TryDiscoverNeighborSecret(GridPosition + Vector2Int.up);
            TryDiscoverNeighborSecret(GridPosition + Vector2Int.down);
            TryDiscoverNeighborSecret(GridPosition + Vector2Int.left);
            TryDiscoverNeighborSecret(GridPosition + Vector2Int.right);
        }
    }

    private void TryDiscoverNeighborSecret(Vector2Int cell)
    {
        var d = GridSystem.Instance.GetDoorAt(cell);

        if (d != null) 
            d.Discover();
    }

    private void UpdateLocked(bool adjacent)
    {
        if (State == DoorState.Open) 
            return;

        if (!adjacent)
        {
            if (State == DoorState.Opening) 
                SetState(DoorState.Closed);

            return;
        }

        var inv = KeyInventory.Instance;
        bool hasKey = inv != null && inv.Has(KeyType.SpecialRoom);

        if (!hasKey)
        {
            if (State == DoorState.Opening) 
                SetState(DoorState.Closed);

            return;
        }

        if (State != DoorState.Opening) 
            SetState(DoorState.Opening);

        _openingProgress += Time.deltaTime;

        if (_openingProgress >= lockedOpenSeconds)
        {
            if (inv.TryConsume(KeyType.SpecialRoom)) 
                SetState(DoorState.Open);
            else 
                SetState(DoorState.Closed);
        }
    }

    private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible, IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_renderer == null) 
            return;
        if (State == DoorState.Hidden) 
        { 
            _renderer.enabled = false; 
            return;
        }
        if (VisionSystem.Instance == null) 
            return;

        _renderer.enabled = VisionSystem.Instance.IsExplored(GridPosition);
    }
}
