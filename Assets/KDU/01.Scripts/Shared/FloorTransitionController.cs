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

    [Tooltip("최종 층 (출품 범위 15층). 이 층 Exit = 승리")]
    [SerializeField] private int finalDepth = 15;

    private bool _transitioning;

    // 세이브가 있으면 로드해 둔 스냅샷. 초기 맵 빌드 후 ApplyState로 소비.
    private RunSnapshot _pendingLoad;

    private void OnEnable()  => GameEvents.OnLevelComplete += OnLevelComplete;
    private void OnDisable() => GameEvents.OnLevelComplete -= OnLevelComplete;

    private void Start()
    {
        if (!generateOnStart)
            return;

        if (!ValidateDeps())
            return;

        BeginRun();
    }

    // 런 시작 — 부팅 시 호출. 이어하기/새 게임을 세이브 유무로 분기.
    public void BeginRun()
    {
        if (_transitioning)
            return;

        if (!ValidateDeps())
            return;

        if (SaveManager.HasSave())
        {
            // 이어하기 — 시드/층/챌린지를 먼저 복원해 같은 맵을 빌드.
            _pendingLoad = SaveManager.Load();
            RunRestorer.ApplySeedDepth(_pendingLoad);
        }
        else
        {
            // 새 게임 — 타이틀에서 고른 챌린지/시드를 적용 후 고정.
            ApplyPendingChallenges();
        }

        _transitioning = true;
        StartCoroutine(InitialFloorCoroutine());
    }

    // 타이틀 씬에서 넘어온 챌린지 선택과 시드를 적용하고 고정한다.
    private void ApplyPendingChallenges()
    {
        ChallengeState cs = ChallengeState.Ensure();
        if (cs != null)
        {
            cs.Configure(ChallengeSelection.PendingTypes);
            cs.Lock();
        }

        if (ChallengeSelection.HasSeed)
            runState.ResetRun(ChallengeSelection.Seed, 1);

        ChallengeSelection.Clear();
    }

    private void OnLevelComplete()
    {
        if (_transitioning)
            return;

        if (!ValidateDeps())
            return;

        // 최종층 Exit = 승리. 다음 층 없음.
        if (runState.CurrentDepth >= finalDepth)
        {
            OnRunCleared();
            return;
        }

        _transitioning = true;
        StartCoroutine(TransitionCoroutine());
    }

    // 최종층 클리어 처리. 영구사망과 같이 런 종료이므로 세이브 삭제.
    private void OnRunCleared()
    {
        SaveManager.Delete();
        GameEvents.OnRunCleared?.Invoke();
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

        // 새 층 진입 = 유일한 저장 시점. 이 층에서 줏은 건 아직 저장 안 됨.
        SaveManager.Save();

        if (blackHoldSeconds > 0f)
            yield return new WaitForSecondsRealtime(blackHoldSeconds);

        if (fader != null)
            yield return fader.FadeIn(fadeDuration);

        _transitioning = false;
    }

    // 초기화 대기
    private IEnumerator InitialFloorCoroutine()
    {
        // 빌드/스폰/카메라 스냅이 보이지 않게 부팅부터 검정 유지
        if (fader != null)
            fader.SetBlack();

        for (int i = 0; i < startDelayFrames; i++)
            yield return null;

        yield return BuildFloorCoroutine();

        if (_pendingLoad != null)
        {
            // 로드 부팅 — 장비 초기화(Start) 이후이므로 여기서 플레이어/인벤/장비 복원.
            RunRestorer.ApplyState(_pendingLoad);
            _pendingLoad = null;
        }
        else
        {
            // 새 런 — 직전 런 종료 시점에서 넘어온 골드를 받고 저장에 반영.
            Wallet.Instance?.ConsumeCarryOver();

            // 새 런 1층 진입도 저장 — 1층에서 나가면 1층부터 다시 시작.
            SaveManager.Save();
        }

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

        // 맵 생성이 성공한 뒤에 BGM을 바꿉니다.
        // 실패한 층에서 음악만 먼저 바뀌면 화면/게임 상태와 소리가 서로 다른 층을 가리키는 문제가 생길 수 있습니다.
        // 보스 BGM은 DialogManager가 보스 이름 연출 이후 DialogData에서 재생하므로, 층 입장 때는 전투 전 대기 BGM을 재생합니다.
        if (mapGenerator.IsBossDepth(runState.CurrentDepth))
            AudioManager.Instance?.PlayBossPreludeBgmForDepth(runState.CurrentDepth);
        else
            AudioManager.Instance?.PlayBgmForDepth(runState.CurrentDepth, false);

        bool isBossFloor = mapGenerator.IsBossDepth(runState.CurrentDepth);

        // 안개 생성 — 보스 층은 안개 없음
        FogRenderer fogRenderer = FindFirstObjectByType<FogRenderer>();
        if (fogRenderer != null)
        {
            if (isBossFloor)
                fogRenderer.SetFogEnabled(false);
            else
            {
                fogRenderer.SetFogEnabled(true);
                fogRenderer.RebuildFogForCurrentMap();
            }
        }

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

        // Safe Room 플래닝 — 보스 층은 플래닝 모드 비활성
        if (cameraFollow != null)
        {
            cameraFollow.ArmSafeRoom(isBossFloor ? null : (safeRoom != null ? safeRoom.floorCells : null));
            cameraFollow.SnapToTarget();
        }

        // 시야 갱신
        VisionSystem.Instance?.UpdateVision(spawnCell);

        // 타이머
        if (isBossFloor)
        {
            // 보스층은 게이트 무장 해제 — 이전 층 게이트가 타이머를 재시작하지 않게.
            // 연출 끝(BossBase.OnEnable)에서 타이머 시작. 진입~걷기~대사~연출 동안 정지.
            // 5/10층은 보스 제한시간 표시, 15층은 무한 연출이 처리.
            safeRoomGate?.Disarm();
            if (runState.CurrentDepth != finalDepth)
                TimeSystem.Instance?.SetBossTime();
            else
                TimeSystem.Instance?.StopTimer();
        }
        else if (safeRoomGate != null && safeRoom != null)
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

