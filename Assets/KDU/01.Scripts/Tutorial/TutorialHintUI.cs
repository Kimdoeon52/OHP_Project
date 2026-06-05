using TMPro;
using UnityEngine;

/// <summary>
/// 튜토리얼 안내 텍스트 패널. TutorialController 가 단계마다 ShowHint / HideHint 호출.
/// panel 을 지정하면 그 오브젝트를 켜고 끔, 비우면 이 오브젝트 자체를 토글.
/// </summary>
public class TutorialHintUI : MonoBehaviour
{
    [Tooltip("켜고 끌 안내 패널 루트. 비우면 이 GameObject 를 토글.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text hintText;

    private GameObject Root => panel != null ? panel : gameObject;

    public void ShowHint(string text)
    {
        if (hintText != null) hintText.text = text;
        Root.SetActive(true);
    }

    public void HideHint()
    {
        Root.SetActive(false);
    }
}
