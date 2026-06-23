using UnityEngine;
using TMPro;

// 리더보드 한 줄 — 순위와 점수 표시
public class ScoreEntryRow : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text scoreText;

    // 순위·점수 세팅
    public void Set(int rank, int score)
    {
        if (rankText != null)
            rankText.text = $"{rank}";
        if (scoreText != null)
            scoreText.text = $"{score:N0}";
    }
}
