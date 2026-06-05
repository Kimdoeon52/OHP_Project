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
/// 팀원 이벤트 훅이 붙기 전까지 advanceKey 수동 진행으로 골격이 끝까지 돌아가게 둔다.
/// 콘텐츠(적 배치 등)가 아직 없을 때 전체 흐름을 점검하려면 debugSkipKey 로 강제 진행.
/// </summary>
public class TutorialController : MonoBehaviour
{
    public enum Condition
    {
        PressAdvanceKey,   // 1 시작
        MapScrollUsed,     // 2 (B) TODO: 지도 스크롤 사용 이벤트 — 현재 임시 수동(advanceKey)
        CameraPanned,      // 3 SafeRoom 카메라 줌/팬
        InventoryOpened,   // 4 (B) TODO: 인벤토리 열림 이벤트(NYH) — 현재 임시 수동(advanceKey)
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

        [Tooltip("EnterRegion 조건용 — 이 콜라이더 영역 안에 플레이어가 들어오면 완료.")]
        public Collider2D region;

        [Tooltip("DefeatAndGetKey 조건용 — 처치해야 할 적 수.")]
        public int killCount = 1;
    }

    [Header("참조")]
    [SerializeField] private TutorialHintUI hintUI;
    [SerializeField] private GridPlayer player;

    [Header("단계")]
    [SerializeField] private List<Step> steps = new();
    [Tooltip("시작/임시 수동 진행에 쓰는 키.")]
    [SerializeField] private KeyCode advanceKey = KeyCode.Space;
    [Tooltip("디버그용 — 조건과 무관하게 현재 단계를 강제로 넘김. None 이면 비활성.")]
    [SerializeField] private KeyCode debugSkipKey = KeyCode.None;

    [Header("시작 SafeRoom Arm — 플래닝 카메라(줌/팬) + 선택적 타이머 정지")]
    [Tooltip("시작 SafeRoom 영역 콜라이더. 이 안의 walkable 셀을 SafeRoom 으로 Arm. 플레이어는 이 안에서 시작해야 함.")]
    [SerializeField] private Collider2D startSafeRegion;
    [Tooltip("플래닝 카메라(없으면 CameraFollow.Instance).")]
    [SerializeField] private CameraFollow cameraFollow;
    [Tooltip("타이머 게이트(선택) — 있으면 시작방 안 타이머 정지, 첫 이탈 시 시작.")]
    [SerializeField] private SafeRoomTimerGate safeRoomGate;

    [Header("종료 — 1층 진입")]
    [Tooltip("종료 시 비울 인벤토리(없으면 자동 검색).")]
    [SerializeField] private PrototypeRT.InventoryController inventory;
    [Tooltip("종료 후 로드할 1층(메인 게임) 씬 이름. 비우면 씬 전환 생략(로그만).")]
    [SerializeField] private string floor1SceneName;

    private int _index = -1;
    private bool _finished;

    // 이벤트 기반 진행 플래그 — 단계 진입 시 리셋.
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
        if (cameraFollow == null) cameraFollow = CameraFollow.Instance;

        ArmStartSafeRoom();
        AdvanceToNext();
    }

    /// <summary>시작 SafeRoom 영역을 Arm — 플래닝 카메라 활성(단계 3 판정) + 선택적 타이머 정지.</summary>
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

    /// <summary>콜라이더 영역 안의 walkable 셀 목록 — startSafeRegion / EnterRegion 영역 산출용.</summary>
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

        bool forced = debugSkipKey != KeyCode.None && Input.GetKeyDown(debugSkipKey);
        if (!forced && !IsCurrentComplete()) return;

        if (Current.condition == Condition.ReachExit) Finish();
        else AdvanceToNext();
    }

    private bool IsCurrentComplete()
    {
        var s = Current;
        switch (s.condition)
        {
            case Condition.PressAdvanceKey:
            case Condition.MapScrollUsed:    // B: TODO 전용 이벤트로 교체. 임시 수동.
            case Condition.InventoryOpened:  // B: TODO 전용 이벤트로 교체. 임시 수동.
                return Input.GetKeyDown(advanceKey);

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
        _index++;
        ResetStepFlags();

        if (_index >= steps.Count)
        {
            Finish();   // steps 가 ReachExit 없이 끝나도 안전 종료.
            return;
        }

        if (hintUI != null) hintUI.ShowHint(Current.hint);
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

        if (hintUI != null) hintUI.HideHint();

        // 튜토리얼 보상 제거 — 본 게임은 빈손 시작 (TUTORIAL_PLAN 제약).
        ClearInventory();
        KeyInventory.Instance?.ClearAll();

        if (!string.IsNullOrEmpty(floor1SceneName))
            SceneManager.LoadScene(floor1SceneName);
        else
            Debug.Log("[Tutorial] 완료 — floor1SceneName 미설정. 1층 씬 로드 생략(테스트).");
    }

    private void ClearInventory()
    {
        if (inventory == null) return;
        // public API 만 사용 — NYH 인벤토리 내부 수정 없음.
        var copy = new List<PrototypeRT.InventoryItem>(inventory.Items);
        foreach (var item in copy) inventory.TryRemoveItem(item);
    }

    // ───── 이벤트 핸들러 (현재 단계 플래그만 갱신) ─────
    private void OnEnemyKilled(Vector2Int pos, float timeBonus) => _kills++;
    private void OnKeyAcquired(KeyType type) => _keyAcquired = true;
    private void OnChestOpened(Vector2Int cell) => _chestOpened = true;
    private void OnSecretDiscovered(Vector2Int cell) => _secretDiscovered = true;
    private void OnExitReached() => _exitReached = true;
}
