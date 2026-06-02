using System.Collections;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [Tooltip("Safe Room 안에 있는 동안 타이머 정지 + 첫 이탈 시 시작. 없으면 텔레포트 즉시 타이머 시작.")]
    [SerializeField] private SafeRoomTimerGate safeRoomGate;

    [Header("게임 시작 시 1층 자동 생성")]
    [Tooltip("켜면 Start() 에서 RunState.CurrentDepth 기준 1회 BuildFloor — 페이드 아웃은 스킵하고 fade in 만 수행. " +
             "ScreenFader 의 initialAlpha 를 1 로 두고 시작하면 깜빡임 없이 자연스럽게 진입.")]
    [SerializeField] private bool generateOnStart = true;
    [Tooltip("Start 후 1층 생성 전 대기 프레임. 다른 컴포넌트 Awake/Start 완료 보장용. 보통 1.")]
    [SerializeField] private int startDelayFrames = 1;

    private bool _transitioning;

    private void OnEnable()  => GameEvents.OnLevelComplete += OnLevelComplete;
    private void OnDisable() => GameEvents.OnLevelComplete -= OnLevelComplete;

    private void Start()
    {
        if (!generateOnStart) return;
        if (!ValidateDeps()) return;
        _transitioning = true;
        StartCoroutine(InitialFloorCoroutine());
    }

    private void OnLevelComplete()
    {
        if (_transitioning) return;
        if (!ValidateDeps()) return;
        _transitioning = true;
        StartCoroutine(TransitionCoroutine());
    }

    private bool ValidateDeps()
    {
        if (mapGenerator == null || runState == null || player == null)
        {
            Debug.LogError("[FloorTransition] 의존성 미할당 — MapGenerator/RunState/GridPlayer 인스펙터에서 연결 필요");
            return false;
        }
        return true;
    }

    /// <summary>층 전환: fade out → depth++ → BuildFloor → fade in.</summary>
    private IEnumerator TransitionCoroutine()
    {
        if (fader != null) yield return fader.FadeOut(fadeDuration);
        runState.AdvanceDepth();
        yield return BuildFloorCoroutine();
        if (blackHoldSeconds > 0f) yield return new WaitForSecondsRealtime(blackHoldSeconds);
        if (fader != null) yield return fader.FadeIn(fadeDuration);
        _transitioning = false;
    }

    /// <summary>게임 시작: 다른 컴포넌트 초기화 대기 → BuildFloor → fade in (이미 검은 화면이라 fade out 스킵).</summary>
    private IEnumerator InitialFloorCoroutine()
    {
        for (int i = 0; i < startDelayFrames; i++) yield return null;
        yield return BuildFloorCoroutine();
        if (blackHoldSeconds > 0f) yield return new WaitForSecondsRealtime(blackHoldSeconds);
        if (fader != null) yield return fader.FadeIn(fadeDuration);
        _transitioning = false;
    }

    /// <summary>
    /// 공통 — 단검 정리 → 시스템 리셋 → MapGenerator.Generate → Safe Room 텔레포트 → 시야/타이머 시작.
    /// depth 증가는 호출자가 결정 (TransitionCoroutine 만 AdvanceDepth, InitialFloorCoroutine 은 현재 depth 사용).
    /// </summary>
    private IEnumerator BuildFloorCoroutine()
    {

        // 시스템 리셋 — 시야/탐험 기록, 시간 풀, Feeling 보정치
        VisionSystem.Instance?.ResetForNewFloor();
        TimeSystem.Instance?.ResetForNewFloor();

        // 새 층 맵 생성. builder/painter/feeling 은 MapGenerator 의 풀에서 자동 픽.
        var rooms = mapGenerator.Generate(runState.RunSeed, runState.CurrentDepth);
        if (rooms == null)
        {
            Debug.LogError($"[FloorTransition] depth={runState.CurrentDepth} 맵 생성 실패");
            yield break;
        }

        // 안개 생성 
        FogRenderer fogRenderer = FindFirstObjectByType<FogRenderer>();
        if (fogRenderer != null)
            fogRenderer.RebuildFogForCurrentMap();

        // Safe Room 중앙 셀로 플레이어 텔레포트. 폴백: Safe 없으면 첫 번째 방의 중앙.
        var safeRoom = rooms.Find(r => r.type == RoomType.Safe);
        var spawnRoom = safeRoom;
        if (spawnRoom == null)
        {
            Debug.LogWarning($"[FloorTransition] depth={runState.CurrentDepth} Safe Room 없음 — 첫 방으로 폴백");
            spawnRoom = rooms.Find(r => r.floorCells != null && r.floorCells.Count > 0);
        }
        if (spawnRoom == null || spawnRoom.floorCells == null || spawnRoom.floorCells.Count == 0)
        {
            Debug.LogError($"[FloorTransition] depth={runState.CurrentDepth} 텔레포트 가능한 셀 없음");
            yield break;
        }
        var spawnCell = PickCenterCell(spawnRoom.floorCells);
        player.SetGridPosition(spawnCell);

        // 시야 갱신 (GridPlayer.Start 가 먼저 갱신해뒀어도 ResetForNewFloor 로 덮어쓰여 정확함)
        VisionSystem.Instance?.UpdateVision(spawnCell);

        // 타이머: Safe Room 이 있으면 게이트가 첫 이탈 시 시작, 없으면 즉시 시작.
        if (safeRoomGate != null && safeRoom != null)
            safeRoomGate.Arm(safeRoom);
        else
            TimeSystem.Instance?.StartTimer();
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

