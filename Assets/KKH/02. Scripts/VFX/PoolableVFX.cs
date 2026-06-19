using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PoolableVFX : MonoBehaviour
{

    private ParticleSystem _particleSystem;
    private VFXManager _manager;
    private VFXType _myType;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    public void Init(VFXManager manager, VFXType type)
    {
        _manager = manager;
        _myType = type;
    }
    private void OnEnable()
    {
        if (_particleSystem != null)
        {
            _particleSystem.Play();
        }
    }

    private void Update()
    {
        if (_particleSystem != null && !_particleSystem.isPlaying)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        gameObject.SetActive(false);
        if (_manager != null)
        {
            _manager.ReturnToPool(_myType, this);
        }
    }
}
