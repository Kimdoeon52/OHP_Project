using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 격자 플레이어 메인 브레인. 4개 서브 컴포넌트를 조정하고 IDamageable을 위임.
/// </summary>
[RequireComponent(typeof(GridPlayerInput))]
[RequireComponent(typeof(GridPlayerMovement))]
[RequireComponent(typeof(GridPlayerCombat))]
[RequireComponent(typeof(GridPlayerHealth))]
[RequireComponent(typeof(PlayerActionDelay))]
[RequireComponent(typeof(PlayerLevelSystem))]
public class GridPlayer : EntityBase, IDamageable
{
    [SerializeField] private GameConfig config;

    public GameConfig Config => config;

    public GridPlayerInput Input { get; private set; }
    public GridPlayerMovement Movement { get; private set; }
    public GridPlayerCombat Combat { get; private set; }
    public GridPlayerHealth Health { get; private set; }
    public PlayerActionDelay ActionDelay { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        ResolveConfigIfMissing();

        Input = GetComponent<GridPlayerInput>();
        Movement = GetComponent<GridPlayerMovement>();
        Combat = GetComponent<GridPlayerCombat>();
        Health = GetComponent<GridPlayerHealth>();
        ActionDelay = GetComponent<PlayerActionDelay>();
        if (ActionDelay == null)
            ActionDelay = gameObject.AddComponent<PlayerActionDelay>();

        Movement.Init(this);
        Combat.Init(this);
        Health.Init(this);
        Input.Init(this);
    }

    /// <summary>
    /// 씬에 연결된 GameConfig 참조가 깨졌을 때, 에디터 플레이에서는 프로젝트 안의 GameConfig 에셋을 자동으로 찾아 연결합니다.
    /// Config가 비어 있으면 이동 딜레이 계산이 1초 기본값으로 떨어지기 때문에, 플레이 테스트 중 설정값이 무시되는 것을 막는 안전장치입니다.
    /// </summary>
    private void ResolveConfigIfMissing()
    {
        if (config != null) return;

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:GameConfig");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
        }
#endif

        if (config == null)
        {
            Debug.LogWarning("GridPlayer: GameConfig가 연결되지 않아 행동 딜레이가 기본값으로 동작할 수 있습니다.");
        }
    }

    private void Start()
    {
        // 씬에 배치된 위치를 격자에 스냅
        var startPos = GridSystem.Instance.WorldToGrid(transform.position);
        Initialize(startPos);

        VisionSystem.Instance.UpdateVision(GridPosition);
        TimeSystem.Instance.StartTimer();
    }

    public override void OnTurnUpdate()
    {
        // 플레이어는 입력으로 구동되며 TurnManager 엔티티 리스트에 등록되지 않음
    }

    // IDamageable 위임
    public int CurrentHp => Health.CurrentHp;
    public int MaxHp => Health.MaxHp;
    public bool IsDead => Health.IsDead;
    public void TakeDamage(int amount) => Health.TakeDamage(amount);
    public void Heal(int amount) => Health.Heal(amount);
}
