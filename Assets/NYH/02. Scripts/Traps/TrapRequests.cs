using UnityEngine;

public enum StatusType
{
    Poison = 0,     // 독
    Bleed = 1,      // 출혈
    Cripple = 2,    // 불구 / 절름발이 / 이동 불능 계열
    Blind = 3,      // 실명
    Weakness = 4,   // 약화
    Curse = 5,      // 저주
    Ooze = 6,       // 점액 / 끈적임
    Chill = 7,      // 냉기
    Burn = 8,       // 화상
    Shock = 9,      // 감전
    Confusion = 10, // 혼란
    Corrosion = 11, // 부식
    Paralysis = 12, // 마비
    Fall = 13,      // 추락
    Slow = 14,      // 둔화
    Hunger = 15,    // 굶주림
    Float = 16,     // 부유
    Haste = 17      // 신속
}

public enum TrapTerrainType
{
    Chill,
    Fire,
    Shock,
    Water,
    Pit,
    Gas,
    Ooze
}

public enum TrapSpawnType
{
    Alarm,
    Guardian,
    Flock,
    Summoning,
    Distortion
}

public readonly struct StatusRequest
{
    public readonly IGridEntity Target;
    public readonly Vector2Int TargetPosition;
    public readonly StatusType StatusType;
    public readonly int DurationTurns;
    public readonly int Power;
    public readonly GridTrap SourceTrap;

    public StatusRequest(IGridEntity target, Vector2Int targetPosition, StatusType statusType, int durationTurns, int power, GridTrap sourceTrap)
    {
        Target = target;
        TargetPosition = targetPosition;
        StatusType = statusType;
        DurationTurns = durationTurns;
        Power = power;
        SourceTrap = sourceTrap;
    }
}

public readonly struct TrapTerrainRequest
{
    public readonly Vector2Int Origin;
    public readonly int Radius;
    public readonly int DurationTurns;
    public readonly TrapTerrainType TerrainType;
    public readonly GridTrap SourceTrap;

    public TrapTerrainRequest(Vector2Int origin, int radius, int durationTurns, TrapTerrainType terrainType, GridTrap sourceTrap)
    {
        Origin = origin;
        Radius = radius;
        DurationTurns = durationTurns;
        TerrainType = terrainType;
        SourceTrap = sourceTrap;
    }
}

public readonly struct TrapSpawnRequest
{
    public readonly Vector2Int Origin;
    public readonly TrapSpawnType SpawnType;
    public readonly int Count;
    public readonly int Radius;
    public readonly GridTrap SourceTrap;

    public TrapSpawnRequest(Vector2Int origin, TrapSpawnType spawnType, int count, int radius, GridTrap sourceTrap)
    {
        Origin = origin;
        SpawnType = spawnType;
        Count = count;
        Radius = radius;
        SourceTrap = sourceTrap;
    }
}

public readonly struct TrapStubRequest
{
    public readonly Vector2Int Origin;
    public readonly string Message;
    public readonly GridTrap SourceTrap;

    public TrapStubRequest(Vector2Int origin, string message, GridTrap sourceTrap)
    {
        Origin = origin;
        Message = message;
        SourceTrap = sourceTrap;
    }
}
