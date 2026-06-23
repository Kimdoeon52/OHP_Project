using UnityEngine;
using UnityEngine.UI;

// 점수 기록 팝업 — 저장된 톱30 점수를 순위순으로 표시
public class ScoreRecordPopup : MonoBehaviour
{
    private const int MaxRows = 30;

    public bool IsOpen => panel != null && panel.activeSelf;

    [Header("패널")]
    [SerializeField] private GameObject panel;

    [Header("리스트")]
    [SerializeField] private Transform content;
    [SerializeField] private ScoreEntryRow rowPrefab;

    [Header("버튼")]
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (panel != null)
            panel.SetActive(false);
    }

    // 저장된 점수 로드 → 행 채우기 → 열기
    public void Open()
    {
        ScoreboardData data = Scoreboard.Load();

        if (content != null)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
        }

        int count = Mathf.Min(MaxRows, data.entries.Count);
        if (content != null && rowPrefab != null)
        {
            for (int i = 0; i < count; i++)
            {
                ScoreEntryRow row = Instantiate(rowPrefab, content);
                row.Set(i + 1, data.entries[i].finalScore);
            }
        }

        if (panel != null)
            panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
