using TMPro;
using UnityEngine;

// "퀵슬롯 1"처럼 고정 단어(key) + 번호(number)가 합쳐진 텍스트용
public class LocalizedNumberedText : MonoBehaviour
{
    public string key;
    public string number;
    private TextMeshProUGUI tmp;

    void Start()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        LocalizationManager.Instance.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    void UpdateText()
    {
        tmp.text = LocalizationManager.Instance.Get(key) + " " + number;
    }

    void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
    }
}
