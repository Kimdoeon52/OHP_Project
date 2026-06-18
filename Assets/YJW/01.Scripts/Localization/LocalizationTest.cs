using UnityEngine;

// 언어 전환 UI 나오기 전까지 인스펙터에서 테스트용으로 쓰는 임시 스크립트
public class LocalizationTest : MonoBehaviour
{
    [ContextMenu("한국어로 전환")]
    void SwitchToKorean()
    {
        LocalizationManager.Instance.SetLanguage(LocalizationManager.Language.Korean);
    }

    [ContextMenu("영어로 전환")]
    void SwitchToEnglish()
    {
        LocalizationManager.Instance.SetLanguage(LocalizationManager.Language.English);
    }
}
