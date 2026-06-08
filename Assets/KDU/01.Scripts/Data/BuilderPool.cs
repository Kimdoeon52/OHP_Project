using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuilderPool", menuName = "60s Dungeon/Map/Builder Pool")]
public class BuilderPool : ScriptableObject
{
    [SerializeField] private List<ScriptableObject> builders = new List<ScriptableObject>();

    public ILayoutBuilder Pick(DungeonRandom rng)
    {
        if (rng == null || builders == null || builders.Count == 0) 
            return null;

        var valid = new List<ILayoutBuilder>(builders.Count);
        foreach (var so in builders)
        {
            if (so is ILayoutBuilder lb) 
                valid.Add(lb);
            else if (so != null) 
                Debug.LogWarning($"[BuilderPool] {so.name}");
        }
        if (valid.Count == 0) return null;
        return rng.Pick(valid);
    }
}
