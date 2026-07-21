using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 상자
[RequireComponent(typeof(SpriteRenderer))]
public class GridChest : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public bool IsOpened => _opened;

    [Header("상자 종류")]
    [SerializeField] private ChestData data;

    [Header("풀 / 프리팹")]
    [SerializeField] private ItemPool itemPool;
    [SerializeField] private ItemPickup itemPickupPrefab;
    [SerializeField] private GoldPickup goldPickupPrefab;
    [SerializeField] private CurseData[] cursePool;

    [SerializeField, Min(0f)] private float openAnimationSeconds = 0.5f;
    private string openTrigger = "Open";

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

    public void Initialize(Vector2Int cell, ChestData chestData, DungeonRandom rng)
    {
        GridPosition = cell;
        data = chestData;
        transform.position = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(cell) : new Vector3(cell.x, cell.y, 0);

        RollRewards(rng);
        _initialized = true;
        ApplySprite();
    }

    private void Start()
    {
        _player = FindFirstObjectByType<GridPlayer>();

        // 튜토리얼용 직접 배치 상자
        if (!_initialized && data != null)
        {
            Vector2Int cell = GridSystem.Instance != null ? GridSystem.Instance.WorldToGrid(transform.position) : Vector2Int.zero;

            var rng = new DungeonRandom();
            rng.Init((cell.x * 73856093) ^ (cell.y * 19349663)); // 해시 기반 시드
            Initialize(cell, data, rng);
        }

        UpdateVisibility();
    }

    private void Update()
    {
        if (_opened || data == null || _player == null || _player.IsDead) 
            return;

        // 플레이어와 겹친 상자만 열림
        bool overlapping = _player.GridPosition == GridPosition;

        if (!overlapping)
        {
            _openProgress = 0f;
            return;
        }

        // 열쇠 필요
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

        if (_openProgress >= GetEffectiveOpenSeconds())
            TryOpen();
    }

    // 탐욕의 초침 마법이 있으면 0.5초 고정, 아니면 data.OpenSeconds
    private float GetEffectiveOpenSeconds()
    {
        if (_player == null)
            return data.OpenSeconds;
        NYHEquipmentController eq = _player.GetComponent<NYHEquipmentController>();
        if (eq == null || !eq.TryGetEquipped(EquipmentSlot.Armor, out NYHEquipmentItemInstance item))
            return data.OpenSeconds;
        if (item.SourceInstance is EquipmentInstance inst && inst.magic != null && inst.magic.OverridesPickupTime)
            return 0.5f;
        return data.OpenSeconds;
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
        GameEvents.OnChestOpened?.Invoke(GridPosition);

        var animator = GetComponentInChildren<Animator>();

        if (animator != null && !string.IsNullOrEmpty(openTrigger))
            animator.SetTrigger(openTrigger);
        else
            ApplySprite();

        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        if (openAnimationSeconds > 0f)
            yield return new WaitForSeconds(openAnimationSeconds);

        DropRewards();
        Destroy(gameObject);
    }

    // 보상 확정
    private void RollRewards(DungeonRandom rng)
    {
        _itemRewards.Clear();
        _goldReward = 0;
        if (data == null || rng == null) 
            return;

        var candidates = BuildCandidates();
        int count = rng.Range(data.ItemDropMin, data.ItemDropMax + 1);

        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            ItemBase so = candidates[rng.Range(0, candidates.Count)];
            _itemRewards.Add(new Reward
            {
                so = so,
                enhanceLevel = so.CanEnhance ? ItemRoll.Enhance(rng) : 0,
                curse = so.CanCurse && ItemRoll.IsCurseEligible(so) ? ItemRoll.Curse(rng, cursePool, so.Slot) : null,
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
            if (so == null) 
                continue;

            if (data.ExcludeItemIds != null && System.Array.IndexOf(data.ExcludeItemIds, so.Id) >= 0)
                continue;

            if (System.Array.IndexOf(data.ItemCategories, ItemCategories.Of(so)) >= 0)
                result.Add(so);
        }

        return result;
    }

    // 아이템 드롭
    private void DropRewards()
    {
        for (int i = 0; i < _itemRewards.Count; i++)
            SpawnPickup(_itemRewards[i], DropCell(i));

        // 골드
        if (_goldReward > 0 && goldPickupPrefab != null)
        {
            Vector2Int cell = DropCell(_itemRewards.Count);
            Vector3 pos = GridSystem.Instance != null
                ? GridSystem.Instance.GridToWorld(cell)
                : new Vector3(cell.x, cell.y, 0);
            GoldPickup gold = Instantiate(goldPickupPrefab, pos, Quaternion.identity);
            gold.Initialize(cell, _goldReward);
        }
    }

    private void SpawnPickup(Reward reward, Vector2Int cell)
    {
        if (itemPickupPrefab == null || reward.so == null)
            return;

        Vector3 pos = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(cell)
            : new Vector3(cell.x, cell.y, 0);

        ItemPickup pickup = Instantiate(itemPickupPrefab, pos, Quaternion.identity);

        pickup.itemSO = reward.so;
        pickup.enhanceLevel = reward.enhanceLevel;
        pickup.curse = reward.curse;
        pickup.Initialize(cell);
    }

    // 보상 분산
    private Vector2Int DropCell(int index)
    {
        if (index == 0) 
            return GridPosition;

        var free = new List<Vector2Int> { GridPosition };

        foreach (var d in Neighbors)
        {
            Vector2Int n = GridPosition + d;
            if (GridSystem.Instance == null || GridSystem.Instance.IsWalkable(n))
                free.Add(n);
        }

        return free[index % free.Count];
    }

    // 시야 기반 표시

    private void ApplySprite()
    {
        if (_renderer == null || data == null) 
            return;

        Sprite s = _opened ? data.OpenedSprite : data.ClosedSprite;
        if (s != null) _renderer.sprite = s;
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
