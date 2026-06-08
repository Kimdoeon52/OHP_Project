using System.Collections.Generic;


// 시드 기반 생성을 위한 랜덤
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

    public float Value()
    {
        CallCount++;
        return (float)rng.NextDouble();
    }

    public T Pick<T>(IList<T> list)
    {
        if (list == null || list.Count == 0) 
            return default;

        return list[Range(0, list.Count)];
    }

    public bool Chance(float probability)
    {
        return Value() < probability;
    }

    // 시드기반 맵 생성을 위해 층 + 시드로 시드값 생성
    public static int DeriveFloorSeed(int runSeed, int depth)
    {
        return runSeed ^ (depth * 31);
    }
}
