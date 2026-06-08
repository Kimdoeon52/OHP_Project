using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorTransitionController : MonoBehaviour
{
    [Header("필수 의존성")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private RunState runState;
    [SerializeField] private GridPlayer player;

    [Header("페이더")]
    [SerializeField] private ScreenFader fader;
    [SerializeField] private float fadeDuration = 0.4f;
    [Tooltip("검은 화면 유지 시간")]
    [SerializeField] private float blackHoldSeconds = 0.1f;

    [SerializeField] private SafeRoomTimerGate safeRoomGate;
    [SerializeField] private CameraFollow cameraFollow;

    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private int startDelayFrames = 1;

    private bool _transitioning;

    private void OnEnable()  => GameEvents.OnLevelComplete += OnLevelComplete;
    private void OnDisable() => GameEvents.OnLevelComplete -= OnLevelComplete;

    private void Start()
    {
        if (!generateOnStart) 
            return;

        if (!ValidateDeps())
            return;

        _transitioning = true;
        StartCoroutine(InitialFloorCoroutine());
    }

    private void OnLevelComplete()
    {
        if (_transitioning) 
            return;

        if (!ValidateDeps()) 
            return;

        _transitioning = true;
        StartCoroutine(TransitionCoroutine());
    }

    private bool ValidateDeps()
    {
        if (mapGenerator == null || runState == null || player == null)
        {
            Debug.LogError("[FloorTransition] 에러");
            return false;
        }
        return true;
    }

    // 층 전환시 호출
    private IEnumerator TransitionCoroutine()
    {
        if (fader != null) 
            yield return fader.FadeOut(fadeDuration);

        runState.AdvanceDepth();

        yield return BuildFloorCoroutine();

        if (blackHoldSeconds > 0f) 
            yield return new WaitForSecondsRealtime(blackHoldSeconds);

        if (fader != null) 
            yield return fader.FadeIn(fadeDuration);

        _transitioning = false;
    }

    // 초기화 대기
    private IEnumerator InitialFloorCoroutine()
    {
        for (int i = 0; i < startDelayFrames; i++) 
            yield return null;

        yield return BuildFloorCoroutine();

        if (blackHoldSeconds > 0f) 
            yield return new WaitForSecondsRealtime(blackHoldSeconds);

        if (fader != null)
            yield return fader.FadeIn(fadeDuration);

        _transitioning = false;
    }

    private IEnumerator BuildFloorCoroutine()
    {

        // 시스템 리셋
        VisionSystem.Instance?.ResetForNewFloor();
        TimeSystem.Instance?.ResetForNewFloor();

        // 새 층 맵 생성
        var rooms = mapGenerator.Generate(runState.RunSeed, runState.CurrentDepth);
        if (rooms == null)
        {
            Debug.LogError($"[FloorTransition] 맵 생성 실패");
            yield break;
        }

        // 안개 생성 
        FogRenderer fogRenderer = FindFirstObjectByType<FogRenderer>();
        if (fogRenderer != null)
            fogRenderer.RebuildFogForCurrentMap();

        // Safe Room 중앙 셀로 플레이어 텔레포트
        var safeRoom = rooms.Find(r => r.type == RoomType.Safe);
        var spawnRoom = safeRoom;

        var spawnCell = PickCenterCell(spawnRoom.floorCells);

        if (player.Movement != null && player.Movement.enabled)
        {
            player.Movement.enabled = false;
            player.Movement.enabled = true;
        }

        player.SetGridPosition(spawnCell);

        // Safe Room 플래닝
        if (cameraFollow != null)
        {
            cameraFollow.ArmSafeRoom(safeRoom != null ? safeRoom.floorCells : null);
            cameraFollow.SnapToTarget();
        }

        // 시야 갱신
        VisionSystem.Instance?.UpdateVision(spawnCell);

        // 타이머
        if (safeRoomGate != null && safeRoom != null)
            safeRoomGate.Arm(safeRoom);
        else
            TimeSystem.Instance?.StartTimer();
    }

    private static Vector2Int PickCenterCell(List<Vector2Int> cells)
    {
        Vector2 center = Vector2.zero;

        foreach (var c in cells)
            center += c;

        center /= cells.Count;

        Vector2Int best = cells[0];
        float bestDist = float.MaxValue;

        foreach (var c in cells)
        {
            float d = ((Vector2)c - center).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = c; }
        }

        return best;
    }
}

