using TMPro;
using UnityEngine;

public class LocalizedText : MonoBehaviour
{
    public string key;
    private TextMeshProUGUI tmp;

    void Start()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        LocalizationManager.Instance.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    void UpdateText()
    {
        tmp.text = LocalizationManager.Instance.Get(key);
    }

    void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
    }
}
