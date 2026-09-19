using UnityEngine;

// 인스펙터 표시만, 수정 불가
public class ReadOnlyAttribute : PropertyAttribute { }

// Vector2를 min/max 슬라이더로 표시 (x=min, y=max)
public class MinMaxAttribute : PropertyAttribute
{
    public readonly float Min;
    public readonly float Max;

    public MinMaxAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}
