using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameEvents
{
    // 타이머
    public static Action<float> OnTimeChanged;
    public static Action OnTimeUp;
    public static Action<float> OnPlayerActionCompleted;

    // 전투
    public static Action<Vector2Int, float> OnEnemyKilled;  // pos, timeBonus
    public static Action<int> OnPlayerDamaged;              // damage amount
    public static Action<CombatTextRequest> OnCombatTextRequested;
    public static Action<Vector2Int> OnTrapTriggered;
    public static Action<StatusRequest> OnStatusRequested;
    public static Action<TrapTerrainRequest> OnTrapTerrainRequested;
    public static Action<TrapSpawnRequest> OnTrapSpawnRequested;
    public static Action<TrapStubRequest> OnTrapStubRequested;

    // 보상 드롭
    public static Action<Vector2Int, int> OnExpDropped;     // pos, exp
    public static Action<Vector2Int, int> OnGoldDropped;    // pos, gold

    // 상태이상
    public static Action<StatusRequest> OnStatusEffectApplied;

    // 시야
    public static Action<IReadOnlyCollection<Vector2Int>, IReadOnlyCollection<Vector2Int>> OnVisionUpdated; // visible, explored

    // 게임 상태
    public static Action OnPlayerDied;
    public static Action OnLevelComplete;

    public static void ClearAllEvents()
    {
        OnTimeChanged = null;
        OnTimeUp = null;
        OnPlayerActionCompleted = null;
        OnEnemyKilled = null;
        OnPlayerDamaged = null;
        OnCombatTextRequested = null;
        OnTrapTriggered = null;
        OnStatusRequested = null;
        OnTrapTerrainRequested = null;
        OnTrapSpawnRequested = null;
        OnTrapStubRequested = null;
        OnExpDropped = null;
        OnGoldDropped = null;
        OnStatusEffectApplied = null;
        OnVisionUpdated = null;
        OnPlayerDied = null;
        OnLevelComplete = null;
        CombatTextCriticalContext.Clear();
    }
}

public enum CombatTextType
{
    Damage,
    CriticalDamage,
    Heal,
    Miss
}

public readonly struct CombatTextRequest
{
    public readonly Vector3 WorldPosition;
    public readonly string Text;
    public readonly int Amount;
    public readonly CombatTextType Type;

    public CombatTextRequest(Vector3 worldPosition, string text, int amount, CombatTextType type)
    {
        WorldPosition = worldPosition;
        Text = text;
        Amount = amount;
        Type = type;
    }
}

public static class CombatTextCriticalContext
{
    public static int CurrentMaxDamage { get; private set; }

    public static void SetCurrentMaxDamage(int maxDamage)
    {
        CurrentMaxDamage = Mathf.Max(0, maxDamage);
    }

    public static void Clear()
    {
        CurrentMaxDamage = 0;
    }
}
