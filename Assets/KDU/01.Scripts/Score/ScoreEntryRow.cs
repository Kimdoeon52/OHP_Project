using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 내 기록 한 줄 — 이름·세계 순위·로컬 순위·층·점수, 클릭 시 상세 콜백
public class ScoreEntryRow : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text depthText;
    [SerializeField] private Button selectButton;

    [Tooltip("Steam 닉네임")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("세계 순위")]
    [SerializeField] private TMP_Text worldRankText;

    // 로컬 순위·기록 세팅
    public void Set(int rank, ScoreEntry entry, Action<ScoreEntry> onSelected)
    {
        if (rankText != null)
            rankText.text = RankFormat.Ordinal(rank);
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

    // 이름·세계 순위 세팅 (Steam 조회 후)
    public void SetWorld(string playerName, string worldRank)
    {
        if (nameText != null)
            nameText.text = playerName;
        if (worldRankText != null)
            worldRankText.text = worldRank;
    }
}
