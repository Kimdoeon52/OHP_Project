using System;
using System.Collections;
using UnityEngine;

public enum PlayerGridMoveMode
{
    LegacyActionDelay,
    ContinuousGridMove
}

/// <summary>
/// 플레이어의 4방향 그리드 이동을 담당합니다.
/// 논리 위치(GridPosition)를 이동 시작 시점에 먼저 바꾸고,
/// 실제 Transform은 코루틴으로 다음 칸 중심까지 부드럽게 이동(보간)시킵니다.
/// </summary>
public class GridPlayerMovement : MonoBehaviour
{
    /// <summary>
    /// 이동을 실제로 시작했을 때 호출됩니다.
    /// 사용 예: 애니메이션 컨트롤러가 이 이벤트를 구독해서 걷기 애니메이션을 켭니다.
    /// </summary>
    public event Action<Vector2Int> OnMoveSucceeded;

    /// <summary>
    /// 보간 이동과 도착 후 추가 대기시간이 모두 끝났을 때 호출됩니다.
    /// 사용 예: 애니메이션 컨트롤러가 이 이벤트를 구독해서 걷기 애니메이션을 끄고 Idle로 돌아갑니다.
    /// </summary>
    public event Action OnMoveFinished;

    /// <summary>
    /// 이동속도 스탯에 따라 이번 이동의 애니메이션 배속이 바뀔 때 호출됩니다.
    /// 사용 예: 애니메이션 컨트롤러가 Animator.speed에 이 값을 적용합니다.
    /// </summary>
    public event Action<float> OnMoveSpeedChanged;

    [Header("Grid Move Duration")]
    [Tooltip("MoveSpeed가 0일 때 한 칸 이동에 걸리는 기본 시간(초).")]
    [SerializeField, Min(0.01f)] private float baseMoveDuration = 0.18f;

    [Tooltip("MoveSpeed 1당 이동 시간이 줄어드는 계수. 공식: duration = base / (1 + MoveSpeed * multiplier)")]
    [SerializeField, Min(0f)] private float moveSpeedDurationMultiplier = 0.15f;

    [Tooltip("MoveSpeed가 높아져도 한 칸 이동 시간이 이 값보다 짧아지지 않게 합니다.")]
    [SerializeField, Min(0.0001f)] private float minMoveDuration = 0.001f;

    [Header("Move Mode")]
    [SerializeField] private PlayerGridMoveMode moveMode = PlayerGridMoveMode.ContinuousGridMove;

    [Header("Move Action Delay")]
    [Tooltip("GDD 기준 최소 이동 행동 딜레이입니다. 레벨/MoveSpeed가 높아도 이 값보다 작아지지 않습니다.")]
    [SerializeField, Min(0.0005f)] private float minMoveActionDelay = 0.001f;

    [Tooltip("GDD 8.3 기준 레벨별 이동 행동 딜레이입니다. index 0은 1레벨, index 9는 10레벨입니다.")]
    [SerializeField] private float[] levelMoveActionDelays =
    {
        1.0f,
        0.9f,
        0.8f,
        0.7f,
        0.6f,
        0.5f,
        0.45f,
        0.4f,
        0.35f,
        0.3f
    };

    [Tooltip("이 MoveSpeed에 도달하면 이동 행동 딜레이가 minMoveActionDelay에 도달합니다. 기본값 10은 GDD의 10레벨 0.3초 기준입니다.")]
    [SerializeField, Min(1)] private int moveSpeedForMinActionDelay = 10;

    [Header("Animation Speed")]
    [Tooltip("걷기 애니메이션 배속 계산에 반영할 최대 MoveSpeed. 실제 이동속도는 이 값을 넘어도 계속 빨라집니다.")]
    [SerializeField, Min(0)] private int animationSpeedCapMoveSpeed = 5;

    [Header("Debug")]
    [Tooltip("이동할 때 레벨/상태/장비 보정이 적용된 최종 행동 대기시간을 Console에 출력합니다.")]
    [SerializeField] private bool logMoveActionDelay;

    /// <summary>현재 플레이어가 바라보고 있는 방향(상/하/좌/우)을 반환합니다.</summary>
    public Vector2Int Facing { get; private set; } = Vector2Int.right;

    /// <summary>
    /// 그리드에서 다음 그리드로 넘어가거나 도착 후 추가 대기시간을 처리하는 동안 true가 됩니다.
    /// 이 값이 이동 중 입력 잠금의 기준이 됩니다.
    /// </summary>
    public bool IsMoving { get; private set; }
    
    /// <summary>플레이어가 턴 기반 행동 대기열을 사용하는 레거시 모드인지 확인합니다.</summary>
    public bool UsesActionDelayForMovement => moveMode == PlayerGridMoveMode.LegacyActionDelay;
    
    /// <summary>플레이어가 대기열 없이 실시간으로 이동을 시도하는 모드인지 확인합니다.</summary>
    public bool IgnoresActionDelayForMovement => moveMode == PlayerGridMoveMode.ContinuousGridMove;

    /// <summary>
    /// 현재 진행 중인 한 칸 이동에 실제 적용된 보간 시간.
    /// 디버그 UI 등에서 이동값 변화를 확인할 때 읽을 수 있습니다.
    /// </summary>
    public float CurrentMoveDuration { get; private set; }

    private GridPlayer _player;
    private NYHPlayerStats _stats;
    private PlayerLevelSystem _levelSystem;

    // 이동 지연 시간의 마지막 보정 통로입니다.
    // MoveSpeed/레벨/상태이상 계산이 끝난 뒤, 신속의 반지나 둔화 저주처럼 "몇 초를 직접 빼거나 더하는 효과"를 여기에 태웁니다.
    private NYHFinalStatModifierController _finalStatModifiers;
    private Coroutine _moveCoroutine;

    /// <summary>
    /// 게임 시작 시 플레이어 본체(GridPlayer)가 이 컴포넌트를 초기화합니다.
    /// 이동에 필요한 스탯 시스템과 보정 컨트롤러 참조를 연결합니다.
    /// </summary>
    public void Init(GridPlayer player)
    {
        _player = player;
        _stats = GetComponent<NYHPlayerStats>();
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;

        _levelSystem = GetComponent<PlayerLevelSystem>();
        if (_levelSystem == null)
            _levelSystem = PlayerLevelSystem.Instance;

        _finalStatModifiers = GetComponent<NYHFinalStatModifierController>();
        if (_finalStatModifiers == null)
        {
            // 장비/특성/상태가 최종 행동 지연값을 한 통로로 보정할 수 있게 플레이어에 붙여둔다.
            _finalStatModifiers = gameObject.AddComponent<NYHFinalStatModifierController>();
        }
    }

    /// <summary>
    /// 공격처럼 실제 이동 없이 바라보는 방향만 바꿀 때 등에 사용합니다.
    /// 내부 호출 시 GridPlayerCombat 등에서 공격 방향을 맞출 때 호출됩니다.
    /// </summary>
    public void SetFacing(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return;
        Facing = dir;
    }

    /// <summary>
    /// 이동 종료 직후에도 걷기 애니메이션을 유지해도 되는지 판단합니다.
    /// 키를 누르고 있어서 다음 칸으로 바로 이동할 예정이면 걷기 모션이 끊기지 않도록 walkable 여부를 다시 확인합니다.
    /// </summary>
    public bool ShouldKeepMoveAnimationAfterMove()
    {
        if (_player == null || _player.Input == null) return false;
        if (UsesActionDelayForMovement && (_player.ActionDelay == null || !_player.ActionDelay.CanAct)) return false;
        if (GridSystem.Instance == null) return false;
        if (!_player.Input.TryGetHeldMoveDirection(out Vector2Int heldDirection)) return false;

        Vector2Int nextCell = _player.GridPosition + heldDirection;
        return CanContinueMoveAnimationInto(nextCell);
    }

    /// <summary>
    /// 진행하려는 다음 칸이 계속해서 걸을 수 있는 비어있는 타일인지 검사합니다.
    /// 벽이거나, 적이 있거나, 함정 등이 점유하고 있으면 false를 반환합니다.
    /// </summary>
    private bool CanContinueMoveAnimationInto(Vector2Int cell)
    {
        if (!GridSystem.Instance.IsWalkable(cell)) return false;
        if (TurnManager.Instance != null && TurnManager.Instance.GetEntityAt(cell) != null) return false;
        return !GridSystem.Instance.IsCellOccupied(cell);
    }

    /// <summary>
    /// 이동 코루틴이 완전히 종료되었거나 이동이 불가능해 취소되었을 때 시각적 처리(이벤트 호출)를 마무리합니다.
    /// </summary>
    private void FinishMoveVisual()
    {
        OnMoveFinished?.Invoke();
        OnMoveSpeedChanged?.Invoke(1f);
    }

    /// <summary>
    /// 플레이어를 direction 방향의 다음 그리드로 이동시킵니다.
    /// 외부 호출 시 GridPlayerInput에서 WASD/방향키 입력을 받아 호출합니다.
    /// 이동 중(IsMoving == true)에는 새 이동을 무시해서 위치 계산 처리가 꼬이지 않게 합니다.
    /// </summary>
    public void TryMove(Vector2Int direction)
    {
        // 그리드 이동 중 입력 잠금은 여기서 1차로 걸립니다.
        // GridPlayerInput에서도 IsMoving을 보고 입력 자체를 막아 중복 호출을 줄입니다.
        if (IsMoving) return;
        if (UsesActionDelayForMovement && _player.ActionDelay != null && !_player.ActionDelay.CanAct) return;

        Facing = direction;

        Vector2Int target = _player.GridPosition + direction;

        // 그리드 이동 중 공격 가능 entity가 있다면 이동을 취소하고 target을 공격합니다.
        IGridEntity entity = TurnManager.Instance.GetEntityAt(target);
        if (entity is IDamageable damageable && (UnityEngine.Object)damageable != _player)
        {
            FinishMoveVisual();
            _player.Combat.TryMeleeAt(target);
            return;
        }
        if (!GridSystem.Instance.IsWalkable(target))
        {
            FinishMoveVisual();
            return;
        }
        if (TurnManager.Instance.GetEntityAt(target) != null)
        {
            FinishMoveVisual();
            return;
        }
        if (GridSystem.Instance.IsCellOccupied(target))
        {
            FinishMoveVisual();
            return;
        }
        
        float baseActionDelay = GetMoveActionDelay();
        float totalCost = baseActionDelay;
        
        StatusController statusController = _player.GetComponent<StatusController>();
        if (statusController != null)
            totalCost = statusController.ModifyMoveTimeCost(totalCost);

        float statusModifiedCost = totalCost;

        // 신속의 반지, 둔화 저주처럼 최종 초 단위 보정은 상태이상 배율 계산 후 마지막에 적용한다.
        // 여기서 바꾸는 값은 "다음 행동까지 기다리는 시간"이다.
        // GetMoveDuration()이 계산하는 실제 이동 보간 애니메이션 시간은 그대로 두어, 화면 이동 속도와 턴 비용을 분리한다.
        if (_finalStatModifiers != null)
            totalCost = _finalStatModifiers.ModifyMoveActionDelay(totalCost);

        if (logMoveActionDelay)
        {
            Debug.Log($"[이동] mode={moveMode}, level={(_levelSystem != null ? _levelSystem.Level : 0)}, base={baseActionDelay:0.###}초, status={statusModifiedCost:0.###}초, final={totalCost:0.###}초, moveDuration={GetMoveDuration():0.###}초");
        }

        Vector3 startWorld = transform.position;
        _player.SetGridPosition(target);
        Vector3 targetWorld = transform.position;

        // EntityBase.SetGridPosition은 즉시 순간이동하므로 화면 위치만 원래대로 되돌린 뒤 직접 보간합니다.
        transform.position = startWorld;

        _moveCoroutine = StartCoroutine(MoveToCell(direction, target, startWorld, targetWorld, totalCost));
    }

    /// <summary>
    /// 플레이어의 실제 화면 위치를 목표 칸으로 부드럽게 이동(Lerp)시키는 코루틴입니다.
    /// 이동이 끝나면 함정 상호작용, 시야 업데이트, 턴 매니저에 턴 종료 신호를 보냅니다.
    /// </summary>
    private IEnumerator MoveToCell(Vector2Int direction, Vector2Int target, Vector3 startWorld, Vector3 targetWorld, float totalCost)
    {
        IsMoving = true;
        CurrentMoveDuration = GetMoveDuration();

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

        // 도착 직후에 함정을 발동시켜서 플레이어 이동 연출 중 일어날 수 있는 꼬임을 방지합니다.
        TrapSystem.Instance?.TryTriggerAt(target, _player);
        VisionSystem.Instance.UpdateVision(_player.GridPosition);

        const bool skipEnemyTurnDelay = false;

        IsMoving = false;
        _moveCoroutine = null;

        if (UsesActionDelayForMovement)
            _player.ActionDelay?.StartDelay(totalCost);

        bool keepMoveAnimation = ShouldKeepMoveAnimationAfterMove();
        if (!keepMoveAnimation)
        {
            FinishMoveVisual();
        }

        if (UsesActionDelayForMovement)
            TurnManager.Instance.OnPlayerActionCompleted(totalCost, skipEnemyTurnDelay);
        else
            TurnManager.Instance.OnPlayerActionCompletedWithoutEnemyTurns(totalCost);
    }

    /// <summary>
    /// 현재 스탯(MoveSpeed)에 기반하여 한 칸을 이동하는 데 걸리는 실제 연출 시간(초 단위)을 계산합니다.
    /// 애니메이션 속도나 턴 소모 속도와는 별개로 시각적으로 화면에서 미끄러지는 속도입니다.
    /// </summary>
    private float GetMoveDuration()
    {
        int moveSpeed = GetMoveSpeed();

        // 실제 그리드 이동 시간 공식:
        // MoveSpeed가 5를 넘어도 계속 전체 수치를 반영합니다.
        float speedFactor = 1f + Mathf.Max(0, moveSpeed) * moveSpeedDurationMultiplier;
        return Mathf.Max(minMoveDuration, baseMoveDuration / speedFactor);
    }

    /// <summary>
    /// 이번 이동으로 인해 소모할 기본 턴 비용(Action Delay)을 계산합니다.
    /// 레벨에 따른 고정 딜레이가 있다면 그것을 우선시하고, 없다면 MoveSpeed에 비례해 선형 보간합니다.
    /// </summary>
    private float GetMoveActionDelay()
    {
        if (TryGetLevelMoveActionDelay(out float levelDelay))
            return Mathf.Max(minMoveActionDelay, levelDelay);

        float baseDelay = _player.Config != null ? _player.Config.moveTimeDelay : 1f;
        int moveSpeed = GetMoveSpeed();

        float progress = moveSpeedForMinActionDelay <= 1
            ? 1f
            : Mathf.Clamp01((moveSpeed - 1f) / (moveSpeedForMinActionDelay - 1f));

        return Mathf.Lerp(baseDelay, minMoveActionDelay, progress);
    }

    /// <summary>
    /// 플레이어의 현재 레벨에 매핑된 하드코딩된 이동 딜레이(levelMoveActionDelays) 배열에서 값을 가져옵니다.
    /// </summary>
    private bool TryGetLevelMoveActionDelay(out float delay)
    {
        delay = 0f;

        if (_levelSystem == null)
            _levelSystem = PlayerLevelSystem.Instance;

        if (_levelSystem == null || levelMoveActionDelays == null || levelMoveActionDelays.Length == 0)
            return false;

        // GDD 8.3의 레벨별 이동 코스트 수치를 우선 적용합니다.
        // 최대 레벨 이후는 마지막 값에 고정하여 고레벨 이동이 과도하게 빨라지는 것을 막습니다.
        int index = Mathf.Clamp(_levelSystem.Level - 1, 0, levelMoveActionDelays.Length - 1);
        delay = levelMoveActionDelays[index];
        return delay > 0f;
    }

    /// <summary>
    /// 애니메이터의 배속(Speed) 프로퍼티에 적용할 배율을 계산합니다.
    /// 시각적 걷기 모션이 너무 방정맞아지는 것을 방지하기 위해 상한선(animationSpeedCapMoveSpeed)이 걸려있습니다.
    /// </summary>
    private float GetAnimationSpeedMultiplier()
    {
        int cappedMoveSpeed = Mathf.Min(GetMoveSpeed(), animationSpeedCapMoveSpeed);

        // 애니메이션 배속 공식:
        // MoveSpeed 5 이후에는 cappedMoveSpeed가 고정되어 걷기 애니메이션이 너무 빨라지지 않게 합니다.
        float animationDuration = GetDurationForMoveSpeed(cappedMoveSpeed);
        if (animationDuration <= 0f) return 1f;

        return Mathf.Max(1f, baseMoveDuration / animationDuration);
    }

    /// <summary>
    /// 특정 MoveSpeed일 때 한 칸 이동에 걸릴 예상 시간을 구합니다.
    /// </summary>
    private float GetDurationForMoveSpeed(int moveSpeed)
    {
        float speedFactor = 1f + Mathf.Max(0, moveSpeed) * moveSpeedDurationMultiplier;
        return Mathf.Max(minMoveDuration, baseMoveDuration / speedFactor);
    }

    /// <summary>
    /// 스탯 시스템에서 현재 장비/강화 등으로 보정된 최종 이동속도(MoveSpeed) 정수값을 가져옵니다.
    /// </summary>
    private int GetMoveSpeed()
    {
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;

        return _stats != null ? _stats.MoveSpeed : 0;
    }

    /// <summary>
    /// 플레이어를 특정 격자 좌표로 밀쳐냅니다. (이동 딜레이나 턴 소모 없이 강제 이동)
    /// 보스의 스킬이나 강제 위치 이동 함정 등에서 사용됩니다.
    /// </summary>
    public void Knockback(Vector2Int targetGridPos, float duration = 0.2f)
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        Vector3 startWorld = transform.position;
        _player.SetGridPosition(targetGridPos);
        Vector3 targetWorld = transform.position;
        transform.position = startWorld;

        IsMoving = true;
        _moveCoroutine = StartCoroutine(KnockbackRoutine(startWorld, targetWorld, duration));
    }

    /// <summary>
    /// 플레이어의 실제 화면 위치를 목표 칸으로 포물선 감속(Ease out quad)하며 부드럽게 밀쳐내는 코루틴입니다.
    /// </summary>
    private IEnumerator KnockbackRoutine(Vector3 startWorld, Vector3 targetWorld, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease out quad
            float easeT = 1f - (1f - t) * (1f - t);
            transform.position = Vector3.Lerp(startWorld, targetWorld, easeT);
            yield return null;
        }
        transform.position = targetWorld;

        IsMoving = false;
        _moveCoroutine = null;

        FinishMoveVisual();
        VisionSystem.Instance.UpdateVision(_player.GridPosition);
    }

    private void OnDisable()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }
        IsMoving = false;
        OnMoveFinished?.Invoke();
        OnMoveSpeedChanged?.Invoke(1f);
    }

    private void OnValidate()
    {
        minMoveActionDelay = Mathf.Max(0.0005f, minMoveActionDelay);

        if (levelMoveActionDelays == null || levelMoveActionDelays.Length == 0)
            return;

        for (int i = 0; i < levelMoveActionDelays.Length; i++)
            levelMoveActionDelays[i] = Mathf.Max(minMoveActionDelay, levelMoveActionDelays[i]);
    }
}
