using TMPro;
using UnityEngine;

// 챌린지 선택 창 왼쪽 고정 패널. hover된 챌린지의 이름+설명 표시.
public class ChallengeDescriptionPanel : Singleton<ChallengeDescriptionPanel>
{
    [Tooltip("이름 텍스트")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("설명 텍스트")]
    [SerializeField] private TMP_Text descriptionText;

    // hover 진입 시 트리거가 호출 — 해당 챌린지 정보로 갱신.
    public void Show(ChallengeData data)
    {
        if (data == null)
            return;

        if (nameText != null)
            nameText.text = data.DisplayName;

        if (descriptionText != null)
            descriptionText.text = data.Description;
    }
}
