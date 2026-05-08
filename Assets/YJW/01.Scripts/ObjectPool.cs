using System.Collections.Generic;
using UnityEngine;

public enum PoolType
{
    FirePotion,
    FireEffect,
    IcePotion,
    IceEffect
}

public class ObjectPool : Singleton<ObjectPool>
{
    [System.Serializable]
    public class PoolInfo
    {
        public PoolType type;    // 풀 타입
        public GameObject prefab; // 생성할 프리팹
        public int count;         // 미리 생성할 개수
    }

    [SerializeField] PoolInfo[] pools; // 인스펙터에서 풀 설정
    Dictionary<PoolType, Queue<GameObject>> poolDic = new(); // 타입별 오브젝트 큐

    protected override void Awake()
    {
        base.Awake();
        foreach (var info in pools)
        {
            // 타입별 부모 오브젝트 생성
            GameObject parent = new GameObject(info.type.ToString());
            parent.transform.SetParent(transform);

            // count만큼 미리 생성 후 비활성화하여 큐에 저장
            Queue<GameObject> q = new();
            for (int i = 0; i < info.count; i++)
            {
                var obj = Instantiate(info.prefab, parent.transform);
                obj.SetActive(false);
                q.Enqueue(obj);
            }
            poolDic.Add(info.type, q);
        }
    }

    // 풀에서 오브젝트 꺼내서 반환
    public GameObject Get(PoolType type)
    {
        if (!poolDic.ContainsKey(type)) return null;

        var q = poolDic[type];
        if (q.Count == 0)
        {
            Debug.LogWarning($"{type} 풀 부족!");
            return null;
        }

        var obj = q.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    // 다 쓴 오브젝트 풀에 반납
    public void Return(PoolType type, GameObject obj)
    {
        obj.SetActive(false);
        poolDic[type].Enqueue(obj);
    }
}