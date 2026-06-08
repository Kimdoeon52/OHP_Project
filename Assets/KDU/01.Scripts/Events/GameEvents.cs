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

    // 상호작용 (튜토리얼 등 구독)
    public static Action<Vector2Int> OnChestOpened;         // 상자 개방 셀
    public static Action<KeyType> OnKeyAcquired;            // 획득한 열쇠 종류

    // 탐색 액션 — center 기준 체비셰프 radius 안의 숨겨진 것(문/함정 등)이 구독해 드러남.
    public static Action<Vector2Int, int> OnSearchPerformed; // center, radius (1 = 3x3)
    public static Action<Vector2Int> OnSecretDiscovered;     // 숨겨진 문/함정이 실제로 드러난 셀

    // 상태이상
    public static Action<StatusRequest> OnStatusEffectApplied;

    // 시야
    public static Action<IReadOnlyCollection<Vector2Int>, IReadOnlyCollection<Vector2Int>> OnVisionUpdated; // visible, explored

    // 게임 상태
    public static Action OnPlayerDied;
    public static Action OnLevelComplete;
    public static Action OnBossDefeated;    // 보스 처치 — 맵 출구 개방 등이 구독

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
        OnChestOpened = null;
        OnKeyAcquired = null;
        OnSearchPerformed = null;
        OnSecretDiscovered = null;
        OnStatusEffectApplied = null;
        OnVisionUpdated = null;
        OnPlayerDied = null;
        OnLevelComplete = null;
        OnBossDefeated = null;
        CombatTextCriticalContext.Clear();
    }
}
