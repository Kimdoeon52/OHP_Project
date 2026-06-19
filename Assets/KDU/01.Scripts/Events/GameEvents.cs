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
    public static Action<int> OnGoldChanged;                // 지갑 잔액 변동 후 현재 잔액. HUD 가 구독.
    public static Action<ItemBase> OnItemPurchased;         // 상점 구매 성공한 상품. 사운드/연출 구독.

    // 상호작용
    public static Action<Vector2Int> OnChestOpened;         // 상자 개방 셀
    public static Action<KeyType> OnKeyAcquired;            // 획득한 열쇠 종류

    // 탐색 액션
    public static Action<Vector2Int, int> OnSearchPerformed; // center, radius (1 = 3x3)
    public static Action<Vector2Int> OnSecretDiscovered;     // 숨겨진 문/함정이 실제로 드러난 셀

    // 상태이상
    public static Action<StatusRequest> OnStatusEffectApplied;

    // 시야
    public static Action<IReadOnlyCollection<Vector2Int>, IReadOnlyCollection<Vector2Int>> OnVisionUpdated; // visible, explored

    // 게임 상태
    public static Action OnFloorReset;                // 새 층 생성 직전 — 바닥 아이템 정리 등이 구독
    public static Action OnPlayerDied;                // 플레이어 사망
    public static Action OnLevelComplete;             // 레벨업
    public static Action OnBossDefeated;              // 보스 처치 — 맵 출구 개방 등이 구독
    public static Action OnRunCleared;                // 최종층 클리어 — 승리/엔딩이 구독
    public static Action<ScoreResult> OnScoreFinalized; // 런 종료 점수 확정 — 결산 UI가 구독

    // 오디오
    public static Action<AudioEventId> OnAudioEventRequested;

    public static void ClearAllEvents()
    {
        OnTimeChanged = null;               // 타이머
        OnTimeUp = null;                    // 타이머 종료
        OnPlayerActionCompleted = null;     // 플레이어 행동 완료
        OnEnemyKilled = null;               // 적 처치
        OnPlayerDamaged = null;             // 플레이어 피해
        OnCombatTextRequested = null;       // 전투 텍스트
        OnTrapTriggered = null;             // 함정 발동
        OnStatusRequested = null;           // 상태
        OnTrapTerrainRequested = null;      // 함정 지형
        OnTrapSpawnRequested = null;        // 함정 생성
        OnTrapStubRequested = null;         // 함정 미리보기
        OnExpDropped = null;                // 경험치 드롭
        OnGoldDropped = null;               // 골드 드롭
        OnGoldChanged = null;               // 골드 잔액 변동
        OnItemPurchased = null;             // 상점 구매
        OnChestOpened = null;               // 상자 개방
        OnKeyAcquired = null;               // 열쇠 획득
        OnSearchPerformed = null;           // 탐색 액션
        OnSecretDiscovered = null;          // 숨겨진 요소 발견
        OnStatusEffectApplied = null;       // 상태이상 적용
        OnVisionUpdated = null;             // 시야 업데이트
        OnFloorReset = null;                // 새 층 생성 직전
        OnPlayerDied = null;                // 플레이어 사망
        OnLevelComplete = null;             // 레벨업
        OnBossDefeated = null;              // 보스 처치
        OnRunCleared = null;                // 최종층 클리어
        OnScoreFinalized = null;            // 점수 확정
        OnAudioEventRequested = null;        // 오디오 이벤트
        CombatTextCriticalContext.Clear();  // 전투 텍스트 초기화
    }
}
