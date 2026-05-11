using UnityEngine;

public enum StatusType
{
    Poison,
    Bleed,
    Cripple,
    Blind,
    Weakness,
    Curse,
    Ooze,
    Chill,
    Burn,
    Shock,
    Confusion,
    Corrosion,
    Paralysis,
    Fall
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
