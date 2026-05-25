using System.Collections;
using UnityEngine;

/// <summary>
/// 화면 페이드 인/아웃. 풀스크린 검정 Image + CanvasGroup 의 알파를 시간 보간.
/// 타이머가 멈춰도 페이드는 동작해야 하므로 unscaledDeltaTime 사용.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private float defaultDuration = 0.4f;

    private CanvasGroup _group;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("ScreenFader 가 씬에 두 개 이상 있습니다.");
            return;
        }
        Instance = this;
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _group.interactable = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public IEnumerator FadeOut(float duration = -1f) => Fade(1f, duration < 0f ? defaultDuration : duration);
    public IEnumerator FadeIn(float duration = -1f) => Fade(0f, duration < 0f ? defaultDuration : duration);

    private IEnumerator Fade(float target, float duration)
    {
        if (_group == null) yield break;
        _group.blocksRaycasts = target > 0.001f;
        float start = _group.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        _group.alpha = target;
        _group.blocksRaycasts = target > 0.001f;
    }
}
