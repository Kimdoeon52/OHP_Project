using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [Tooltip("0 = 즉시 스냅, 0.1~0.3 = 부드러운 추적")]
    [SerializeField, Range(0f, 1f)] private float smoothing = 0.12f;

    private Vector3 _velocity;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

        if (smoothing <= 0f)
            transform.position = desired;
        else
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothing);
    }

    /// <summary>층 전환 등 타깃이 순간이동할 때 호출 — smoothdamp 누적 속도를 리셋하고 위치를 즉시 스냅.</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = target.position + offset;
        _velocity = Vector3.zero;
    }
}
