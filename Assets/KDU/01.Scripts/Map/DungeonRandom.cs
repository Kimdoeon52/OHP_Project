using System.Collections.Generic;

/// <summary>
/// 시드 기반 결정론적 난수. 맵 생성 / 스폰 / Feeling 선택 등 모든 던전 무작위는 이 클래스만 사용.
/// UnityEngine.Random / System.Random 직접 호출 금지.
/// </summary>
public class DungeonRandom
{
    private System.Random rng;

    public int Seed { get; private set; }

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

    /// <summary>Run Seed + depth 로 층별 시드를 파생. 같은 (run, depth) -> 같은 층 시드.</summary>
    public static int DeriveFloorSeed(int runSeed, int depth)
    {
        return runSeed ^ (depth * 31);
    }
}
