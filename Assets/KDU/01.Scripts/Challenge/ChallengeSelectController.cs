using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 씬의 챌린지 설정 창. 선택을 ChallengeSelection 정적 전달자에 기록 → 새 게임 시 게임 씬이 읽음.
public class ChallengeSelectController : MonoBehaviour
{
    // 토글 1개 ↔ 챌린지 1종 연결.
    [Serializable]
    public struct ToggleBinding
    {
        public ChallengeType type;
        public Toggle toggle;
    }

    [Tooltip("챌린지 토글 6종 연결")]
    [SerializeField] private List<ToggleBinding> toggles = new List<ToggleBinding>();

    [Tooltip("시드 입력 필드 (비우면 무작위 시드)")]
    [SerializeField] private TMP_InputField seedInput;

    [Tooltip("설정 창 루트 — 열기/닫기 토글")]
    [SerializeField] private GameObject panel;

    // 챌린지 버튼이 호출 — 설정 창 열고 기존 선택을 토글에 반영.
    public void Open()
    {
        if (panel != null)
            panel.SetActive(true);

        SyncTogglesFromSelection();
    }

    // 확인 버튼이 호출 — 현재 토글/시드를 전달자에 기록하고 창 닫음.
    public void Confirm()
    {
        ChallengeSelection.Set(CollectSelected(), TryGetSeed(out int seed), seed);

        if (panel != null)
            panel.SetActive(false);
    }

    // 닫기/취소 — 기록 없이 창만 닫음.
    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private List<ChallengeType> CollectSelected()
    {
        var selected = new List<ChallengeType>();
        foreach (var b in toggles)
        {
            if (b.toggle != null && b.toggle.isOn)
                selected.Add(b.type);
        }
        return selected;
    }

    // 기존에 고른 선택을 토글 상태로 되돌림 — 창을 다시 열어도 유지되게.
    private void SyncTogglesFromSelection()
    {
        var pending = ChallengeSelection.PendingTypes;
        foreach (var b in toggles)
        {
            if (b.toggle == null)
                continue;

            b.toggle.SetIsOnWithoutNotify(pending.Contains(b.type));
        }

        if (seedInput != null)
            seedInput.text = ChallengeSelection.HasSeed ? ChallengeSelection.Seed.ToString() : "";
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
