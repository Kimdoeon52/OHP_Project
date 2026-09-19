using UnityEngine;

// 값 범위 변환·이징
public static class MathUtils
{
    // ── Remap ──

    // from 범위 값을 to 범위로 변환
    public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        if (Mathf.Approximately(fromMax, fromMin))
            return toMin;

        return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }

    // Remap 후 to 범위로 자름
    public static float RemapClamped(this float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        float r = value.Remap(fromMin, fromMax, toMin, toMax);
        return toMin < toMax ? Mathf.Clamp(r, toMin, toMax) : Mathf.Clamp(r, toMax, toMin);
    }

    // from 범위를 0~1로 정규화
    public static float Remap01(this float value, float fromMin, float fromMax)
    {
        return value.RemapClamped(fromMin, fromMax, 0f, 1f);
    }

    // ── Easing (t: 0~1) ──

    public static float EaseInQuad(this float t) => t * t;
    public static float EaseOutQuad(this float t) => t * (2f - t);
    public static float EaseInOutQuad(this float t) => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

    public static float EaseInCubic(this float t) => t * t * t;

    public static float EaseOutCubic(this float t)
    {
        t -= 1f;
        return t * t * t + 1f;
    }

    public static float EaseInOutCubic(this float t) => t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;

    // 끝에서 살짝 넘쳤다 복귀 (UI 등장)
    public static float EaseOutBack(this float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    // 탄성 있게 튕기며 정지
    public static float EaseOutElastic(this float t)
    {
        if (t <= 0f)
            return 0f;
        if (t >= 1f)
            return 1f;

        const float c4 = 2f * Mathf.PI / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    // 바닥에 튕기듯 낙하
    public static float EaseOutBounce(this float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1f / d1)
            return n1 * t * t;

        if (t < 2f / d1)
        {
            t -= 1.5f / d1;
            return n1 * t * t + 0.75f;
        }

        if (t < 2.5f / d1)
        {
            t -= 2.25f / d1;
            return n1 * t * t + 0.9375f;
        }

        t -= 2.625f / d1;
        return n1 * t * t + 0.984375f;
    }
}
