using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 리더보드 한 줄 — 순위·점수·층 표시, 클릭 시 상세 콜백
public class ScoreEntryRow : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text depthText;
    [SerializeField] private Button selectButton;

    // 순위·기록 세팅
    public void Set(int rank, ScoreEntry entry, Action<ScoreEntry> onSelected)
    {
        if (rankText != null)
            rankText.text = $"{rank}";
        if (scoreText != null)
            scoreText.text = $"{entry.finalScore:N0}";
        if (depthText != null)
            depthText.text = $"{entry.reachedDepth}F";

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelected?.Invoke(entry));
        }
    }
}
