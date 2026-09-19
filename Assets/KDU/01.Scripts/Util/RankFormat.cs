// 순위 표기 — 1st, 2nd, 3rd, 4th ... / 상한 밖은 NA
public static class RankFormat
{
    public const string OutOfRank = "NA";

    public static string Ordinal(int rank, int maxRank = int.MaxValue)
    {
        if (rank <= 0 || rank > maxRank)
            return OutOfRank;

        // 11~13은 th
        int mod100 = rank % 100;
        if (mod100 >= 11 && mod100 <= 13)
            return $"{rank}th";

        switch (rank % 10)
        {
            case 1: return $"{rank}st";
            case 2: return $"{rank}nd";
            case 3: return $"{rank}rd";
            default: return $"{rank}th";
        }
    }
}
