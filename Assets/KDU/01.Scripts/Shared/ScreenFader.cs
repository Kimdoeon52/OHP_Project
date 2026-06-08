using System.Collections;
using UnityEngine;

// 화면 페이드 인/아웃
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : Singleton<ScreenFader>
{
    [SerializeField] private float defaultDuration = 0.4f;

    [Range(0f, 1f)]
    [SerializeField] private float initialAlpha = 0f;

    private CanvasGroup _group;

    protected override void Awake()
    {
        base.Awake();
        _group = GetComponent<CanvasGroup>();
        _group.alpha = initialAlpha;
        _group.blocksRaycasts = initialAlpha > 0.001f;
        _group.interactable = false;
    }

    public IEnumerator FadeOut(float duration = -1f) => Fade(1f, duration < 0f ? defaultDuration : duration);
    public IEnumerator FadeIn(float duration = -1f) => Fade(0f, duration < 0f ? defaultDuration : duration);

    private IEnumerator Fade(float target, float duration)
    {
        if (_group == null) 
            yield break;

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
