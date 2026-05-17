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
    /// 보간 이동이 끝나고 목표 칸 중심에 도착했을 때 호출된다.
    /// 사용 예: 애니메이션 컨트롤러가 이 이벤트를 구독해서 걷기 애니메이션을 끄고 Idle로 돌린다.
    /// </summary>
    public event Action OnMoveFinished;

    /// <summary>
    /// 이동속도 스탯에 따라 이번 이동의 애니메이션 배속이 바뀔 때 호출된다.
    /// 사용 예: 애니메이션 컨트롤러가 Animator.speed에 이 값을 적용한다.
    /// </summary>
    public event Action<float> OnMoveSpeedChanged;

    [Header("Grid Move Animation")]
    [Tooltip("MoveSpeed가 0일 때 한 칸 이동에 걸리는 기본 시간(초).")]
    [SerializeField, Min(0.01f)] private float baseMoveDuration = 0.18f;

    [Tooltip("MoveSpeed 1당 이동 시간을 얼마나 줄일지 정하는 계수. 공식: duration = base / (1 + MoveSpeed * multiplier)")]
    [SerializeField, Min(0f)] private float moveSpeedDurationMultiplier = 0.15f;

    [Tooltip("MoveSpeed가 너무 높아도 이 시간보다 빠르게 이동하지 않게 하는 하한값.")]
    [SerializeField, Min(0.01f)] private float minMoveDuration = 0.06f;

    public Vector2Int Facing { get; private set; } = Vector2Int.right;

    /// <summary>
    /// 그리드에서 다음 그리드로 넘어가는 동안 true가 된다.
    /// 이 값이 이동 중 입력 잠금의 기준이다.
    /// </summary>
    public bool IsMoving { get; private set; }

    /// <summary>
    /// 현재 진행 중인 한 칸 이동에 실제 적용된 시간.
    /// 디버그 UI나 이동감 튜닝 확인용으로 읽을 수 있다.
    /// </summary>
    public float CurrentMoveDuration { get; private set; }

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
        OnMoveSpeedChanged?.Invoke(GetAnimationSpeedMultiplier(CurrentMoveDuration));
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

        IsMoving = false;
        _moveCoroutine = null;
        OnMoveFinished?.Invoke();
        OnMoveSpeedChanged?.Invoke(1f);

        TurnManager.Instance.OnPlayerActionCompleted(totalCost);
    }

    
    private float GetMoveDuration()
    {
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;

        int moveSpeed = _stats != null ? _stats.MoveSpeed : 0;

        // 이동 대기시간 공식:
        // MoveSpeed가 높을수록 speedFactor가 커지고, 한 칸 이동 시간이 짧아진다.
        // 예: base 0.18, multiplier 0.15, MoveSpeed 1 => 0.18 / 1.15초
        float speedFactor = 1f + Mathf.Max(0, moveSpeed) * moveSpeedDurationMultiplier;
        return Mathf.Max(minMoveDuration, baseMoveDuration / speedFactor);
    }

    private float GetAnimationSpeedMultiplier(float moveDuration)
    {
        if (moveDuration <= 0f) return 1f;

        // 이동 시간이 짧아진 만큼 걷기 애니메이션도 빨라져야 발이 늦게 따라오지 않는다.
        return Mathf.Max(1f, baseMoveDuration / moveDuration);
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
