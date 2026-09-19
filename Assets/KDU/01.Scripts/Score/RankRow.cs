using System;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Steam 랭킹 한 줄 — 순위·닉네임·점수·층, 내 기록 강조, 클릭 시 상세 콜백
public class RankRow : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text depthText;

    [Tooltip("내 기록 강조")]
    [SerializeField] private GameObject meHighlight;

    [Tooltip("클릭 시 상세 팝업")]
    [SerializeField] private Button selectButton;

    private CSteamID _user;

    // 순위·기록 세팅, 상세 정보가 있으면 클릭 가능
    public void Set(SteamLeaderboard.Entry entry, Action<SteamLeaderboard.Entry> onSelected)
    {
        _user = entry.user;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelected?.Invoke(entry));
            selectButton.interactable = entry.detail != null;
        }

        if (rankText != null)
            rankText.text = RankFormat.Ordinal(entry.rank, SteamLeaderboard.MaxRank);
        if (scoreText != null)
            scoreText.text = $"{entry.score:N0}";
        if (depthText != null)
            depthText.text = $"{entry.depth}F";
        if (meHighlight != null)
            meHighlight.SetActive(entry.isMe);

        RefreshName();
    }

    // 닉네임 캐시 도착 시 재호출
    public void RefreshName()
    {
        if (nameText != null)
            nameText.text = SteamFriends.GetFriendPersonaName(_user);
    }
}
