using System.Collections.Generic;
using UnityEngine;
using PrototypeRT;

// 마법 부여 제단 (Secret Enchant 방). 인접+상호작용키 → 무기/방어구 선택 팝업
[RequireComponent(typeof(SpriteRenderer))]
public class EnchantAltar : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public bool IsUsed => _used;

    // 미리 확정된 부여 마법 (결정론 — 상호작용 시점 비결정성 회피)
    public MagicData RolledWeaponMagic { get; private set; }
    public MagicData RolledArmorMagic { get; private set; }

    [Header("마법 풀")]
    [Tooltip("무기 부여 후보")]
    [SerializeField] private MagicData[] weaponMagics;
    [Tooltip("방어구 부여 후보")]
    [SerializeField] private MagicData[] armorMagics;

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private EnchantPopup _popup;
    private bool _used;
    private bool _wasAdjacent;

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

    // 스포너가 셀·깊이 지정 → 결정론 시드로 마법 확정
    public void Initialize(Vector2Int cell, int depth)
    {
        GridPosition = cell;
        transform.position = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);

        int seed = (MapGenerator.Instance != null ? MapGenerator.Instance.CurrentSeed : 0)
            ^ (depth * 92821) ^ (cell.x * 73856093) ^ (cell.y * 19349663);
        var rng = new DungeonRandom();
        rng.Init(seed);
        RolledWeaponMagic = Pick(weaponMagics, rng);
        RolledArmorMagic = Pick(armorMagics, rng);
    }

    private static MagicData Pick(MagicData[] pool, DungeonRandom rng)
    {
        if (pool == null || pool.Length == 0)
            return null;
        return pool[rng.Range(0, pool.Length)];
    }

    private void Start()
    {
        if (GridPosition == Vector2Int.zero && GridSystem.Instance != null)
            GridPosition = GridSystem.Instance.WorldToGrid(transform.position);

        _player = FindFirstObjectByType<GridPlayer>();
        _popup = FindFirstObjectByType<EnchantPopup>(FindObjectsInactive.Include);
        UpdateVisibility();
    }

    private void Update()
    {
        if (_used)
            return;
        if (_player == null)
        {
            _player = FindFirstObjectByType<GridPlayer>();
            if (_player == null)
                return;
        }
        if (_player.IsDead)
            return;

        // 체비셰프 거리 — 대각선 포함 8방향(둘러싼 3x3)이면 인접
        int dx = Mathf.Abs(_player.GridPosition.x - GridPosition.x);
        int dy = Mathf.Abs(_player.GridPosition.y - GridPosition.y);
        bool adjacent = Mathf.Max(dx, dy) <= 1;

        // 떠나면 다시 접근 시 재오픈 가능하게 리셋
        if (!adjacent)
        {
            _wasAdjacent = false;
            return;
        }

        // 접근당 1회만 — 취소 후 그 자리서 재오픈 루프 방지
        if (_wasAdjacent)
            return;
        if (_popup != null && _popup.IsOpen)
            return;

        if (_popup == null)
            _popup = FindFirstObjectByType<EnchantPopup>(FindObjectsInactive.Include);
        if (_popup == null)
        {
            Debug.LogWarning("[EnchantAltar] EnchantPopup 씬에 없음 — 팝업 배선 필요");
            return;
        }

        _wasAdjacent = true;
        _popup.Open(this);
    }

    // 부여 완료 — 제단 소모
    public void MarkUsed()
    {
        _used = true;
        Destroy(gameObject);
    }

    private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible, IReadOnlyCollection<Vector2Int> explored)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_renderer == null || VisionSystem.Instance == null)
            return;
        _renderer.enabled = VisionSystem.Instance.IsExplored(GridPosition);
    }
}
