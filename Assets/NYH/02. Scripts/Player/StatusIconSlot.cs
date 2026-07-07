using UnityEngine;
using UnityEngine.UI;

// 상태 아이콘 한 칸. 아이콘 스프라이트와 남은 시간 회색 오버레이(쿨타임)를 표시한다.
// 회색은 밑에서 위로 차오르고, 마지막 blinkThreshold초 구간에는 가득 찬 채 깜빡이다 사라진다.
public class StatusIconSlot : MonoBehaviour
{
    [Tooltip("상태 아이콘을 그릴 Image")]
    [SerializeField] private Image icon;

    [Tooltip("남은 시간 회색 오버레이. Image Type=Filled, Fill Method=Vertical, Fill Origin=Bottom 으로 설정")]
    [SerializeField] private Image cooldownOverlay;

    [Tooltip("남은 시간이 이 값(초) 이하면 오버레이가 가득 찬 채 깜빡인다")]
    [SerializeField, Min(0f)] private float blinkThreshold = 2f;

    [Tooltip("깜빡임 한 번 주기(초)")]
    [SerializeField, Min(0.05f)] private float blinkInterval = 0.2f;

    // 아이콘 스프라이트와 남은/전체 시간을 받아 표시를 갱신한다. 바가 매 프레임 호출한다.
    public void Apply(Sprite sprite, float remaining, float total)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        UpdateOverlay(remaining, total);
    }

    private void UpdateOverlay(float remaining, float total)
    {
        if (cooldownOverlay == null)
            return;

        // 남은 시간이 줄수록 회색이 차오른다. 마지막 blinkThreshold초에 딱 가득 차도록 그 구간을 뺀 비율로 계산한다.
        float fill;
        if (remaining > blinkThreshold && total > blinkThreshold)
            fill = 1f - (remaining - blinkThreshold) / (total - blinkThreshold);
        else
            fill = 1f;

        cooldownOverlay.fillAmount = Mathf.Clamp01(fill);

        // 마지막 blinkThreshold초: 가득 찬 회색을 주기적으로 켰다 껐다 한다.
        bool blinking = remaining <= blinkThreshold;
        bool visibleThisFrame = !blinking || Mathf.Repeat(Time.unscaledTime, blinkInterval * 2f) < blinkInterval;
        cooldownOverlay.enabled = visibleThisFrame && cooldownOverlay.fillAmount > 0f;
    }
}
