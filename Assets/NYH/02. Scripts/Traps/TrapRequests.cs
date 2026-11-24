using UnityEngine;

public enum StatusType
{
    Poison = 0,     // ��
    Bleed = 1,      // ����
    Cripple = 2,    // �ұ� / �������� / �̵� �Ҵ� �迭
    Blind = 3,      // �Ǹ�
    Weakness = 4,   // ��ȭ
    Curse = 5,      // ����
    Ooze = 6,       // ���� / ������
    Chill = 7,      // �ñ�
    Burn = 8,       // ȭ��
    Shock = 9,      // ����
    Confusion = 10, // ȥ��
    Corrosion = 11, // �ν�
    Paralysis = 12, // ����
    Fall = 13,      // �߶�
    Slow = 14,      // ��ȭ
    Hunger = 15,    // ���ָ�
    Float = 16,     // ����
    Haste = 17,     // 신속
    Purified = 18   // 정화 (Poison, Slow 면역)
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

public readonly struct StatusRequest
{
    public readonly IGridEntity Target;
    public readonly Vector2Int TargetPosition;
    public readonly StatusType StatusType;
    public readonly int DurationTurns;
    public readonly int Power;

    public StatusRequest(IGridEntity target, Vector2Int targetPosition, StatusType statusType, int durationTurns, int power)
    {
        Target = target;
        TargetPosition = targetPosition;
        StatusType = statusType;
        DurationTurns = durationTurns;
        Power = power;
    }
}

public readonly struct TrapTerrainRequest
{
    public readonly Vector2Int Origin;
    public readonly int Radius;
    public readonly int DurationTurns;
    public readonly TrapTerrainType TerrainType;

    public TrapTerrainRequest(Vector2Int origin, int radius, int durationTurns, TrapTerrainType terrainType)
    {
        Origin = origin;
        Radius = radius;
        DurationTurns = durationTurns;
        TerrainType = terrainType;
    }
}
