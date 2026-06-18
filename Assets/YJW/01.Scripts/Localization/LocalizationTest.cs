using TMPro;
using UnityEngine;

// 언어 전환 UI 나오기 전까지 인스펙터에서 테스트용으로 쓰는 임시 스크립트
public class LocalizationTest : MonoBehaviour
{
    [SerializeField] private TMP_Text languageText;

    private bool isKorean = true;
    private bool isEnglis = false;

    private void OnEnable()
    {
        SubscribeLanguageChanged();
        SyncFromCurrentLanguage();
    }

    private void Start()
    {
        // 씬 초기화 순서에 따라 OnEnable 시점에 매니저가 늦게 잡힐 수 있어 한 번 더 맞춘다.
        SubscribeLanguageChanged();
        SyncFromCurrentLanguage();
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= SyncFromCurrentLanguage;
    }

    public void SwitchingLanguage()
    {
        SyncFromCurrentLanguage();

        if (isKorean)
        {
            SwitchToEnglish();
        }
        else
        {
            SwitchToKorean();
        }
    }

    [ContextMenu("한국어로 전환")]
    private void SwitchToKorean()
    {
        if (isKorean)
        {
            Debug.Log("이미 한국어로 설정되어 있습니다.");
            return;
        }

        if (!TrySetLanguage(LocalizationManager.Language.Korean, "한국어"))
            return;

        isKorean = true;
        isEnglis = false;
    }

    [ContextMenu("영어로 전환")]
    private void SwitchToEnglish()
    {
        if (isEnglis)
        {
            Debug.Log("이미 영어로 설정되어 있습니다.");
            return;
        }

        if (!TrySetLanguage(LocalizationManager.Language.English, "English"))
            return;

        isKorean = false;
        isEnglis = true;
    }

    private bool TrySetLanguage(LocalizationManager.Language language, string displayName)
    {
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("LocalizationTest: 씬에 LocalizationManager가 없어 언어를 변경할 수 없습니다.", this);
            return false;
        }

        LocalizationManager.Instance.SetLanguage(language);
        ApplyLanguageState(language, displayName);
        return true;
    }

    private void SubscribeLanguageChanged()
    {
        if (LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLanguageChanged -= SyncFromCurrentLanguage;
        LocalizationManager.Instance.OnLanguageChanged += SyncFromCurrentLanguage;
    }

    private void SyncFromCurrentLanguage()
    {
        if (LocalizationManager.Instance == null)
        {
            ApplyLanguageState(LocalizationManager.Language.Korean, "한국어");
            return;
        }

        LocalizationManager.Language currentLanguage = LocalizationManager.Instance.currentLanguage;
        ApplyLanguageState(currentLanguage, GetDisplayName(currentLanguage));
    }

    private void ApplyLanguageState(LocalizationManager.Language language, string displayName)
    {
        isKorean = language == LocalizationManager.Language.Korean;
        isEnglis = language == LocalizationManager.Language.English;

        if (languageText != null)
            languageText.text = displayName;
        else
            Debug.LogWarning("LocalizationTest: Language Text가 연결되지 않아 표시 텍스트는 바꾸지 않습니다.", this);
    }

    private string GetDisplayName(LocalizationManager.Language language)
    {
        return language == LocalizationManager.Language.English ? "English" : "한국어";
    }
}
