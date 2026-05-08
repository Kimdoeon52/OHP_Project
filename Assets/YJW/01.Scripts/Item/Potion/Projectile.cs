using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class Projectile : MonoBehaviour
{
    public void Launch(Vector2Int targetPos, Action onLand)
    {
        FlyAsync(targetPos, onLand).Forget();
    }

    private async UniTaskVoid FlyAsync(Vector2Int targetPos, Action onLand)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetPos.x, targetPos.y, 0);
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * 2f;
            transform.position = pos;

            await UniTask.Yield();
        }

        // 착지 후 효과 적용
        onLand?.Invoke();
    }
}
