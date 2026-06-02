using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 행동 → 적 턴 처리 흐름 관리.
/// 적/함정은 Register()로 등록, 사망 시 Unregister().
/// </summary>
public class TurnManager : Singleton<TurnManager>
{
    // 적 턴 사이 딜레이 (애니메이션 여유 시간)
    [SerializeField] private float enemyTurnDelay = 0.05f;

    private readonly List<IGridEntity> _entities = new();
    private bool _processingTurn;

    public bool IsPlayerTurn { get; private set; } = true;
    public float LastPlayerActionTimeCost { get; private set; } = 1.0f;


    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        // 플레이어 캐릭터 생성 대기 후 이동 성공(시작) 이벤트 구독함 - 구조가 바뀌면서 필요없어짐 혹시 몰라 일단 놔둠
        // StartCoroutine(SetupPlayerMovementSubscription());
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
        float totalCost = player.Config.moveTimeDelay;
        var status = player.GetComponent<StatusController>();
        if (status != null)
        {
            totalCost = status.ModifyMoveTimeCost(totalCost);
        }

        bool skipEnemyTurnDelay = false;
       //  OnPlayerActionCompleted(totalCost, skipEnemyTurnDelay);
    }


    private static bool IsEntityAlive(IGridEntity entity)
    {
        if (entity == null) return false;

        if(entity is UnityEngine.Object unityObject)
        return unityObject != null;

        return true;
    }

    public void Register(IGridEntity entity)
    {
        if(!IsEntityAlive(entity)) return;


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
            if(!IsEntityAlive(e))
            {
                _entities.RemoveAt(i);
                i--;
                continue;
            }

            // 2x2 이상 대형 보스는 GridPosition 하나만 비교하면 몸통 일부 칸을 공격 대상으로 찾을 수 없다.
            // IMultiCellGridEntity를 구현한 엔티티는 OccupiedCells 전체를 검사해서,
            // 플레이어가 대형 보스의 어느 칸을 공격해도 같은 보스를 반환한다.
            if (e is IMultiCellGridEntity multiCellEntity)
            {
                IEnumerable<Vector2Int> occupiedCells = multiCellEntity.OccupiedCells;
                if (occupiedCells != null)
                {
                    foreach (Vector2Int occupiedCell in occupiedCells)
                    {
                        if (occupiedCell == pos)
                            return e;
                    }
                }
            }

            if (e != null && e.GridPosition == pos) 
            return e;
        }
        return null;
    }

    /// <summary>
    /// 플레이어 행동 완료 시 호출. timeCost는 남은 시간 차감량이 아니라 다음 행동까지의 딜레이 값이다.
    /// </summary>
    public void OnPlayerActionCompleted(float timeCost)
    {
        OnPlayerActionCompleted(timeCost, false);
    }

    public void OnPlayerActionCompletedWithoutEnemyTurns(float timeCost)
    {
        CompletePlayerAction(timeCost, false, false);
    }

    /// <summary>
    /// 플레이어 행동 완료 후 행동 딜레이 알림과 적 턴을 처리한다.
    /// skipEnemyTurnDelay가 true면 적 행동 자체는 유지하되, 고속 이동이 끊겨 보이지 않도록 턴 사이 대기만 생략한다.
    /// </summary>
    public void OnPlayerActionCompleted(float timeCost, bool skipEnemyTurnDelay)
    {
        CompletePlayerAction(timeCost, skipEnemyTurnDelay, true);
    }

    private void CompletePlayerAction(float timeCost, bool skipEnemyTurnDelay, bool processEnemyTurns)
    {
        if (_processingTurn) return;

        LastPlayerActionTimeCost = timeCost;

        GameEvents.OnPlayerActionCompleted?.Invoke(timeCost);

        // 행동 후 시간 감소 기획의도와 달라져 주석으로 처리함.
        // timeCost는 이제 TimeSystem을 깎는 값이 아니라 플레이어 행동 딜레이로만 사용한다.
        // TimeSystem.Instance?.ConsumeTime(timeCost);

        // 시간 소진으로 이미 게임오버 처리된 경우 적 턴 차단함 (세이프룸/테스트씬 등 타이머 정지 중에도 적 행동 허용)
        if (TimeSystem.Instance != null && TimeSystem.Instance.TimeRemaining <= 0f) return;
        if (!processEnemyTurns) return;

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
            if (!IsEntityAlive(entity)) continue;

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
