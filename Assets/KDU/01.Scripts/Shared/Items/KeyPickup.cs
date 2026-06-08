using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 바닥에 떨어진 열쇠 픽업. 플레이어가 같은 셀에 들어오면 KeyInventory 에 적립 후 자기 자신 파괴.
/// 시야 밖일 때는 렌더러 숨김(탐험된 셀이면 표시 유지) — GridDoor 와 같은 패턴.
/// MapGenerator 가 생성 시점에 Initialize 로 셀·종류 지정.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class KeyPickup : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public KeyType KeyType => keyType;

    [Tooltip("기본 종류 — MapGenerator 가 Initialize 로 덮어씀.")]
    [SerializeField] private KeyType keyType = KeyType.SpecialRoom;

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private bool _collected;
    private bool _initialized;

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

    public void Initialize(Vector2Int cell, KeyType type)
    {
        GridPosition = cell;
        keyType = type;
        _initialized = true;
        transform.position = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);
    }

    private void Start()
    {
        _player = FindFirstObjectByType<GridPlayer>();

        // 씬에 직접 배치(튜토리얼 등) — Initialize 가 안 불렸으면 현재 위치로 자기 칸을 잡는다.
        if (!_initialized && GridSystem.Instance != null)
            GridPosition = GridSystem.Instance.WorldToGrid(transform.position);

        UpdateVisibility();
    }

    private void Update()
    {
        if (_collected || _player == null || _player.IsDead) return;
        if (_player.GridPosition != GridPosition) return;
        TryCollect();
    }

    private void TryCollect()
    {
        if (KeyInventory.Instance == null) return; // 인벤토리 없으면 줍지 못함 — 살아남기.
        KeyInventory.Instance.Grant(keyType);
        _collected = true;
        Destroy(gameObject);
    }

    private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible,
                                 IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_renderer == null || VisionSystem.Instance == null) return;
        _renderer.enabled = VisionSystem.Instance.IsExplored(GridPosition);
    }
}
