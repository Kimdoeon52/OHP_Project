using System.Collections.Generic;

/// <summary>
/// 시드 기반 결정론적 난수. 맵 생성 / 스폰 / Feeling 선택 등 모든 던전 무작위는 이 클래스만 사용.
/// UnityEngine.Random / System.Random 직접 호출 금지.
/// 호출 순서가 결과를 결정하므로 리팩토링 시 호출 순서 보존 필수.
/// </summary>
public class DungeonRandom
{
    private System.Random rng;

    public int Seed { get; private set; }

    /// <summary>디버그/재현용. 같은 시드라도 호출 횟수가 다르면 결과가 갈라진 것.</summary>
    public int CallCount { get; private set; }

    public void Init(int seed)
    {
        Seed = seed;
        CallCount = 0;
        rng = new System.Random(seed);
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        CallCount++;
        return rng.Next(minInclusive, maxExclusive);
    }

    /// <summary>[0, 1) 범위의 float.</summary>
    public float Value()
    {
        CallCount++;
        return (float)rng.NextDouble();
    }

    public T Pick<T>(IList<T> list)
    {
        if (list == null || list.Count == 0) return default;
        return list[Range(0, list.Count)];
    }

    public bool Chance(float probability)
    {
        return Value() < probability;
    }

    /// <summary>Run Seed + depth 로 층별 시드를 파생. 같은 (run, depth) → 같은 층 시드.</summary>
    public static int DeriveFloorSeed(int runSeed, int depth)
    {
        return runSeed ^ (depth * 31);
    }
}
