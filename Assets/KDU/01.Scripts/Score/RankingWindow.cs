using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 랭킹 창 — 왼쪽 탭 버튼으로 오른쪽 페이지 전환. 페이지는 켜질 때 스스로 목록 갱신
public class RankingWindow : MonoBehaviour
{
    // 탭 버튼 1개 ↔ 페이지 1개
    [Serializable]
    public struct Tab
    {
        public Button button;
        public GameObject page;
    }

    [Tooltip("탭 버튼 ↔ 페이지")]
    [SerializeField] private List<Tab> tabs = new List<Tab>();

    [Tooltip("닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Tooltip("열 때 기본 탭 번호")]
    [SerializeField] private int defaultTab;

    private void Awake()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            if (tabs[i].button == null)
                continue;

            int index = i;
            tabs[i].button.onClick.AddListener(() => Show(index));
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    // 기본 탭으로 열기 (메뉴 버튼)
    public void Open() => OpenTab(defaultTab);

    // 지정 탭으로 열기 — 페이지를 먼저 고른 뒤 창을 켜서 선택 페이지만 갱신되게
    public void OpenTab(int index)
    {
        Show(index);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // 선택 페이지만 켜고, 선택된 탭 버튼은 비활성으로 표시
    public void Show(int index)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            bool selected = i == index;

            if (tabs[i].page != null)
                tabs[i].page.SetActive(selected);
            if (tabs[i].button != null)
                tabs[i].button.interactable = !selected;
        }
    }
}
