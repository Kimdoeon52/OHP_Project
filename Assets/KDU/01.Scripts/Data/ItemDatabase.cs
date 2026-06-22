using System.Collections.Generic;
using UnityEngine;

// 저장/로드용 id→SO 역참조 레지스트리. 세이브엔 id만 남고, 로드 때 여기서 SO 복원.
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "60s Dungeon/Data/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Tooltip("전 아이템 SO")]
    [SerializeField] private List<ItemBase> items = new List<ItemBase>();

    [Tooltip("전 저주 SO")]
    [SerializeField] private List<CurseData> curses = new List<CurseData>();
    public List<CurseData> Curses => curses;

    [Tooltip("전 마법 SO")]
    [SerializeField] private List<MagicData> magics = new List<MagicData>();

    // Resources에서 자동 로드할 때 쓸 에셋 이름
    private const string ResourcesPath = "ItemDatabase";

    private static ItemDatabase _instance;

    // ItemBase.Id → SO
    private Dictionary<int, ItemBase> _itemMap;
    // CurseType → SO (저주엔 id가 없어 enum을 유일 키로 씀)
    private Dictionary<CurseType, CurseData> _curseMap;
    // MagicType → SO
    private Dictionary<MagicType, MagicData> _magicMap;

    // 어디서든 접근할 단일 인스턴스. 명시 주입 없으면 Resources/ItemDatabase에서 로드.
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<ItemDatabase>(ResourcesPath);

            if (_instance == null)
                Debug.LogError("[ItemDatabase] Resources/ItemDatabase 없음");

            return _instance;
        }
        set => _instance = value;
    }

    public ItemBase GetItem(int id)
    {
        EnsureMaps();
        return _itemMap.TryGetValue(id, out var v) ? v : null;
    }

    // -1 또는 미등록이면 null (저주 없음)
    public CurseData GetCurse(int curseType)
    {
        if (curseType < 0)
            return null;

        EnsureMaps();
        return _curseMap.TryGetValue((CurseType)curseType, out var v) ? v : null;
    }

    public MagicData GetMagic(int magicType)
    {
        if (magicType < 0)
            return null;

        EnsureMaps();
        return _magicMap.TryGetValue((MagicType)magicType, out var v) ? v : null;
    }

    // 첫 접근 때 한 번 맵 구축
    private void EnsureMaps()
    {
        if (_itemMap != null)
            return;

        _itemMap = new Dictionary<int, ItemBase>();
        foreach (var it in items)
        {
            if (it == null)
                continue;

            if (_itemMap.ContainsKey(it.Id))
                Debug.LogError($"[ItemDatabase] 중복 id {it.Id}: {it.name}");
            else
                _itemMap[it.Id] = it;
        }

        _curseMap = new Dictionary<CurseType, CurseData>();
        foreach (var c in curses)
        {
            if (c == null)
                continue;

            if (_curseMap.ContainsKey(c.Type))
                Debug.LogError($"[ItemDatabase] 중복 CurseType {c.Type}: {c.name}");
            else
                _curseMap[c.Type] = c;
        }

        _magicMap = new Dictionary<MagicType, MagicData>();
        foreach (var m in magics)
        {
            if (m == null)
                continue;

            if (_magicMap.ContainsKey(m.Type))
                Debug.LogError($"[ItemDatabase] 중복 MagicType {m.Type}: {m.name}");
            else
                _magicMap[m.Type] = m;
        }
    }

#if UNITY_EDITOR
    // 프로젝트 전체에서 SO 자동 수집 + id 중복 검사
    [ContextMenu("Collect All")]
    private void CollectAll()
    {
        items = LoadAll<ItemBase>();
        curses = LoadAll<CurseData>();
        magics = LoadAll<MagicData>();

        _itemMap = null;
        EnsureMaps();

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[ItemDatabase] 수집: 아이템 {items.Count}, 저주 {curses.Count}, 마법 {magics.Count}");
    }

    private static List<T> LoadAll<T>() where T : Object
    {
        var list = new List<T>();
        var guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                list.Add(asset);
        }
        return list;
    }
#endif
}
