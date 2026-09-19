using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Steam 랭킹 목록 — 일일(오늘 KST)/전체 보드. 켜질 때마다 갱신 (페이지 자체 또는 페이지 안 영역에 부착)
public class LeaderboardPanel : MonoBehaviour
{
    [Tooltip("보드 종류")]
    [SerializeField] private SteamLeaderboard.Board board = SteamLeaderboard.Board.Global;

    [Tooltip("조회 범위 (Top=1~100위, AroundMe=내 순위)")]
    [SerializeField] private SteamLeaderboard.Scope scope = SteamLeaderboard.Scope.Top;

    [Tooltip("AroundMe 앞뒤 인원 (0=나만)")]
    [SerializeField, Min(0)] private int aroundRange = SteamLeaderboard.DefaultAroundRange;

    [Tooltip("행 부모")]
    [SerializeField] private Transform rowParent;

    [Tooltip("행 프리팹")]
    [SerializeField] private RankRow rowPrefab;

    [Tooltip("로딩·미연결·기록 없음 안내 문구")]
    [SerializeField] private TMP_Text statusText;

    [Tooltip("행 클릭 시 상세 팝업")]
    [SerializeField] private ScoreDetailPopup detailPopup;

    [Header("일일 전용 (전체 보드면 비워둠)")]
    [Tooltip("날짜")]
    [SerializeField] private TMP_Text dateText;

    [Tooltip("시작 버튼")]
    [SerializeField] private Button startButton;

    [Tooltip("씬 이동")]
    [SerializeField] private MoveScene moveScene;

    private readonly List<RankRow> _rows = new List<RankRow>();
    private Callback<PersonaStateChange_t> _personaChanged;
    private string _boardName;
    private float _nextTick;

    private void Awake()
    {
        if (startButton != null && moveScene != null)
            startButton.onClick.AddListener(moveScene.StartDailyChallenge);
    }

    private void OnEnable()
    {
        if (detailPopup != null)
            detailPopup.Close();

        if (SteamBootstrap.IsReady)
            _personaChanged = Callback<PersonaStateChange_t>.Create(_ => RefreshNames());

        _nextTick = 0f;
        Refresh();
    }

    private void OnDisable()
    {
        if (detailPopup != null)
            detailPopup.Close();

        _personaChanged?.Dispose();
        _personaChanged = null;
    }

    // 보드 다시 조회
    private void Refresh()
    {
        ClearRows();

        bool online = SteamLeaderboard.Instance != null && TryResolveBoard(out _boardName);
        if (startButton != null)
            startButton.interactable = online;

        if (!online)
        {
            SetStatus(LocalizationManager.GetOrDefault("leaderboard.offline", "Steam에 연결되어 있지 않습니다"));
            return;
        }

        SetStatus(LocalizationManager.GetOrDefault("leaderboard.loading", "불러오는 중..."));

        string requested = _boardName;
        SteamLeaderboard.Instance.Download(_boardName, scope, aroundRange, entries => OnDownloaded(requested, entries));
    }

    // 보드 이름 결정 — 일일은 오늘(KST) 날짜 보드
    private bool TryResolveBoard(out string boardName)
    {
        boardName = null;
        if (!SteamBootstrap.IsReady)
            return false;

        if (board == SteamLeaderboard.Board.Global)
        {
            boardName = SteamLeaderboard.GlobalBoardName;
            return true;
        }

        if (!DailyChallenge.TryGetTodayKey(out string key))
            return false;

        if (dateText != null)
            dateText.text = DailyChallenge.DisplayDate(key);

        boardName = DailyChallenge.BoardName(key);
        return true;
    }

    private void OnDownloaded(string requested, List<SteamLeaderboard.Entry> entries)
    {
        // 페이지가 꺼졌거나 보드가 바뀐 뒤 도착한 응답은 버림
        if (this == null || !isActiveAndEnabled || requested != _boardName)
            return;

        ClearRows();
        if (entries == null)
        {
            SetStatus(LocalizationManager.GetOrDefault("leaderboard.failed", "랭킹을 불러오지 못했습니다"));
            return;
        }

        foreach (SteamLeaderboard.Entry e in entries)
        {
            RankRow row = Instantiate(rowPrefab, rowParent);
            row.Set(e, OnRowSelected);
            _rows.Add(row);
        }

        SetStatus(entries.Count == 0 ? LocalizationManager.GetOrDefault("leaderboard.empty", "아직 기록이 없습니다") : "");
    }

    // 행 클릭 → 해당 유저 기록 상세
    private void OnRowSelected(SteamLeaderboard.Entry entry)
    {
        if (detailPopup != null && entry.detail != null)
            detailPopup.Open(entry.detail, SteamFriends.GetFriendPersonaName(entry.user));
    }

    // 일일 보드 — KST 자정 지나면 새 보드로 전환
    private void Update()
    {
        if (board != SteamLeaderboard.Board.Daily || Time.unscaledTime < _nextTick)
            return;

        _nextTick = Time.unscaledTime + 1f;
        if (DailyChallenge.TryGetTodayKey(out string today) && DailyChallenge.BoardName(today) != _boardName)
            Refresh();
    }

    private void RefreshNames()
    {
        foreach (RankRow row in _rows)
            row.RefreshName();
    }

    // 씬에 배치해 둔 미리보기 행까지 모두 제거
    private void ClearRows()
    {
        if (rowParent != null)
            rowParent.DestroyChildren();

        _rows.Clear();
    }

    private void SetStatus(string message)
    {
        if (statusText == null)
            return;

        statusText.text = message;
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }
}
