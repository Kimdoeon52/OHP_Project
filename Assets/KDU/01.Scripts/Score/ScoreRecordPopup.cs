using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 점수 기록 팝업 — 저장된 톱30 점수를 순위순으로 표시, 각 기록의 세계 순위(Steam 전체 Top 100 기준) 병기
// 이 스크립트가 붙은 오브젝트 자체를 켜고 끔. 켜지는 순간 목록을 채움.
public class ScoreRecordPopup : MonoBehaviour
{
    private const int MaxRows = 30;

    // 이름·세계 순위 없음 표기
    private const string NoValue = "-";

    private readonly List<ScoreEntryRow> _rows = new List<ScoreEntryRow>();
    private readonly List<ScoreEntry> _entries = new List<ScoreEntry>();
    private int _request;

    public bool IsOpen => gameObject.activeSelf;

    [Header("리스트")]
    [SerializeField] private Transform content;
    [SerializeField] private ScoreEntryRow rowPrefab;

    [Tooltip("맨 위 제목 행 프리팹")]
    [SerializeField] private GameObject headerPrefab;

    [Header("상세")]
    [SerializeField] private ScoreDetailPopup detailPopup;

    [Header("버튼")]
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    // 켜질 때마다 최신 기록으로 다시 채움
    private void OnEnable()
    {
        if (detailPopup != null)
            detailPopup.Close();

        Populate();
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (detailPopup != null)
            detailPopup.Close();

        gameObject.SetActive(false);
    }

    // 저장된 점수 로드 → 행 재생성
    private void Populate()
    {
        ScoreboardData data = Scoreboard.Load();

        if (content != null)
        {
            content.DestroyChildren();

            // 제목 행은 목록을 지울 때 같이 사라지므로 매번 맨 위에 다시 만든다
            if (headerPrefab != null)
                Instantiate(headerPrefab, content);
        }

        _rows.Clear();
        _entries.Clear();

        int count = Mathf.Min(MaxRows, data.entries.Count);
        if (content != null && rowPrefab != null)
        {
            for (int i = 0; i < count; i++)
            {
                ScoreEntryRow row = Instantiate(rowPrefab, content);
                row.Set(i + 1, data.entries[i], OnRowSelected);
                _rows.Add(row);
                _entries.Add(data.entries[i]);
            }
        }

        RequestWorldRanks();
    }

    // 이름·세계 순위 채우기 — Steam 전체 Top 100 기준
    private void RequestWorldRanks()
    {
        for (int i = 0; i < _rows.Count; i++)
            _rows[i].SetWorld(NameOf(_entries[i]), NoValue);

        if (!SteamBootstrap.IsReady || SteamLeaderboard.Instance == null || _rows.Count == 0)
            return;

        int request = ++_request;
        SteamLeaderboard.Instance.Download(SteamLeaderboard.GlobalBoardName, SteamLeaderboard.Scope.Top, 0, top => ApplyWorldRanks(request, top));
    }

    // 기록에 저장된 이름 — 구버전 기록이면 현재 이름
    private static string NameOf(ScoreEntry entry)
    {
        return string.IsNullOrEmpty(entry.playerName) ? PlayerProfile.DisplayName : entry.playerName;
    }

    private void ApplyWorldRanks(int request, List<SteamLeaderboard.Entry> top)
    {
        // 창이 꺼졌거나 다시 채워진 뒤 도착한 응답은 버림
        if (this == null || !isActiveAndEnabled || request != _request || top == null)
            return;

        for (int i = 0; i < _rows.Count; i++)
        {
            ScoreEntry entry = _entries[i];
            string worldRank = entry.daily ? NoValue : RankFormat.Ordinal(SteamLeaderboard.RankForScore(top, entry.finalScore), SteamLeaderboard.MaxRank);
            _rows[i].SetWorld(NameOf(entry), worldRank);
        }
    }

    // 행 클릭 → 상세 팝업
    private void OnRowSelected(ScoreEntry entry)
    {
        if (detailPopup != null)
            detailPopup.Open(entry, NameOf(entry));
    }
}
