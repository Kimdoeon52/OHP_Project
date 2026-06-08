using TMPro;
using UnityEngine;

/// <summary>
/// 튜토리얼 안내 텍스트 패널. TutorialController 가 단계마다 ShowHint / HideHint 호출.
/// panel 을 지정하면 그 오브젝트를 켜고 끔, 비우면 이 오브젝트 자체를 토글.
///
/// 자유 둘러보기 단계(Step.freeLook)에선 메인 패널을 숨기고 하단 작은 안내문(bottomPrompt)만 띄운다.
/// </summary>
public class TutorialHintUI : MonoBehaviour
{
    [Tooltip("튜토리얼 패널")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text hintText;

    [Header("튜토리얼 하단 안내문")]
    [SerializeField] private GameObject bottomPrompt;
    [SerializeField] private TMP_Text bottomPromptText;

    private GameObject Root => panel != null ? panel : gameObject;

    /// <summary>메인 안내 패널 표시 — 하단 안내문은 숨김.</summary>
    public void ShowHint(string text)
    {
        if (hintText != null) hintText.text = text;
        Root.SetActive(true);
        if (bottomPrompt != null) bottomPrompt.SetActive(false);
    }

    /// <summary>하단 작은 안내문만 표시 — 메인 패널은 숨겨 시야 확보(자유 둘러보기).</summary>
    public void ShowPrompt(string text)
    {
        Root.SetActive(false);
        if (bottomPrompt != null)
        {
            if (bottomPromptText != null) bottomPromptText.text = text;
            bottomPrompt.SetActive(true);
        }
    }

    public void HideHint()
    {
        Root.SetActive(false);
        if (bottomPrompt != null) bottomPrompt.SetActive(false);
    }
}
