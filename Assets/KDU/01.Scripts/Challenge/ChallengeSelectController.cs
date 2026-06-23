using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 씬의 챌린지 설정 창. 선택을 ChallengeSelection 정적 전달자에 기록 → 새 게임 시 게임 씬이 읽음.
public class ChallengeSelectController : MonoBehaviour
{
    // 버튼 1개 ↔ 챌린지 1종 연결. 라벨은 On/Off 표시.
    [Serializable]
    public struct ButtonBinding
    {
        public ChallengeType type;
        public Button button;
        public TMP_Text label;
    }

    [Tooltip("챌린지 버튼 6종 연결")]
    [SerializeField] private List<ButtonBinding> buttons = new List<ButtonBinding>();

    [Tooltip("On 라벨")]
    [SerializeField] private string onText = "On";

    [Tooltip("Off 라벨")]
    [SerializeField] private string offText = "Off";

    [Tooltip("시드 입력 필드 (비우면 무작위 시드)")]
    [SerializeField] private TMP_InputField seedInput;

    [Tooltip("설정 창 루트 — 열기/닫기 토글")]
    [SerializeField] private GameObject panel;

    // 현재 켜진 챌린지.
    private readonly HashSet<ChallengeType> active = new HashSet<ChallengeType>();

    private void Awake()
    {
        foreach (var b in buttons)
        {
            if (b.button == null)
                continue;

            ChallengeType type = b.type;
            b.button.onClick.AddListener(() => ToggleType(type));
        }
    }

    // 버튼 클릭 — 해당 챌린지 On/Off 뒤집고 라벨 갱신.
    private void ToggleType(ChallengeType type)
    {
        if (!active.Remove(type))
            active.Add(type);

        RefreshLabels();
    }

    // 챌린지 버튼이 호출 — 설정 창 열고 기존 선택을 버튼에 반영.
    public void Open()
    {
        if (panel != null)
            panel.SetActive(true);

        SyncFromSelection();
    }

    // 확인 버튼이 호출 — 현재 선택/시드를 전달자에 기록하고 창 닫음.
    public void Confirm()
    {
        ChallengeSelection.Set(active.ToList(), TryGetSeed(out int seed), seed);

        if (panel != null)
            panel.SetActive(false);
    }

    // 닫기/취소 — 기록 없이 창만 닫음.
    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    // 기존에 고른 선택을 버튼 상태로 되돌림 — 창을 다시 열어도 유지되게.
    private void SyncFromSelection()
    {
        active.Clear();
        foreach (var t in ChallengeSelection.PendingTypes)
            active.Add(t);

        RefreshLabels();

        if (seedInput != null)
            seedInput.text = ChallengeSelection.HasSeed ? ChallengeSelection.Seed.ToString() : "";
    }

    // 각 버튼 라벨을 On/Off로 갱신.
    private void RefreshLabels()
    {
        foreach (var b in buttons)
        {
            if (b.label == null)
                continue;

            b.label.text = active.Contains(b.type) ? onText : offText;
        }
    }

    // 시드 입력 파싱 — 숫자면 그대로, 문자열이면 해시. 비면 시드 없음.
    private bool TryGetSeed(out int seed)
    {
        seed = 0;
        if (seedInput == null || string.IsNullOrWhiteSpace(seedInput.text))
            return false;

        string text = seedInput.text.Trim();
        seed = int.TryParse(text, out int n) ? n : text.GetHashCode();
        return true;
    }
}
