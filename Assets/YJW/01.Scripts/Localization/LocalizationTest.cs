using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 언어 전환 UI 나오기 전까지 인스펙터에서 테스트용으로 쓰는 임시 스크립트
public class LocalizationTest : MonoBehaviour
{
    [SerializeField] private TMP_Text languageText;

    private bool isKorean = true;
    private bool isEnglis = false;


    public void SwitchingLanguage()
    {
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
    void SwitchToKorean()
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
    void SwitchToEnglish()
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
        if (languageText != null)
            languageText.text = displayName;
        else
            Debug.LogWarning("LocalizationTest: Language Text가 연결되지 않아 표시 텍스트는 바꾸지 않습니다.", this);

        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("LocalizationTest: 씬에 LocalizationManager가 없어 언어를 변경할 수 없습니다.", this);
            return false;
        }

        LocalizationManager.Instance.SetLanguage(language);
        return true;
    }
}
