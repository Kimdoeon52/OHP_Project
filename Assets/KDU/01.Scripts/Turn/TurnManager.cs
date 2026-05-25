using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 행동 → 적 턴 처리 흐름 관리.
/// 적/함정은 Register()로 등록, 사망 시 Unregister().
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    // 적 턴 사이 딜레이 (애니메이션 여유 시간)
    [SerializeField] private float enemyTurnDelay = 0.05f;

    private readonly List<IGridEntity> _entities = new();
    private bool _processingTurn;

    public bool IsPlayerTurn { get; private set; } = true;
    public float LastPlayerActionTimeCost { get; private set; } = 1.0f;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("TurnManager가 씬에 두 개 이상 있습니다.");
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 플레이어 캐릭터 생성 대기 후 이동 성공(시작) 이벤트 구독함
        StartCoroutine(SetupPlayerMovementSubscription());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 플레이어 캐릭터의 이동 컴포넌트를 탐색하여 이동 성공(시작) 이벤트를 동적으로 구독함.
    /// </summary>
    private IEnumerator SetupPlayerMovementSubscription()
    {
        GridPlayer player = null;
        while (player == null)
        {
            player = FindFirstObjectByType<GridPlayer>();
            yield return null;
        }

        if (player.Movement != null)
        {
            player.Movement.OnMoveSucceeded += OnPlayerMoveSucceeded;
        }
    }

    /// <summary>
    /// 플레이어가 이동을 시작했을 때 호출되는 콜백임.
    /// 동시성 충돌을 방지하기 위해 즉시 실행하지 않고 도착할 때까지 기다리는 코루틴을 시작함.
    /// </summary>
    private void OnPlayerMoveSucceeded(Vector2Int direction)
    {
        StartCoroutine(WaitAndCompletePlayerMove());
    }

    /// <summary>
    /// 플레이어가 물리적으로 목표 셀에 안착할 때까지 대기한 후 턴 마감 처리를 완료시킴.
    /// </summary>
    private IEnumerator WaitAndCompletePlayerMove()
    {
        var player = FindFirstObjectByType<GridPlayer>();
        if (player == null) yield break;

        // 플레이어 이동 보간 시간(CurrentMoveDuration)만큼 대기함
        float duration = player.Movement.CurrentMoveDuration;
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        // 이동 완료 후 시간 비용 계산
        float totalCost = player.Config.moveTimeCost;
        var status = player.GetComponent<StatusController>();
        if (status != null)
        {
            totalCost = status.ModifyMoveTimeCost(totalCost);
        }

        bool skipEnemyTurnDelay = player.Movement.ShouldSkipEnemyTurnDelay();
        OnPlayerActionCompleted(totalCost, skipEnemyTurnDelay);
    }

    public void Register(IGridEntity entity)
    {
        if (!_entities.Contains(entity))
            _entities.Add(entity);
    }

    public void Unregister(IGridEntity entity)
    {
        _entities.Remove(entity);
    }

    /// <summary>
    /// 해당 격자 셀에 등록된 엔티티 반환 (단검 충돌 판정 등에 사용). 없으면 null.
    /// </summary>
    public IGridEntity GetEntityAt(Vector2Int pos)
    {
        for (int i = 0; i < _entities.Count; i++)
        {
            var e = _entities[i];
            if (e != null && e.GridPosition == pos) return e;
        }
        return null;
    }

    /// <summary>
    /// 플레이어 행동 완료 시 호출. 시간 소모 후 적 턴 시작.
    /// </summary>
    public void OnPlayerActionCompleted(float timeCost)
    {
        OnPlayerActionCompleted(timeCost, false);
    }

    /// <summary>
    /// 플레이어 행동 완료 후 시간 소모와 적 턴을 처리한다.
    /// skipEnemyTurnDelay가 true면 적 행동 자체는 유지하되, 고속 이동이 끊겨 보이지 않도록 턴 사이 대기만 생략한다.
    /// </summary>
    public void OnPlayerActionCompleted(float timeCost, bool skipEnemyTurnDelay)
    {
        if (_processingTurn) return;

        LastPlayerActionTimeCost = timeCost;

        GameEvents.OnPlayerActionCompleted?.Invoke(timeCost);
        TimeSystem.Instance?.ConsumeTime(timeCost);

        // 시간 소진으로 이미 게임오버 처리된 경우 적 턴 차단함 (세이프룸/테스트씬 등 타이머 정지 중에도 적 행동 허용)
        if (TimeSystem.Instance != null && TimeSystem.Instance.TimeRemaining <= 0f) return;

        StartCoroutine(ProcessEnemyTurns(skipEnemyTurnDelay));
    }

    private IEnumerator ProcessEnemyTurns(bool skipEnemyTurnDelay)
    {
        _processingTurn = true;
        IsPlayerTurn = false;

        // 리스트 복사 — 턴 도중 Unregister가 일어나도 안전
        var snapshot = new List<IGridEntity>(_entities);
        foreach (var entity in snapshot)
        {
            if (entity == null) continue;
            entity.OnTurnUpdate();
        }

        // 개별 대기 대신, 전체 적의 행동 업데이트가 끝난 뒤 단 한 번만 대기하여 렉 방지함
        if (!skipEnemyTurnDelay && enemyTurnDelay > 0f)
            yield return new WaitForSeconds(enemyTurnDelay);

        // 죽은 엔티티 정리
        _entities.RemoveAll(e => e == null);

        _processingTurn = false;
        IsPlayerTurn = true;
    }
}
