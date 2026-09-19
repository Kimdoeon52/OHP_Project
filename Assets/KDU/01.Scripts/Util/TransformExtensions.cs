using UnityEngine;

// 자식 일괄 처리
public static class TransformExtensions
{
    // 자식 전부 파괴 (프레임 끝)
    public static void DestroyChildren(this Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.Destroy(t.GetChild(i).gameObject);
    }

    // 자식 전부 즉시 파괴 (에디터용)
    public static void DestroyChildrenImmediate(this Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    // 자식 전부 비활성
    public static void DeactivateChildren(this Transform t)
    {
        for (int i = 0; i < t.childCount; i++)
            t.GetChild(i).gameObject.SetActive(false);
    }

    // index번 자식 재사용, 없으면 프리팹으로 생성
    public static Transform GetOrCreateChild(this Transform t, GameObject prefab, int index)
    {
        if (index < t.childCount)
            return t.GetChild(index);

        return Object.Instantiate(prefab, t).transform;
    }
}
