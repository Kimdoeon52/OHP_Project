using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 튜토리얼 선형 진행 컨트롤러 (TUTORIAL_PLAN.md). 고정 튜토리얼 씬에 1개 배치.
/// steps 를 순서대로 진행 — 각 단계의 완료 조건이 충족되면 다음 단계 안내로 넘어간다.
/// 마지막(ReachExit) 단계에서 인벤토리/열쇠를 비우고 1층으로 넘긴다.
///
/// A그룹(즉시 구현) 조건만 실제 판정한다. B그룹(MapScrollUsed / InventoryOpened)은
/// 팀원 이벤트 훅이 붙기 전까지 아무 키 입력(anyKeyDown) 수동 진행으로 골격이 끝까지 돌아가게 둔다.
/// 콘텐츠(적 배치 등)가 아직 없을 때 전체 흐름을 점검하려면 debugSkipKey 로 강제 진행.
/// </summary>
public class TutorialController : MonoBehaviour
{
    public enum Condition
    {
        PressAdvanceKey,   // 1 시작
        MapScrollUsed,     // 2 (B) TODO: 지도 스크롤 사용 이벤트 — 현재 임시 수동(advanceKey)
        CameraPanned,      // 3 SafeRoom 카메라 줌/팬
        InventoryOpened,   // 4 인벤토리 열림 이벤트
        EnterRegion,       // 5/7/9 영역 진입
        DefeatAndGetKey,   // 6 적 처치 수 + 열쇠 획득
        SecretDiscovered,  // 7 앞: F 탐색으로 숨겨진 문 발견
        ChestOpened,       // 8 상자 개방
        ReachExit,         // 10 포탈 도달 → 종료(인벤 비우고 1층)
    }

    [Serializable]
    public class Step
    {
        [TextArea] public string hint;
        public Condition condition;
        
        public Collider2D region;

        public int killCount = 1;

        public bool freeLook;

        public bool compactHint;

        public List<GridDoor> openDoors = new();

        public GameObject highlight;

        [Tooltip("이 단계 진입 시 인벤토리 패널을 닫는다(예: 둘러보기 전 시야 확보).")]
        public bool closeInventory;
    }

    [Header("참조")]
    [SerializeField] private TutorialHintUI hintUI;
    [SerializeField] private GridPlayer player;
    [Tooltip("인벤토리 패널 토글(없으면 자동 검색) — closeInventory 단계에서 닫는 데 사용.")]
    [SerializeField] private PrototypeRT.InventoryPanelToggle inventoryToggle;

    [Header("단계")]
    [SerializeField] private List<Step> steps = new();
    [SerializeField] private KeyCode debugSkipKey = KeyCode.None;
    [SerializeField, Min(0f)] private float advanceCooldown = 1f;
    [SerializeField] private int compactHintFromIndex = -1;

    [Header("시작 SafeRoom")]
    [Tooltip("시작 SafeRoom 영역 콜라이더.")]
    [SerializeField] private Collider2D startSafeRegion;
    [Tooltip("카메라")]
    [SerializeField] private CameraFollow cameraFollow;
    [Tooltip("카메라 팬 경계 여백")]
    [SerializeField] private float panBoundsPadding = 0f;
    [Tooltip("타이머 게이트")]
    [SerializeField] private SafeRoomTimerGate safeRoomGate;

    [Header("종료 — 1층 진입")]
    [Tooltip("종료 시 비울 인벤토리(없으면 자동 검색).")]
    [SerializeField] private PrototypeRT.InventoryController inventory;
    [Tooltip("1층 씬 이름")]
    [SerializeField] private string floor1SceneName;

    private int _index = -1;
    private bool _finished;
    private float _stepStartTime;

    private int _kills;
    private bool _keyAcquired;
    private bool _chestOpened;
    private bool _exitReached;
    private bool _secretDiscovered;

    private Step Current => (_index >= 0 && _index < steps.Count) ? steps[_index] : null;

    private void OnEnable()
    {
        GameEvents.OnEnemyKilled += OnEnemyKilled;
        GameEvents.OnKeyAcquired += OnKeyAcquired;
        GameEvents.OnChestOpened += OnChestOpened;
        GameEvents.OnSecretDiscovered += OnSecretDiscovered;
        GameEvents.OnLevelComplete += OnExitReached;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyKilled -= OnEnemyKilled;
        GameEvents.OnKeyAcquired -= OnKeyAcquired;
        GameEvents.OnChestOpened -= OnChestOpened;
        GameEvents.OnSecretDiscovered -= OnSecretDiscovered;
        GameEvents.OnLevelComplete -= OnExitReached;
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<GridPlayer>();
        if (inventory == null) inventory = FindFirstObjectByType<PrototypeRT.InventoryController>();
        if (inventoryToggle == null) inventoryToggle = FindFirstObjectByType<PrototypeRT.InventoryPanelToggle>();
        if (cameraFollow == null) cameraFollow = CameraFollow.Instance;

        foreach (var s in steps)
            if (s.highlight != null) s.highlight.SetActive(false);

        ArmStartSafeRoom();
        ArmCameraBounds();
        AdvanceToNext();
    }

    private void ArmStartSafeRoom()
    {
        if (startSafeRegion == null) return;
        var cells = CellsInRegion(startSafeRegion);
        if (cells.Count == 0)
        {
            Debug.LogWarning("[Tutorial] startSafeRegion 안에 walkable 셀이 없음 — SafeRoom Arm 생략.");
            return;
        }

        if (cameraFollow != null) cameraFollow.ArmSafeRoom(cells);
        if (safeRoomGate != null)
            safeRoomGate.Arm(new RoomData { type = RoomType.Safe, floorCells = cells });
    }

    // 고정 맵의 Floor 타일맵 범위로 카메라 팬 경계 정하기
    private void ArmCameraBounds()
    {
        if (cameraFollow == null || GridSystem.Instance == null) return;
        var floor = GridSystem.Instance.Tilemaps != null ? GridSystem.Instance.Tilemaps.floor : null;
        if (floor == null) return;

        floor.CompressBounds();
        var cb = floor.cellBounds;
        Vector3 wMin = GridSystem.Instance.GridToWorld(new Vector2Int(cb.xMin, cb.yMin));
        Vector3 wMax = GridSystem.Instance.GridToWorld(new Vector2Int(cb.xMax, cb.yMax));
        var pad = new Vector3(panBoundsPadding, panBoundsPadding, 0f);
        wMin -= pad;
        wMax += pad;
        cameraFollow.SetMapBounds(new Bounds(
            (wMin + wMax) * 0.5f,
            new Vector3(Mathf.Abs(wMax.x - wMin.x), Mathf.Abs(wMax.y - wMin.y), 0f)));
    }

    // 콜라이더 영역 안의 walkable 셀 목록
    private static List<Vector2Int> CellsInRegion(Collider2D region)
    {
        var cells = new List<Vector2Int>();
        if (region == null || GridSystem.Instance == null) return cells;

        Bounds b = region.bounds;
        Vector2Int min = GridSystem.Instance.WorldToGrid(b.min);
        Vector2Int max = GridSystem.Instance.WorldToGrid(b.max);
        for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
            {
                var c = new Vector2Int(x, y);
                if (!GridSystem.Instance.IsWalkable(c)) continue;
                if (region.OverlapPoint(GridSystem.Instance.GridToWorld(c)))
                    cells.Add(c);
            }
        return cells;
    }

    private void Update()
    {
        if (_finished || Current == null) return;

        // closeInventory 단계 동안엔 닫힌 상태 유지 — 진행 키(Tab)가 같은 프레임에 다시 여는 충돌 방지.
        if (Current.closeInventory && inventoryToggle != null && inventoryToggle.IsOpen)
            inventoryToggle.SetOpen(false);

        bool forced = debugSkipKey != KeyCode.None && Input.GetKeyDown(debugSkipKey);

        if (!forced && Time.unscaledTime - _stepStartTime < advanceCooldown) return;

        if (!forced && !IsCurrentComplete()) return;

        if (Current.condition == Condition.ReachExit) Finish();
        else AdvanceToNext();
    }

    private bool IsCurrentComplete()
    {
        var s = Current;

        if (s.freeLook) return Input.GetKeyDown(KeyCode.Space);

        switch (s.condition)
        {
            case Condition.PressAdvanceKey:
            case Condition.MapScrollUsed:    // 맵 스크롤 만들면 만들 예정
                return Input.anyKey;
            case Condition.InventoryOpened:
                return Input.GetKeyDown(KeyCode.Tab);

            case Condition.CameraPanned:
                return CameraFollow.Instance != null && CameraFollow.Instance.HasPannedSinceArm;

            case Condition.EnterRegion:
                return PlayerInRegion(s.region);

            case Condition.DefeatAndGetKey:
                return _kills >= s.killCount && _keyAcquired;

            case Condition.SecretDiscovered:
                return _secretDiscovered;

            case Condition.ChestOpened:
                return _chestOpened;

            case Condition.ReachExit:
                return _exitReached;

            default:
                return false;
        }
    }

    private bool PlayerInRegion(Collider2D region)
    {
        if (region == null || player == null) return false;
        Vector3 world = GridSystem.Instance != null
            ? GridSystem.Instance.GridToWorld(player.GridPosition)
            : new Vector3(player.GridPosition.x, player.GridPosition.y, 0f);
        return region.OverlapPoint(world);
    }

    private void AdvanceToNext()
    {
        if (Current != null && Current.highlight != null) Current.highlight.SetActive(false);

        _index++;
        _stepStartTime = Time.unscaledTime;
        ResetStepFlags();

        if (_index >= steps.Count)
        {
            Finish();
            return;
        }

        if (Current.openDoors != null)
            foreach (var door in Current.openDoors)
                if (door != null) door.Open();

        // 이 단계에서 인벤토리 패널 닫기(둘러보기 전 시야 확보 등).
        if (Current.closeInventory && inventoryToggle != null) inventoryToggle.SetOpen(false);

        if (Current.highlight != null) Current.highlight.SetActive(true);

        if (hintUI != null)
        {
            bool compact = Current.freeLook || Current.compactHint
                || (compactHintFromIndex >= 0 && _index >= compactHintFromIndex);
            if (compact) hintUI.ShowPrompt(Current.hint);
            else hintUI.ShowHint(Current.hint);
        }
    }

    private void ResetStepFlags()
    {
        _kills = 0;
        _keyAcquired = false;
        _chestOpened = false;
        _exitReached = false;
        _secretDiscovered = false;
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;

        if (Current != null && Current.highlight != null) Current.highlight.SetActive(false);
        if (hintUI != null) hintUI.HideHint();
        // 인벤 비우기
        ClearInventory();
        KeyInventory.Instance?.ClearAll();
        SceneManager.LoadScene("KDU_2");
    }

    private void ClearInventory()
    {
        if (inventory == null) return;
        
        var copy = new List<PrototypeRT.InventoryItem>(inventory.Items);
        foreach (var item in copy) inventory.TryRemoveItem(item);
    }

    
    private void OnEnemyKilled(Vector2Int pos, float timeBonus) => _kills++;
    private void OnKeyAcquired(KeyType type) => _keyAcquired = true;
    private void OnChestOpened(Vector2Int cell) => _chestOpened = true;

    private void OnSecretDiscovered(Vector2Int cell)
    {
        _secretDiscovered = true;
        // 문이 드러나는 즉시 하이라이트 끄기
        if (Current != null && Current.highlight != null) Current.highlight.SetActive(false);
    }
    private void OnExitReached() => _exitReached = true;
}
