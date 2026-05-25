using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어의 4방향 그리드 이동을 담당한다.
/// 논리 위치(GridPosition)는 이동 시작 시점에 먼저 바꾸고,
/// 실제 Transform은 코루틴으로 다음 칸 중심까지 보간한다.
/// </summary>
public class GridPlayerMovement : MonoBehaviour
{
    /// <summary>
    /// 이동이 실제로 시작됐을 때 호출된다.
    /// 사용 예: 애니메이션 컨트롤러가 이 이벤트를 구독해서 걷기 애니메이션을 켠다.
    /// </summary>
    public event Action<Vector2Int> OnMoveSucceeded;

    /// <summary>
    /// 보간 이동과 도착 후 추가 대기시간이 모두 끝났을 때 호출된다.
    /// 사용 예: 애니메이션 컨트롤러가 이 이벤트를 구독해서 걷기 애니메이션을 끄고 Idle로 돌린다.
    /// </summary>
    public event Action OnMoveFinished;

    /// <summary>
    /// 이동속도 스탯에 따라 이번 이동의 애니메이션 배속이 바뀔 때 호출된다.
    /// 사용 예: 애니메이션 컨트롤러가 Animator.speed에 이 값을 적용한다.
    /// </summary>
    public event Action<float> OnMoveSpeedChanged;

    [Header("Grid Move Duration")]
    [Tooltip("MoveSpeed가 0일 때 한 칸 이동에 걸리는 기본 시간(초).")]
    [SerializeField, Min(0.01f)] private float baseMoveDuration = 0.18f;

    [Tooltip("MoveSpeed 1당 이동 시간이 줄어드는 계수. 공식: duration = base / (1 + MoveSpeed * multiplier)")]
    [SerializeField, Min(0f)] private float moveSpeedDurationMultiplier = 0.15f;

    [Tooltip("MoveSpeed가 높아도 한 칸 이동 시간이 이 값보다 짧아지지 않게 한다.")]
    [SerializeField, Min(0.01f)] private float minMoveDuration = 0.06f;

    [Header("Post Move Delay")]
    [Tooltip("한 칸 도착 후 다음 행동까지 기다리는 추가 대기시간. 기본 0이면 현재 조작감을 유지한다.")]
    [SerializeField, Min(0f)] private float basePostMoveDelay = 0f;

    [Tooltip("MoveSpeed가 이 값 이상이면 도착 후 추가 대기시간을 0으로 강제한다.")]
    [SerializeField, Min(0)] private int zeroPostMoveDelaySpeed = 10;

    [Header("Animation Speed")]
    [Tooltip("걷기 애니메이션 배속 계산에 반영할 최대 MoveSpeed. 실제 이동속도는 이 값 이후에도 계속 오른다.")]
    [SerializeField, Min(0)] private int animationSpeedCapMoveSpeed = 5;

    public Vector2Int Facing { get; private set; } = Vector2Int.right;

    /// <summary>
    /// 그리드에서 다음 그리드로 넘어가거나 도착 후 추가 대기시간을 처리하는 동안 true가 된다.
    /// 이 값이 이동 중 입력 잠금의 기준이다.
    /// </summary>
    public bool IsMoving { get; private set; }

    /// <summary>
    /// 현재 진행 중인 한 칸 이동에 실제 적용된 보간 시간.
    /// 디버그 UI나 이동감 튜닝 확인용으로 읽을 수 있다.
    /// </summary>
    public float CurrentMoveDuration { get; private set; }

    /// <summary>
    /// 현재 진행 중인 이동에 적용된 도착 후 추가 대기시간.
    /// MoveSpeed 10 이상이면 0이 된다.
    /// </summary>
    public float CurrentPostMoveDelay { get; private set; }

    private GridPlayer _player;
    private NYHPlayerStats _stats;
    private Coroutine _moveCoroutine;

    public void Init(GridPlayer player)
    {
        _player = player;
        _stats = GetComponent<NYHPlayerStats>();
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;
    }

    /// <summary>
    /// 공격처럼 실제 이동 없이 바라보는 방향만 바꿔야 할 때 사용한다.
    /// 외부 호출 예: GridPlayerCombat이 공격 방향을 맞출 때 호출한다.
    /// </summary>
    public void SetFacing(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return;
        Facing = dir;
    }

    /// <summary>
    /// MoveSpeed가 기준 이상이면 적 행동은 유지하되, 적 턴 사이 연출 대기만 생략한다.
    /// 실제 한 칸 이동 시간은 GetMoveDuration()에서 계속 계산한다.
    /// </summary>
    public bool ShouldSkipEnemyTurnDelay()
    {
        return GetMoveSpeed() >= zeroPostMoveDelaySpeed;
    }

    /// <summary>
    /// 이동 종료 직후에도 걷기 애니메이션을 유지해도 되는지 판단한다.
    /// 키를 누르고 있어도 다음 칸이 벽이면 걷기 모션이 남지 않도록 walkable을 다시 확인한다.
    /// </summary>
    public bool ShouldKeepMoveAnimationAfterMove()
    {
        if (!ShouldSkipEnemyTurnDelay()) return false;
        if (_player == null || _player.Input == null) return false;
        if (GridSystem.Instance == null) return false;
        if (!_player.Input.TryGetHeldMoveDirection(out Vector2Int heldDirection)) return false;

        Vector2Int nextCell = _player.GridPosition + heldDirection;
        return GridSystem.Instance.IsWalkable(nextCell);
    }

    /// <summary>
    /// 플레이어를 direction 방향의 다음 그리드로 이동시킨다.
    /// 외부 호출 예: GridPlayerInput이 WASD/방향키 입력을 받아 호출한다.
    /// 이동 중(IsMoving == true)에는 새 이동을 무시해서 위치와 턴 처리가 꼬이지 않게 한다.
    /// </summary>
    public void TryMove(Vector2Int direction)
    {
        // 그리드 이동 중 입력 잠금은 여기서 1차로 걸린다.
        // GridPlayerInput에서도 IsMoving을 보고 입력 자체를 막아 중복 호출을 줄인다.
        if (IsMoving) return;

        Facing = direction;

        Vector2Int target = _player.GridPosition + direction;

        // 그리드 이동 중 공격 가능 entity가 있다면 이동을 안하고 target을 공격 한다.
        IGridEntity entity = TurnManager.Instance.GetEntityAt(target);
        if (entity is IDamageable damageable && (UnityEngine.Object)damageable != _player)
        {
            _player.Combat.TryMeleeAt(target);
            return;
        }
        if (!GridSystem.Instance.IsWalkable(target))
            return;
        float totalCost = _player.Config.moveTimeCost;
        StatusController statusController = _player.GetComponent<StatusController>();
        if (statusController != null)
            totalCost = statusController.ModifyMoveTimeCost(totalCost);

        if (_player.Combat.TryRetrieveAt(target))
            totalCost += _player.Config.retrieveTimeCost;

        Vector3 startWorld = transform.position;
        _player.SetGridPosition(target);
        Vector3 targetWorld = transform.position;

        // EntityBase.SetGridPosition은 즉시 순간이동하므로, 화면 위치만 되돌린 뒤 직접 보간한다.
        transform.position = startWorld;

        _moveCoroutine = StartCoroutine(MoveToCell(direction, target, startWorld, targetWorld, totalCost));
    }

    private IEnumerator MoveToCell(Vector2Int direction, Vector2Int target, Vector3 startWorld, Vector3 targetWorld, float totalCost)
    {
        IsMoving = true;
        CurrentMoveDuration = GetMoveDuration();
        CurrentPostMoveDelay = GetPostMoveDelay();

        OnMoveSpeedChanged?.Invoke(GetAnimationSpeedMultiplier());
        OnMoveSucceeded?.Invoke(direction);

        float elapsed = 0f;
        while (elapsed < CurrentMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / CurrentMoveDuration);
            transform.position = Vector3.Lerp(startWorld, targetWorld, t);
            yield return null;
        }

        transform.position = targetWorld;

        // 도착 후에 턴을 넘겨 적 행동이 플레이어 이동 연출 중 끼어들지 않게 한다.
        TrapSystem.Instance?.TryTriggerAt(target, _player);
        VisionSystem.Instance.UpdateVision(_player.GridPosition);

        if (CurrentPostMoveDelay > 0f)
            yield return new WaitForSeconds(CurrentPostMoveDelay);

        bool keepMoveAnimation = ShouldKeepMoveAnimationAfterMove();
        bool skipEnemyTurnDelay = ShouldSkipEnemyTurnDelay();

        IsMoving = false;
        _moveCoroutine = null;

        if (!keepMoveAnimation)
        {
            OnMoveFinished?.Invoke();
            OnMoveSpeedChanged?.Invoke(1f);
        }

        // 행동 후 시간 감소 
         TurnManager.Instance.OnPlayerActionCompleted(totalCost, skipEnemyTurnDelay);
    }

    private float GetMoveDuration()
    {
        int moveSpeed = GetMoveSpeed();

        // 실제 그리드 이동 시간 공식:
        // MoveSpeed가 5를 넘어도 계속 전체 수치를 반영한다.
        float speedFactor = 1f + Mathf.Max(0, moveSpeed) * moveSpeedDurationMultiplier;
        return Mathf.Max(minMoveDuration, baseMoveDuration / speedFactor);
    }

    private float GetPostMoveDelay()
    {
        int moveSpeed = GetMoveSpeed();
        if (moveSpeed >= zeroPostMoveDelaySpeed)
            return 0f;

        if (basePostMoveDelay <= 0f)
            return 0f;

        // 도착 후 추가 대기시간 공식:
        // MoveSpeed가 zeroPostMoveDelaySpeed에 가까워질수록 0에 가까워진다.
        float normalizedSpeed = zeroPostMoveDelaySpeed <= 0 ? 1f : Mathf.Clamp01((float)moveSpeed / zeroPostMoveDelaySpeed);
        return basePostMoveDelay * (1f - normalizedSpeed);
    }

    private float GetAnimationSpeedMultiplier()
    {
        int cappedMoveSpeed = Mathf.Min(GetMoveSpeed(), animationSpeedCapMoveSpeed);

        // 애니메이션 배속 공식:
        // MoveSpeed 5 이후에는 cappedMoveSpeed가 고정되어 걷기 애니메이션이 더 빨라지지 않는다.
        float animationDuration = GetDurationForMoveSpeed(cappedMoveSpeed);
        if (animationDuration <= 0f) return 1f;

        return Mathf.Max(1f, baseMoveDuration / animationDuration);
    }

    private float GetDurationForMoveSpeed(int moveSpeed)
    {
        float speedFactor = 1f + Mathf.Max(0, moveSpeed) * moveSpeedDurationMultiplier;
        return Mathf.Max(minMoveDuration, baseMoveDuration / speedFactor);
    }

    private int GetMoveSpeed()
    {
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;

        return _stats != null ? _stats.MoveSpeed : 0;
    }

    private void OnDisable()
    {
        if (_moveCoroutine == null) return;

        StopCoroutine(_moveCoroutine);
        _moveCoroutine = null;
        IsMoving = false;
        OnMoveFinished?.Invoke();
        OnMoveSpeedChanged?.Invoke(1f);
    }
}
