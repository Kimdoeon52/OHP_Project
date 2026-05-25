using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 체력 값을 UI Image의 fillAmount로 반영하는 HP 바 전용 스크립트입니다.
/// GridPlayerHealth.OnHealthChanged 이벤트를 구독해서 체력이 바뀔 때마다 자동으로 갱신합니다.
/// </summary>
public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("체력 참조")]
    [Tooltip("HP 바가 바라볼 플레이어 체력 컴포넌트입니다. 비워두면 씬에서 GridPlayerHealth를 자동으로 찾습니다.")]
    [SerializeField] private GridPlayerHealth helth;

    [Header("UI 참조")]
    [Tooltip("현재 체력 비율을 fillAmount로 표시할 Image입니다. Image Type은 Filled로 설정해야 합니다.")]
    [SerializeField] private Image fillImage;

    private void Awake()
    {
        if (helth == null)
            helth = FindFirstObjectByType<GridPlayerHealth>();
    }

    private void OnEnable()
    {
        if (helth == null) return;

        helth.OnHealthChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (helth == null) return;

        helth.OnHealthChanged -= Refresh;
    }

    /// <summary>
    /// 현재 HP / 최대 HP 비율을 계산해서 UI Image에 전달합니다.
    /// MaxHp가 0 이하인 비정상 상태에서는 0으로 표시해 UI 오류를 막습니다.
    /// </summary>
    private void Refresh()
    {
        if (fillImage == null || helth == null) return;

        fillImage.fillAmount = helth.MaxHp > 0
            ? (float)helth.CurrentHp / helth.MaxHp
            : 0f;
    }
}
