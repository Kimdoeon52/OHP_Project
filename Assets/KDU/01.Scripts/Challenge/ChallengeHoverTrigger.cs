using UnityEngine;
using UnityEngine.EventSystems;

// 챌린지 토글/이미지에 부착. hover 시 왼쪽 설명 패널 갱신.
public class ChallengeHoverTrigger : MonoBehaviour, IPointerEnterHandler
{
    [Tooltip("이 챌린지 데이터")]
    [SerializeField] private ChallengeData data;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ChallengeDescriptionPanel.Instance != null)
            ChallengeDescriptionPanel.Instance.Show(data);
    }
}
