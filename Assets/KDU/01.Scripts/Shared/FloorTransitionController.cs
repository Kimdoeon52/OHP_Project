using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Exit 도달(OnLevelComplete) → fade out → 다음 층 맵 재생성 → Safe Room 으로 플레이어 텔레포트 →
/// 시야/타이머 리셋 → fade in. 씬 재시작 없이 진행.
///
/// 의존성:
///   - MapGenerator: 새 층 생성
///   - RunState: depth 증가 + 시드 보관
///   - GridPlayer: 텔레포트 대상
///   - ScreenFader: 화면 페이드 (없으면 즉시 전환)
/// 이전 적은 EnemySpawner.Spawn 내부에서, Exit 마커는 MapGenerator.Generate 진입부에서 자체 정리됨.
/// 단검(GridDagger) 만 여기서 정리.
/// </summary>
public class FloorTransitionController : MonoBehaviour
{
    [Header("필수 의존성")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private RunState runState;
    [SerializeField] private GridPlayer player;

    [Header("옵션")]
    [Tooltip("없으면 페이드 없이 즉시 전환.")]
    [SerializeField] private ScreenFader fader;
    [SerializeField] private float fadeDuration = 0.4f;
    [Tooltip("Fade out 끝나고 fade in 시작 전 검은 화면 유지 시간. 맵 생성 체감 안정용.")]
    [SerializeField] private float blackHoldSeconds = 0.1f;

    private bool _transitioning;

    private void OnEnable()  => GameEvents.OnLevelComplete += OnLevelComplete;
    private void OnDisable() => GameEvents.OnLevelComplete -= OnLevelComplete;

    private void OnLevelComplete()
    {
        if (_transitioning) return;
        if (mapGenerator == null || runState == null || player == null)
        {
            Debug.LogError("[FloorTransition] 의존성 미할당 — MapGenerator/RunState/GridPlayer 인스펙터에서 연결 필요");
            return;
        }
        _transitioning = true;
        StartCoroutine(TransitionCoroutine());
    }

    private IEnumerator TransitionCoroutine()
    {
        // 0. fade out
        if (fader != null) yield return fader.FadeOut(fadeDuration);

        // 1. depth ++
        runState.AdvanceDepth();

        // 2. 단검 정리 — 씬에 떨어진 모든 GridDagger 파괴.
        // (적은 EnemySpawner.Spawn 가 ClearPreviousSpawns 호출, Exit 은 MapGenerator 가 자체 정리)
        foreach (var d in FindObjectsByType<GridDagger>(FindObjectsSortMode.None))
            Destroy(d.gameObject);

        // 3. 시스템 리셋 — 시야/탐험 기록, 시간 풀, Feeling 보정치 (Generate 안에서도 VisionRadiusDelta 리셋되긴 함)
        VisionSystem.Instance?.ResetForNewFloor();
        TimeSystem.Instance?.ResetForNewFloor();

        // 4. 새 층 맵 생성. builder/painter/feeling 은 MapGenerator 의 풀에서 자동 픽.
        var rooms = mapGenerator.Generate(runState.RunSeed, runState.CurrentDepth);
        if (rooms == null)
        {
            Debug.LogError($"[FloorTransition] depth={runState.CurrentDepth} 맵 생성 실패");
            _transitioning = false;
            yield break;
        }

        // 5. Safe Room 중앙 셀로 플레이어 텔레포트. 보스 층은 BossRoomLoader 가 Safe 를 어떻게 두는지에
        //    따라 다를 수 있어 폴백: Safe 없으면 첫 번째 방의 중앙.
        var spawnRoom = rooms.Find(r => r.type == RoomType.Safe);
        if (spawnRoom == null)
        {
            Debug.LogWarning($"[FloorTransition] depth={runState.CurrentDepth} Safe Room 없음 — 첫 방으로 폴백");
            spawnRoom = rooms.Find(r => r.floorCells != null && r.floorCells.Count > 0);
        }
        if (spawnRoom == null || spawnRoom.floorCells == null || spawnRoom.floorCells.Count == 0)
        {
            Debug.LogError($"[FloorTransition] depth={runState.CurrentDepth} 텔레포트 가능한 셀 없음");
            _transitioning = false;
            yield break;
        }
        var spawnCell = PickCenterCell(spawnRoom.floorCells);
        player.SetGridPosition(spawnCell);

        // 6. 시야 갱신 + 타이머 재시작
        VisionSystem.Instance?.UpdateVision(spawnCell);
        TimeSystem.Instance?.StartTimer();

        // 7. fade in
        if (blackHoldSeconds > 0f) yield return new WaitForSecondsRealtime(blackHoldSeconds);
        if (fader != null) yield return fader.FadeIn(fadeDuration);

        _transitioning = false;
    }

    private static Vector2Int PickCenterCell(List<Vector2Int> cells)
    {
        Vector2 center = Vector2.zero;
        foreach (var c in cells) center += c;
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
