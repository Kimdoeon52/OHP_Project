using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Floor Feeling 풀 (Hazard/Traps/Dark 3종 고정 — GDD 6.3).
/// 영역별 가중치(GDD 5.2)로 발동 시 1개 선택, 미발동 시 null 반환.
/// 새 Feeling 추가 = 이 풀의 entries 에 SO 드래그-드롭 (기존 코드 수정 X).
/// </summary>
[CreateAssetMenu(fileName = "FloorFeelingPool", menuName = "60s Dungeon/Map/Floor Feeling Pool")]
public class FloorFeelingPool : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public FloorFeeling feeling;

        [Tooltip("Area 1 (하수구, depth 1~5) 가중치 — GDD 5.2 기본값 Hazard 40 / Traps 40 / Dark 20")]
        [Min(0)] public int weightArea1 = 1;

        [Tooltip("Area 2 (던전, depth 6~10) 가중치 — GDD 5.2 기본값 Hazard 40 / Traps 30 / Dark 30")]
        [Min(0)] public int weightArea2 = 1;

        [Tooltip("Area 3 (지옥, depth 11~15) 가중치 — GDD 5.2 기본값 Hazard 30 / Traps 30 / Dark 40")]
        [Min(0)] public int weightArea3 = 1;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    [Header("발동 확률 (GDD 6.3: 50%)")]
    [SerializeField, Range(0f, 1f)] private float triggerChance = 0.5f;

    /// <summary>
    /// 발동 시 해당 depth 영역의 가중치로 Feeling 1개를 선택. 미발동 시 null.
    /// 결정론: 같은 (rng 상태, depth) → 같은 결과.
    /// </summary>
    public FloorFeeling Pick(DungeonRandom rng, int depth)
    {
        if (rng == null || entries == null || entries.Count == 0) return null;
        if (!rng.Chance(triggerChance)) return null;

        int area = MapGenerator.AreaFromDepth(depth);
        int totalWeight = 0;
        foreach (var e in entries)
        {
            if (e == null || e.feeling == null) continue;
            totalWeight += GetWeight(e, area);
        }
        if (totalWeight <= 0) return null;

        int roll = rng.Range(0, totalWeight);
        int cumulative = 0;
        foreach (var e in entries)
        {
            if (e == null || e.feeling == null) continue;
            cumulative += GetWeight(e, area);
            if (roll < cumulative) return e.feeling;
        }
        return null;
    }

    private static int GetWeight(Entry e, int area)
    {
        switch (area)
        {
            case 1: return e.weightArea1;
            case 2: return e.weightArea2;
            case 3: return e.weightArea3;
            default: return 0;
        }
    }
}
