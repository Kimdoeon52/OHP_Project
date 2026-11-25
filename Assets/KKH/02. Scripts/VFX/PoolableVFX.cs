using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PoolableVFX : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    private VFXManager _manager;
    private VFXType _myType;
    private bool _isInPool = false;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    public void Init(VFXManager manager, VFXType type)
    {
        _manager = manager;
        _myType = type;
        _isInPool = true;
    }

    private void OnEnable()
    {
        _isInPool = false;
        if (_particleSystem != null)
            _particleSystem.Play(true);
    }

    private void Update()
    {
        if (_particleSystem != null && !_particleSystem.IsAlive(true))
            ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (_isInPool)
            return;

        _isInPool = true;
        gameObject.SetActive(false);

        if (_manager != null)
            _manager.ReturnToPool(_myType, this);
    }
}
