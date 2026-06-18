using UnityEngine;

/// <summary>
/// 파티클 재생이 끝나면 자동으로 오브젝트를 파괴하는 최적화 헬퍼 컴포넌트입니다.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class AutoDestroyParticle : MonoBehaviour
{
    private ParticleSystem _particleSystem;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        // 파티클의 재생 지속 시간을 기준으로 파괴를 예약합니다.
        float duration = _particleSystem.main.duration + _particleSystem.main.startLifetime.constantMax;
        Destroy(gameObject, duration);
    }
}
