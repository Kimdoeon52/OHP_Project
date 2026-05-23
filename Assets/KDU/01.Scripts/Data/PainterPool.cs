using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IRegionPainter SO 풀 — depth 범위 기반 선택.
/// 영역별 페인터(Sewer 1~5 / Dungeon 6~10 / Hell 11~15) 매핑에 사용.
/// 한 depth 에 여러 entry 가 매치되면 그 중 무작위 — 같은 영역 안에서 다양성 확보.
/// </summary>
[CreateAssetMenu(fileName = "PainterPool", menuName = "60s Dungeon/Map/Painter Pool")]
public class PainterPool : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("DefaultPainter 등 IRegionPainter SO")]
        public ScriptableObject painter;

        [Tooltip("이 페인터가 적용될 최소 depth (포함)")]
        [Min(1)] public int minDepth = 1;

        [Tooltip("이 페인터가 적용될 최대 depth (포함)")]
        [Min(1)] public int maxDepth = 15;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IRegionPainter Pick(DungeonRandom rng, int depth)
    {
        if (rng == null || entries == null || entries.Count == 0) return null;

        var matches = new List<IRegionPainter>();
        foreach (var e in entries)
        {
            if (e == null || e.painter == null) continue;
            if (depth < e.minDepth || depth > e.maxDepth) continue;
            if (e.painter is IRegionPainter p) matches.Add(p);
            else Debug.LogWarning($"[PainterPool] '{e.painter.name}' 가 IRegionPainter 미구현 — 무시");
        }
        if (matches.Count == 0) return null;
        return rng.Pick(matches);
    }
}
