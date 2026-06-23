using TMPro;
using UnityEngine;

// 챌린지 선택 창 왼쪽 고정 패널. hover된 챌린지의 이름+설명 표시.
public class ChallengeDescriptionPanel : Singleton<ChallengeDescriptionPanel>
{
    [Tooltip("이름 텍스트")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("설명 텍스트")]
    [SerializeField] private TMP_Text descriptionText;

    private ChallengeData _current;

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += Refresh;
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= Refresh;
    }

    // hover 진입 시 트리거가 호출 — 해당 챌린지 정보로 갱신.
    public void Show(ChallengeData data)
    {
        if (data == null)
            return;

        _current = data;
        Refresh();
    }

    // 언어 변경 시 현재 표시 중인 챌린지 설명을 다시 그린다.
    private void Refresh()
    {
        if (_current == null)
            return;

        if (nameText != null)
            nameText.text = _current.LocalizedDisplayName;

        if (descriptionText != null)
            descriptionText.text = _current.LocalizedDescription;
    }
}
