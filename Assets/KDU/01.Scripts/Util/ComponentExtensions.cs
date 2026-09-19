using UnityEngine;

// 컴포넌트 조회/부착
public static class ComponentExtensions
{
    // 있으면 가져오고 없으면 부착
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        return go.TryGetComponent(out T c) ? c : go.AddComponent<T>();
    }

    public static T GetOrAddComponent<T>(this Component self) where T : Component
    {
        return self.gameObject.GetOrAddComponent<T>();
    }
}
