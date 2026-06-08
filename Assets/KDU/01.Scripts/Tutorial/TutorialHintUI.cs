using TMPro;
using UnityEngine;

// 튜토리얼 안내 텍스트 패널
public class TutorialHintUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text hintText;

    [SerializeField] private GameObject bottomPrompt;
    [SerializeField] private TMP_Text bottomPromptText;

    private GameObject Root => panel != null ? panel : gameObject;

    public void ShowHint(string text)
    {
        if (hintText != null) 
            hintText.text = text;

        Root.SetActive(true);

        if (bottomPrompt != null) 
            bottomPrompt.SetActive(false);
    }

    public void ShowPrompt(string text)
    {
        Root.SetActive(false);
        if (bottomPrompt != null)
        {
            if (bottomPromptText != null) 
                bottomPromptText.text = text;

            bottomPrompt.SetActive(true);
        }
    }

    public void HideHint()
    {
        Root.SetActive(false);
        if (bottomPrompt != null) 
            bottomPrompt.SetActive(false);
    }
}
