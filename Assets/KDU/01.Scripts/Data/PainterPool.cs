using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PainterPool", menuName = "60s Dungeon/Map/Painter Pool")]
public class PainterPool : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public ScriptableObject painter;

        [Min(1)] public int minDepth = 1;

        [Min(1)] public int maxDepth = 15;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IRegionPainter Pick(DungeonRandom rng, int depth)
    {
        if (rng == null || entries == null || entries.Count == 0) 
            return null;

        var matches = new List<IRegionPainter>();
        foreach (var e in entries)
        {
            if (e == null || e.painter == null) 
                continue;

            if (depth < e.minDepth || depth > e.maxDepth) 
                continue;

            if (e.painter is IRegionPainter p) 
                matches.Add(p);

            else 
                Debug.LogWarning($"[PainterPool] {e.painter.name}");
        }
        if (matches.Count == 0) 
            return null;

        return rng.Pick(matches);
    }
}
