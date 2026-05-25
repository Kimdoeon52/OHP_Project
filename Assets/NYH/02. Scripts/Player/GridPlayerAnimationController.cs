using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 애니메이션 전용 어댑터.
/// 이동/공격/체력 로직이 Animator를 직접 알지 않도록 이벤트만 구독한다.
/// 사용법:
/// 1. Player 오브젝트에 Animator와 이 컴포넌트를 붙인다.
/// 2. Animator Controller에 Attack, Die, Move Trigger와 IsMoving, IsDead Bool을 만든다.
/// 3. idleStateName/runStateName을 Animator의 실제 상태 이름과 맞춘다.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(GridPlayer))]
public class GridPlayerAnimationController : MonoBehaviour
{
    [Header("Animator Parameters")]
    [Tooltip("공격 성공 시 사용하는 Trigger 이름.")]
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string hurtTriggerName = "Hurt";

    [Tooltip("사망 시 사용하는 Trigger 이름.")]
    [SerializeField] private string dieTriggerName = "Die";

    [Tooltip("이전 Move Trigger가 남아 있으면 늦게 소비될 수 있어 이동 시작/종료 때 Reset만 한다.")]
    [SerializeField] private string moveTriggerName = "Move";

    [Tooltip("이동 중 true, 이동 종료 시 false가 되는 Bool 이름.")]
    [SerializeField] private string isMovingBoolName = "IsMoving";

    [Tooltip("사망 후 true로 고정되는 Bool 이름.")]
    [SerializeField] private string isDeadBoolName = "IsDead";

    [Header("Animator States")]
    [Tooltip("기본 Base Layer는 0. 다른 레이어에서 재생할 때만 바꾼다.")]
    [SerializeField] private int baseLayerIndex;

    [Tooltip("이동이 끝났을 때 즉시 재생할 Idle 상태 이름.")]
    [SerializeField] private string idleStateName = "warrior idle";

    [Tooltip("이동이 시작됐을 때 즉시 재생할 Run/Walk 상태 이름.")]
    [SerializeField] private string runStateName = "warrior run";

    [Header("Facing")]
    [Tooltip("원본 스프라이트가 오른쪽을 보고 있으면 true, 왼쪽을 보고 있으면 false.")]
    [SerializeField] private bool faceRightByDefault = true;

    [Header("공격 애니메이션 속도")]
    [Tooltip("AttackSpeed 1당 공격 애니메이션 배속을 올리는 비율입니다.")]
    [SerializeField, Min(0f)] private float attackSpeedAnimationMultiplier = 0.12f;

    [Tooltip("공격 애니메이션 배속이 이 값보다 높아지지 않게 제한합니다.")]
    [SerializeField, Min(1f)] private float maxAttackAnimationSpeed = 2.5f;

    [Tooltip("공격 후 Animator speed를 기본값으로 되돌리기까지 기다리는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float attackSpeedResetDelay = 0.25f;

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private GridPlayer _player;
    private NYHPlayerStats _stats;
    private Coroutine _attackSpeedCoroutine;
    private bool _isDead;
    private bool _isFacingRight;
    private bool _isSubscribed;
    private bool _isMoveVisualActive;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _player = GetComponent<GridPlayer>();
        _stats = GetComponent<NYHPlayerStats>();
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;
        _isFacingRight = faceRightByDefault;
        ApplyFacing();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (_attackSpeedCoroutine != null)
        {
            StopCoroutine(_attackSpeedCoroutine);
            _attackSpeedCoroutine = null;
        }

        if (_animator != null)
            _animator.speed = 1f;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (_isDead || !_isSubscribed || _player == null || _player.Movement == null) return;
        if (!_isMoveVisualActive || _player.Movement.IsMoving) return;

        // 고속 홀드 이동 중에는 이동 코루틴 사이 한 프레임에 Idle로 튀지 않게 유지한다.
        // 단, 키를 놓았거나 다음 칸이 벽이면 즉시 Idle로 되돌려 걷기 모션이 남지 않게 한다.
        if (!_player.Movement.ShouldKeepMoveAnimationAfterMove())
            OnMoveFinished();
    }

    private void TrySubscribe()
    {
        if (_isSubscribed) return;

        if (_player == null)
            _player = GetComponent<GridPlayer>();

        if (_player == null || _player.Movement == null || _player.Combat == null || _player.Health == null)
            return;

        // GridPlayer.Awake 이후에만 구독해 컴포넌트 초기화 순서 문제를 피한다.
        _player.Movement.OnMoveSucceeded += OnMoveSucceeded;
        _player.Movement.OnMoveFinished += OnMoveFinished;
        _player.Movement.OnMoveSpeedChanged += OnMoveSpeedChanged;
        _player.Combat.OnAttackPerformed += OnAttackPerformed;
        _player.Health.OnDamaged += OnDamaged;
        _player.Health.OnDied += OnDied;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed || _player == null) return;

        _player.Movement.OnMoveSucceeded -= OnMoveSucceeded;
        _player.Movement.OnMoveFinished -= OnMoveFinished;
        _player.Movement.OnMoveSpeedChanged -= OnMoveSpeedChanged;
        _player.Combat.OnAttackPerformed -= OnAttackPerformed;
        _player.Health.OnDamaged -= OnDamaged;
        _player.Health.OnDied -= OnDied;
        _isSubscribed = false;
    }

    private void OnMoveSucceeded(Vector2Int direction)
    {
        if (_isDead) return;

        UpdateHorizontalFacing(direction);
        ResetTriggerIfConfigured(moveTriggerName);
        SetBoolIfConfigured(isMovingBoolName, true);
        _isMoveVisualActive = true;

        // Trigger가 남아서 늦게 재생되는 문제를 막기 위해 이동 상태는 직접 재생한다.
        PlayStateIfConfigured(runStateName, keepCurrentRunTime: true);
    }

    private void OnMoveFinished()
    {
        ResetTriggerIfConfigured(moveTriggerName);
        SetBoolIfConfigured(isMovingBoolName, false);
        _isMoveVisualActive = false;
        if (_animator != null)
            _animator.speed = 1f;

        // 걷기 클립이 끝나길 기다리지 않고 도착 즉시 Idle로 자른다.
        PlayStateIfConfigured(idleStateName, keepCurrentRunTime: false);
    }

    private void OnMoveSpeedChanged(float speedMultiplier)
    {
        if (_isDead || _animator == null) return;

        // GridPlayerMovement가 계산한 배속만 적용한다.
        // MoveSpeed 5 이후 애니메이션 배속 고정 정책도 이동 스크립트에서 결정한다.
        _animator.speed = speedMultiplier;
    }

    private void OnAttackPerformed(Vector2Int direction)
    {
        if (_isDead) return;

        UpdateHorizontalFacing(direction);
        ApplyAttackAnimationSpeed();
        SetTriggerIfConfigured(attackTriggerName);
    }

    private void OnDamaged(int damage)
    {
        if (_isDead || _player.Health.IsDead) return;

        SetTriggerIfConfigured(hurtTriggerName);
    }

    private void OnDied()
    {
        if (_isDead) return;

        _isDead = true;
        _isMoveVisualActive = false;
        if (_attackSpeedCoroutine != null)
        {
            StopCoroutine(_attackSpeedCoroutine);
            _attackSpeedCoroutine = null;
        }

        if (_animator != null)
            _animator.speed = 1f;

        ResetTriggerIfConfigured(attackTriggerName);
        ResetTriggerIfConfigured(hurtTriggerName);
        ResetTriggerIfConfigured(moveTriggerName);
        SetBoolIfConfigured(isMovingBoolName, false);
        SetBoolIfConfigured(isDeadBoolName, true);
        SetTriggerIfConfigured(dieTriggerName);
    }

    private void UpdateHorizontalFacing(Vector2Int direction)
    {
        if (direction.x == 0) return;

        _isFacingRight = direction.x > 0;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        // 원본 스프라이트가 오른쪽을 본다는 전제로 flipX만 뒤집어 좌우 리소스 중복을 줄인다.
        _spriteRenderer.flipX = faceRightByDefault ? !_isFacingRight : _isFacingRight;
    }

    private void ApplyAttackAnimationSpeed()
    {
        if (_animator == null) return;

        if (_attackSpeedCoroutine != null)
            StopCoroutine(_attackSpeedCoroutine);

        _animator.speed = GetAttackAnimationSpeed();
        _attackSpeedCoroutine = StartCoroutine(RestoreAnimatorSpeedAfterAttack());
    }

    private IEnumerator RestoreAnimatorSpeedAfterAttack()
    {
        yield return new WaitForSeconds(attackSpeedResetDelay);

        if (_animator != null && !_isDead)
            _animator.speed = _isMoveVisualActive ? _animator.speed : 1f;

        _attackSpeedCoroutine = null;
    }

    private float GetAttackAnimationSpeed()
    {
        int attackSpeed = GetAttackSpeed();
        float speed = 1f + Mathf.Max(0, attackSpeed) * attackSpeedAnimationMultiplier;
        return Mathf.Min(maxAttackAnimationSpeed, speed);
    }

    private int GetAttackSpeed()
    {
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;

        return _stats != null ? _stats.AttackSpeed : 0;
    }

    private void PlayStateIfConfigured(string stateName, bool keepCurrentRunTime)
    {
        if (string.IsNullOrEmpty(stateName) || _animator == null || _animator.runtimeAnimatorController == null)
            return;

        int stateHash = Animator.StringToHash(stateName);
        if (!_animator.HasState(baseLayerIndex, stateHash))
            return;

        float normalizedTime = 0f;
        if (keepCurrentRunTime && IsCurrentState(stateHash))
            normalizedTime = _animator.GetCurrentAnimatorStateInfo(baseLayerIndex).normalizedTime;

        _animator.Play(stateHash, baseLayerIndex, normalizedTime);
    }

    private bool IsCurrentState(int stateHash)
    {
        if (_animator == null) return false;
        return _animator.GetCurrentAnimatorStateInfo(baseLayerIndex).shortNameHash == stateHash;
    }

    private void SetTriggerIfConfigured(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName) || !HasParameter(triggerName, AnimatorControllerParameterType.Trigger)) return;
        _animator.SetTrigger(triggerName);
    }

    private void ResetTriggerIfConfigured(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName) || !HasParameter(triggerName, AnimatorControllerParameterType.Trigger)) return;
        _animator.ResetTrigger(triggerName);
    }

    private void SetBoolIfConfigured(string boolName, bool value)
    {
        if (string.IsNullOrEmpty(boolName) || !HasParameter(boolName, AnimatorControllerParameterType.Bool)) return;
        _animator.SetBool(boolName, value);
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.type == parameterType && parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
