using TMPro;
using UnityEngine;

// 현재 층 수와 진행 중 점수 HUD
public class DepthScoreHud : MonoBehaviour
{
    [Tooltip("층 표시 텍스트")]
    [SerializeField] private TMP_Text depthText;
    [Tooltip("점수 표시 텍스트")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("층 포맷")]
    [SerializeField] private string depthFormat = "{0}F";
    [Tooltip("점수 포맷")]
    [SerializeField] private string scoreFormat = "{0}";

    private void OnEnable()
    {
        GameEvents.OnDepthChanged += RefreshDepth;
        GameEvents.OnScoreChanged += RefreshScore;

        if (RunState.Instance != null)
            RefreshDepth(RunState.Instance.CurrentDepth);
        if (ScoreSystem.Instance != null)
            RefreshScore(ScoreSystem.Instance.CurrentScore);
    }

    private void OnDisable()
    {
        GameEvents.OnDepthChanged -= RefreshDepth;
        GameEvents.OnScoreChanged -= RefreshScore;
    }

    private void RefreshDepth(int depth)
    {
        if (depthText != null)
            depthText.text = string.Format(depthFormat, depth);
    }

    private void RefreshScore(int score)
    {
        if (scoreText != null)
            scoreText.text = string.Format(scoreFormat, score);
    }
}
