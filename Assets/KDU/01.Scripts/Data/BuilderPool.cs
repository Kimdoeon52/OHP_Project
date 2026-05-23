using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ILayoutBuilder SO 풀. MapGenerator 가 builder 인자 null 일 때 Pick(rng) 로 무작위 선택.
/// 새 빌더 추가 = SO 1개 + 이 풀의 entries 에 드래그-드롭 (기존 코드 수정 X).
/// 풀에는 ILayoutBuilder 를 구현한 ScriptableObject 만 등록 — 캐스팅 실패 시 경고.
/// </summary>
[CreateAssetMenu(fileName = "BuilderPool", menuName = "60s Dungeon/Map/Builder Pool")]
public class BuilderPool : ScriptableObject
{
    [Tooltip("LoopBuilder / FigureEightBuilder 등 ILayoutBuilder SO 들")]
    [SerializeField] private List<ScriptableObject> builders = new List<ScriptableObject>();

    public ILayoutBuilder Pick(DungeonRandom rng)
    {
        if (rng == null || builders == null || builders.Count == 0) return null;

        var valid = new List<ILayoutBuilder>(builders.Count);
        foreach (var so in builders)
        {
            if (so is ILayoutBuilder lb) valid.Add(lb);
            else if (so != null) Debug.LogWarning($"[BuilderPool] '{so.name}' 가 ILayoutBuilder 미구현 — 무시");
        }
        if (valid.Count == 0) return null;
        return rng.Pick(valid);
    }
}
