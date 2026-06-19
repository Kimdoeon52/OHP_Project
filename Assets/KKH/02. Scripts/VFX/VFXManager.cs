using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [System.Serializable]
    public struct VFXEntry
    {
        public VFXType type;
        public GameObject prefab;
        public int initialPoolSize;
    }

    [Header("이펙트 프리팹 매핑 설정")]
    [SerializeField] private List<VFXEntry> vfxList;

    // 이펙트 종류별 오브젝트 풀 대기 큐
    private Dictionary<VFXType, Queue<PoolableVFX>> _poolDictionary;
    private Dictionary<VFXType, GameObject> _prefabDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    private void OnEnable()
    {
        // GameEvents.OnPlayVFX += PlayVFX;
    }

    private void OnDisable()
    {
        // GameEvents.OnPlayVFX -= PlayVFX;
    }

    private void InitializePools()
    {
        _poolDictionary = new Dictionary<VFXType, Queue<PoolableVFX>>();
        _prefabDictionary = new Dictionary<VFXType, GameObject>();

        foreach (var entry in vfxList)
        {
            if (entry.type == VFXType.None || entry.prefab == null) continue;

            _prefabDictionary[entry.type] = entry.prefab;
            _poolDictionary[entry.type] = new Queue<PoolableVFX>();

            for (int i = 0; i < entry.initialPoolSize; i++)
            {
                CreateNewVFXInstance(entry.type, entry.prefab);
            }
        }
    }

    private PoolableVFX CreateNewVFXInstance(VFXType type, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);

        PoolableVFX poolable = obj.GetComponent<PoolableVFX>();
        if (poolable == null)
        {
            poolable = obj.AddComponent<PoolableVFX>();
        }

        poolable.Init(this, type);

        _poolDictionary[type].Enqueue(poolable);
        return poolable;

    }

    /// <summary>
    /// 외부 이벤트 또는 직접 호출을 받아 특정위치에 이펙트를 재생함
    /// </summary>
    public void PlayVFX(Vector3 position, VFXType type)
    {
        if (!_poolDictionary.ContainsKey(type))
        {
            Debug.LogError($"VFXType{type} is not registered in VFXManager");
            return;
        }
        PoolableVFX vfx = null;
        var queue = _poolDictionary[type];

        if (queue.Count > 0)
        {
            vfx = queue.Dequeue();
        }
        else
        {
            if (_prefabDictionary.TryGetValue(type, out GameObject prefab))
            {
                vfx = CreateNewVFXInstance(type, prefab);
                queue.Dequeue();
            }
        }
        if (vfx != null)
        {
            vfx.transform.position = position;
            vfx.gameObject.SetActive(true);
        }
    }
    /// <summary>
    /// PoolableVFX가 재생 졸료 후 호출해서 풀 큐로 인스턴스 반환함 
    /// </summary>
    public void ReturnToPool(VFXType type, PoolableVFX vfx)
    {
        if (_poolDictionary.ContainsKey(type))
        {
            _poolDictionary[type].Enqueue(vfx);
        }

    }

}

