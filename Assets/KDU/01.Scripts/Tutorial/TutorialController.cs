using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 튜토리얼 진행용 스크립트
public class TutorialController : MonoBehaviour
{
    public enum Condition
    {
        PressAdvanceKey,   // 시작
        MapScrollUsed,     // 지도 스크롤 사용 이벤트
        CameraPanned,      // SafeRoom 카메라 줌/팬
        InventoryOpened,   // 인벤토리 열림 이벤트
        EnterRegion,       // 영역 진입
        DefeatAndGetKey,   // 적 처치 수 + 열쇠 획득
        SecretDiscovered,  // 앞: F 탐색으로 숨겨진 문 발견
        ChestOpened,       // 상자 개방
        ReachExit,         // 포탈 도달
    }

    [Serializable]
    public class Step
    {
        [TextArea] public string hint; // 안내 텍스트
        public Condition condition; // 완료 조건
        
        public Collider2D region; // 방 도착 체크용

        public int killCount = 1; // 적 처치 수

        public bool freeLook; // 자유 시점 모드

        public bool compactHint; // 하단 프롬프트로 간단하게 보여줌

        public List<GridDoor> openDoors = new(); // 문 개방

        public GameObject highlight; // 숨겨진 방 강조

        public bool closeInventory; // 인벤토리 패널 닫기

        public bool lockPlayerInput; // 플레이어 입력 차단
    }

    [SerializeField] private TutorialHintUI hintUI;
    [SerializeField] private GridPlayer player;
    [SerializeField] private PrototypeRT.InventoryPanelToggle inventoryToggle;
    [SerializeField] private GridPlayerInput playerInput;

    [SerializeField] private List<Step> steps = new();
    [SerializeField] private KeyCode debugSkipKey = KeyCode.None;
    [SerializeField, Min(0f)] private float advanceCooldown = 1f;
    [SerializeField] private int compactHintFromIndex = -1;

    [SerializeField] private Collider2D startSafeRegion;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private float panBoundsPadding = 0f;
    [SerializeField] private SafeRoomTimerGate safeRoomGate;

    [SerializeField] private PrototypeRT.InventoryController inventory;

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
        if (player == null)
            player = FindFirstObjectByType<GridPlayer>();

        if (inventory == null) 
            inventory = FindFirstObjectByType<PrototypeRT.InventoryController>();

        if (inventoryToggle == null) 
            inventoryToggle = FindFirstObjectByType<PrototypeRT.InventoryPanelToggle>();

        if (playerInput == null)
            playerInput = FindFirstObjectByType<GridPlayerInput>();

        if (cameraFollow == null) cameraFollow = CameraFollow.Instance;

        foreach (var s in steps)
            if (s.highlight != null) 
                s.highlight.SetActive(false);

        ArmStartSafeRoom();
        ArmCameraBounds();
        AdvanceToNext();
    }

    private void ArmStartSafeRoom()
    {
        if (startSafeRegion == null)
            return;

        var cells = CellsInRegion(startSafeRegion);
        if (cells.Count == 0)
        {
            Debug.LogWarning("[Tutorial] startSafeRegion 안에 walkable 셀이 없음 — SafeRoom Arm 생략.");
            return;
        }

        if (cameraFollow != null)
            cameraFollow.ArmSafeRoom(cells);

        if (safeRoomGate != null)
            safeRoomGate.Arm(new RoomData { type = RoomType.Safe, floorCells = cells });
    }

    private void ArmCameraBounds()
    {
        if (cameraFollow == null || GridSystem.Instance == null) 
            return;

        var floor = GridSystem.Instance.Tilemaps != null ? GridSystem.Instance.Tilemaps.floor : null;
        if (floor == null) 
            return;

        floor.CompressBounds();
        var cb = floor.cellBounds;
        Vector3 wMin = GridSystem.Instance.GridToWorld(new Vector2Int(cb.xMin, cb.yMin));
        Vector3 wMax = GridSystem.Instance.GridToWorld(new Vector2Int(cb.xMax, cb.yMax));
        var pad = new Vector3(panBoundsPadding, panBoundsPadding, 0f);
        wMin -= pad;
        wMax += pad;
        cameraFollow.SetMapBounds(new Bounds( (wMin + wMax) * 0.5f, new Vector3(Mathf.Abs(wMax.x - wMin.x), Mathf.Abs(wMax.y - wMin.y), 0f)));
    }

    // 콜라이더 영역 안의 walkable 셀 목록
    private static List<Vector2Int> CellsInRegion(Collider2D region)
    {
        var cells = new List<Vector2Int>();
        if (region == null || GridSystem.Instance == null)
            return cells;

        Bounds b = region.bounds;
        Vector2Int min = GridSystem.Instance.WorldToGrid(b.min);
        Vector2Int max = GridSystem.Instance.WorldToGrid(b.max);
        for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
            {
                var c = new Vector2Int(x, y);
                if (!GridSystem.Instance.IsWalkable(c)) 
                    continue;

                if (region.OverlapPoint(GridSystem.Instance.GridToWorld(c)))
                    cells.Add(c);
            }
        return cells;
    }

    private void Update()
    {
        if (_finished || Current == null) 
            return;

        if (Current.closeInventory && inventoryToggle != null && inventoryToggle.IsOpen)
            inventoryToggle.SetOpen(false);

        bool forced = debugSkipKey != KeyCode.None && Input.GetKeyDown(debugSkipKey);

        if (!forced && Time.unscaledTime - _stepStartTime < advanceCooldown)
            return;

        if (!forced && !IsCurrentComplete())
            return;

        if (Current.condition == Condition.ReachExit) 
            Finish();

        else AdvanceToNext();
    }

    private bool IsCurrentComplete()
    {
        var s = Current;

        if (s.freeLook) return Input.GetKeyDown(KeyCode.Space);

        switch (s.condition)
        {
            case Condition.PressAdvanceKey:
            case Condition.MapScrollUsed:    // 맵 스크롤 사용시 호출
                return Input.anyKeyDown;
            case Condition.InventoryOpened:
                return inventoryToggle != null && inventoryToggle.IsOpen;

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
        if (region == null || player == null) 
            return false;

        Vector3 world = GridSystem.Instance != null ? GridSystem.Instance.GridToWorld(player.GridPosition) : new Vector3(player.GridPosition.x, player.GridPosition.y, 0f);

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
                if (door != null) 
                    door.Open();

        if (Current.closeInventory && inventoryToggle != null)
            inventoryToggle.SetOpen(false);

        SetPlayerInputLocked(Current.lockPlayerInput);

        if (Current.highlight != null) 
            Current.highlight.SetActive(true);

        if (hintUI != null)
        {
            bool compact = Current.freeLook || Current.compactHint || (compactHintFromIndex >= 0 && _index >= compactHintFromIndex);
            if (compact) 
                hintUI.ShowPrompt(Current.hint);
            else 
                hintUI.ShowHint(Current.hint);
        }
    }

    // 플레이어 입력 차단
    private void SetPlayerInputLocked(bool locked)
    {
        if (playerInput != null)
            playerInput.enabled = !locked;

        if (inventoryToggle != null) 
            inventoryToggle.enabled = !locked;
    }

    private void ResetStepFlags()
    {
        _kills = 0;
        _keyAcquired = false;
        _chestOpened = false;
        _exitReached = false;
        _secretDiscovered = false;
    }

    // 튜토리얼 스킵
    public void Skip() => Finish();

    private void Finish()
    {
        if (_finished)
            return;

        _finished = true;

        if (Current != null && Current.highlight != null)
            Current.highlight.SetActive(false);

        if (hintUI != null) 
            hintUI.HideHint();

        SetPlayerInputLocked(false);
        // 인벤 초기화
        ClearInventory();
        KeyInventory.Instance?.ClearAll();
        SceneManager.LoadScene(1); // 다음 씬
    }

    private void ClearInventory()
    {
        if (inventory == null) 
            return;
        
        var copy = new List<PrototypeRT.InventoryItem>(inventory.Items);
        foreach (var item in copy) 
            inventory.TryRemoveItem(item);
    }

    
    private void OnEnemyKilled(Vector2Int pos, float timeBonus) => _kills++;
    private void OnKeyAcquired(KeyType type) => _keyAcquired = true;
    private void OnChestOpened(Vector2Int cell) => _chestOpened = true;

    private void OnSecretDiscovered(Vector2Int cell)
    {
        _secretDiscovered = true;
        if (Current != null && Current.highlight != null) 
            Current.highlight.SetActive(false);
    }
    private void OnExitReached() => _exitReached = true;
}
