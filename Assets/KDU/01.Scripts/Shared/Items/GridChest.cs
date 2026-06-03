using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 격자 위 상자 (GDD 9.14). GridDoor / KeyPickup 와 같은 패턴.
/// 플레이어가 인접하면 openSeconds(기본 1초) 동안 머물러 개방 → 보상 드롭.
/// 반지 상자(RequiresKey)는 해당 열쇠 보유 시에만 개방되며 개방 시 열쇠 1개 소비.
/// 보상은 생성 시점(Initialize)에 전달받은 DungeonRandom 으로 미리 굴려두어 시드 결정론 유지.
/// 시야 밖에서는 렌더러를 숨기고, 한 번 탐험된 셀이면 계속 표시.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GridChest : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public bool IsOpened => _opened;

    [Header("상자 종류 — MapGenerator 가 Initialize 로 덮어씀")]
    [SerializeField] private ChestData data;

    [Header("전리품 풀 / 프리팹")]
    [Tooltip("아이템 추첨 대상 마스터 풀 (ID 구간으로 필터).")]
    [SerializeField] private ItemPool itemPool;
    [Tooltip("드롭할 아이템 픽업 프리팹.")]
    [SerializeField] private ItemPickup itemPickupPrefab;
    [Tooltip("장비에 부여할 수 있는 저주 풀 (비우면 저주 없음).")]
    [SerializeField] private CurseData[] cursePool;

    private struct Reward
    {
        public ItemBase so;
        public int enhanceLevel;
        public CurseData curse;
    }

    private readonly List<Reward> _itemRewards = new();
    private int _goldReward;

    private SpriteRenderer _renderer;
    private GridPlayer _player;
    private bool _initialized;
    private bool _opened;
    private float _openProgress;

    private static readonly Vector2Int[] Neighbors =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

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

    /// <summary>MapGenerator/스포너가 호출 — 셀·종류 지정 후 보상을 결정론적으로 굴려둔다.</summary>
    public void Initialize(Vector2Int cell, ChestData chestData, DungeonRandom rng)
    {
        GridPosition = cell;
        data = chestData;
        transform.position = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);

        RollRewards(rng);
        _initialized = true;
        ApplySprite();
    }

    private void Start()
    {
        _player = FindFirstObjectByType<GridPlayer>();

        // 씬에 직접 배치된 테스트용 상자 — Initialize 가 안 불렸으면 현재 위치로 자체 초기화.
        if (!_initialized && data != null)
        {
            Vector2Int cell = GridSystem.Instance != null
                ? GridSystem.Instance.WorldToGrid(transform.position)
                : Vector2Int.zero;
            var rng = new DungeonRandom();
            rng.Init((cell.x * 73856093) ^ (cell.y * 19349663));
            Initialize(cell, data, rng);
        }

        UpdateVisibility();
    }

    private void Update()
    {
        if (_opened || data == null || _player == null || _player.IsDead) return;

        int dist = Mathf.Abs(_player.GridPosition.x - GridPosition.x)
                 + Mathf.Abs(_player.GridPosition.y - GridPosition.y);
        bool adjacent = dist <= 1;

        if (!adjacent)
        {
            _openProgress = 0f;
            return;
        }

        // 반지 상자 등 — 열쇠 없으면 개방 진행 불가.
        if (data.RequiresKey)
        {
            var inv = KeyInventory.Instance;
            if (inv == null || !inv.Has(data.KeyType))
            {
                _openProgress = 0f;
                return;
            }
        }

        _openProgress += Time.deltaTime;
        if (_openProgress >= data.OpenSeconds)
            TryOpen();
    }

    private void TryOpen()
    {
        if (data.RequiresKey)
        {
            var inv = KeyInventory.Instance;
            if (inv == null || !inv.TryConsume(data.KeyType))
            {
                _openProgress = 0f;
                return;
            }
        }

        _opened = true;
        ApplySprite();
        DropRewards();
    }

    // ───── 보상 추첨 (Initialize 시점, 시드 결정론) ─────

    private void RollRewards(DungeonRandom rng)
    {
        _itemRewards.Clear();
        _goldReward = 0;
        if (data == null || rng == null) return;

        var candidates = BuildCandidates();
        int count = rng.Range(data.ItemDropMin, data.ItemDropMax + 1);
        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            ItemBase so = candidates[rng.Range(0, candidates.Count)];
            _itemRewards.Add(new Reward
            {
                so = so,
                enhanceLevel = so.CanEnhance ? ItemRoll.Enhance(rng) : 0,
                curse = so.CanCurse ? ItemRoll.Curse(rng, cursePool) : null,
            });
        }

        if (data.GoldMax > 0)
            _goldReward = rng.Range(data.GoldMin, data.GoldMax + 1);
    }

    private List<ItemBase> BuildCandidates()
    {
        var result = new List<ItemBase>();
        if (itemPool == null || itemPool.items == null || data.ItemCategories == null)
            return result;

        foreach (var so in itemPool.items)
        {
            if (so == null) continue;
            if (data.ExcludeReinforceScroll && so is ReinforceScroll) continue;
            if (System.Array.IndexOf(data.ItemCategories, ItemCategories.Of(so)) >= 0)
                result.Add(so);
        }
        return result;
    }

    // ───── 개방 시 드롭 ─────

    private void DropRewards()
    {
        for (int i = 0; i < _itemRewards.Count; i++)
            SpawnPickup(_itemRewards[i], DropCell(i));

        if (_goldReward > 0)
            GameEvents.OnGoldDropped?.Invoke(GridPosition, _goldReward);
    }

    private void SpawnPickup(Reward reward, Vector2Int cell)
    {
        if (itemPickupPrefab == null || reward.so == null) return;

        Vector3 pos = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);

        ItemPickup pickup = Instantiate(itemPickupPrefab, pos, Quaternion.identity);
        // ItemPickup.Start() 가 이 필드들로 인스턴스를 구성한다.
        pickup.itemSO = reward.so;
        pickup.enhanceLevel = reward.enhanceLevel;
        pickup.curse = reward.curse;
        pickup.Initialize(cell);
    }

    /// <summary>드롭 셀: 상자 칸 우선, 보상이 여럿이면 보행 가능한 인접 칸으로 분산.</summary>
    private Vector2Int DropCell(int index)
    {
        if (index == 0) return GridPosition;

        var free = new List<Vector2Int> { GridPosition };
        foreach (var d in Neighbors)
        {
            Vector2Int n = GridPosition + d;
            if (GridSystem.Instance == null || GridSystem.Instance.IsWalkable(n))
                free.Add(n);
        }
        return free[index % free.Count];
    }

    // ───── 시야 기반 표시 (GridDoor / KeyPickup 패턴) ─────

    private void ApplySprite()
    {
        if (_renderer == null || data == null) return;
        Sprite s = _opened ? data.OpenedSprite : data.ClosedSprite;
        if (s != null) _renderer.sprite = s;
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
