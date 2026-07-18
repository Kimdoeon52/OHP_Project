// 런 종료 시 확정된 점수 내역. 결산 UI가 항목별로 표시.
public struct ScoreResult
{
    public int seed;             // 런 시드
    public int reachedDepth;     // 도달 층
    public int depthBonus;       // 도달층 × 100
    public int residualBonus;    // 각 층 잔여 시간 합
    public int bossBonus;        // 보스 클리어 합
    public int killBonus;        // 처치 시간보너스 합
    public int killCount;        // 총 처치 수
    public int baseScore;        // 위 항목 합
    public int challengeCount;   // 활성 챌린지 수
    public float multiplier;     // 1.5^챌린지수
    public int finalScore;       // 기본 × 배수
}
