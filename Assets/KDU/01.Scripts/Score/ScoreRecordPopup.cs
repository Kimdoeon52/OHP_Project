using UnityEngine;
using UnityEngine.UI;

// 점수 기록 팝업 — 저장된 톱30 점수를 순위순으로 표시
// 이 스크립트가 붙은 오브젝트 자체를 켜고 끔. 켜지는 순간 목록을 채움.
public class ScoreRecordPopup : MonoBehaviour
{
    private const int MaxRows = 30;

    public bool IsOpen => gameObject.activeSelf;

    [Header("리스트")]
    [SerializeField] private Transform content;
    [SerializeField] private ScoreEntryRow rowPrefab;

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
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
        }

        int count = Mathf.Min(MaxRows, data.entries.Count);
        if (content != null && rowPrefab != null)
        {
            for (int i = 0; i < count; i++)
            {
                ScoreEntryRow row = Instantiate(rowPrefab, content);
                row.Set(i + 1, data.entries[i], OnRowSelected);
            }
        }
    }

    // 행 클릭 → 상세 팝업
    private void OnRowSelected(ScoreEntry entry)
    {
        if (detailPopup != null)
            detailPopup.Open(entry);
    }
}
