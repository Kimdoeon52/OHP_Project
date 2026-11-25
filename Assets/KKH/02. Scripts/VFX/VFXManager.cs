using System.Collections.Generic;
using UnityEngine;

// 이펙트 오브젝트 풀링 매니저 (GameEvents.OnPlayVFX 구독)
public class VFXManager : PersistentSingleton<VFXManager>
{
    [System.Serializable]
    public struct VFXEntry
    {
        public VFXType type;
        public GameObject prefab;
        public int initialPoolSize;
    }

    [Header("이펙트 프리팹 매핑 설정")]
    [SerializeField] private List<VFXEntry> vfxList = new List<VFXEntry>();

    private Dictionary<VFXType, Queue<PoolableVFX>> _poolDictionary;
    private Dictionary<VFXType, GameObject> _prefabDictionary;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        InitializePools();
    }

    private void OnEnable()
    {
        GameEvents.OnPlayVFX += PlayVFX;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayVFX -= PlayVFX;
    }

    private void InitializePools()
    {
        _poolDictionary = new Dictionary<VFXType, Queue<PoolableVFX>>();
        _prefabDictionary = new Dictionary<VFXType, GameObject>();

        if (vfxList == null)
            return;

        foreach (var entry in vfxList)
        {
            if (entry.type == VFXType.None || entry.prefab == null)
                continue;

            _prefabDictionary[entry.type] = entry.prefab;
            _poolDictionary[entry.type] = new Queue<PoolableVFX>();

            for (int i = 0; i < entry.initialPoolSize; i++)
                CreateNewVFXInstance(entry.type, entry.prefab);
        }
    }

    private PoolableVFX CreateNewVFXInstance(VFXType type, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);

        PoolableVFX poolable = obj.GetComponent<PoolableVFX>();
        if (poolable == null)
            poolable = obj.AddComponent<PoolableVFX>();

        poolable.Init(this, type);
        _poolDictionary[type].Enqueue(poolable);
        return poolable;
    }

    public void PlayVFX(Vector3 position, VFXType type)
    {
        if (_poolDictionary == null || !_poolDictionary.ContainsKey(type))
        {
            Debug.LogError($"VFXType {type} is not registered in VFXManager");
            return;
        }

        PoolableVFX vfx = null;
        var queue = _poolDictionary[type];

        if (queue.Count > 0)
        {
            vfx = queue.Dequeue();
        }
        else if (_prefabDictionary.TryGetValue(type, out GameObject prefab))
        {
            vfx = CreateNewVFXInstance(type, prefab);
            queue.Dequeue();
        }

        if (vfx != null)
        {
            vfx.transform.position = position;
            vfx.gameObject.SetActive(true);
        }
    }

    public void ReturnToPool(VFXType type, PoolableVFX vfx)
    {
        if (vfx == null || _poolDictionary == null)
            return;

        if (_poolDictionary.ContainsKey(type))
            _poolDictionary[type].Enqueue(vfx);
    }
}
