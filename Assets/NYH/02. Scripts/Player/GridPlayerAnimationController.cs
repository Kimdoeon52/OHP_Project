using System.Collections;
using UnityEngine;

/// <summary>
/// ?뚮젅?댁뼱 ?좊땲硫붿씠???꾩슜 ?대뙌??
/// ?대룞/怨듦꺽/泥대젰 濡쒖쭅??Animator瑜?吏곸젒 ?뚯? ?딅룄濡??대깽?몃쭔 援щ룆?쒕떎.
/// ?ъ슜踰?
/// 1. Player ?ㅻ툕?앺듃??Animator? ??而댄룷?뚰듃瑜?遺숈씤??
/// 2. Animator Controller??Attack, Die, Move Trigger? IsMoving, IsDead Bool??留뚮뱺??
/// 3. idleStateName/runStateName??Animator???ㅼ젣 ?곹깭 ?대쫫怨?留욎텣??
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(GridPlayer))]
public class GridPlayerAnimationController : MonoBehaviour
{
    [Header("Animator Parameters")]
    [Tooltip("怨듦꺽 ?깃났 ???ъ슜?섎뒗 Trigger ?대쫫.")]
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string hurtTriggerName = "Hurt";

    [Tooltip("?щ쭩 ???ъ슜?섎뒗 Trigger ?대쫫.")]
    [SerializeField] private string dieTriggerName = "Die";

    [Tooltip("?댁쟾 Move Trigger媛 ?⑥븘 ?덉쑝硫???쾶 ?뚮퉬?????덉뼱 ?대룞 ?쒖옉/醫낅즺 ??Reset留??쒕떎.")]
    [SerializeField] private string moveTriggerName = "Move";

    [Tooltip("?대룞 以?true, ?대룞 醫낅즺 ??false媛 ?섎뒗 Bool ?대쫫.")]
    [SerializeField] private string isMovingBoolName = "IsMoving";

    [Tooltip("?щ쭩 ??true濡?怨좎젙?섎뒗 Bool ?대쫫.")]
    [SerializeField] private string isDeadBoolName = "IsDead";

    [Header("Animator States")]
    [Tooltip("湲곕낯 Base Layer??0. ?ㅻⅨ ?덉씠?댁뿉???ъ깮???뚮쭔 諛붽씔??")]
    [SerializeField] private int baseLayerIndex;

    [Tooltip("?대룞???앸궗????利됱떆 ?ъ깮??Idle ?곹깭 ?대쫫.")]
    [SerializeField] private string idleStateName = "warrior idle";

    [Tooltip("?대룞???쒖옉?먯쓣 ??利됱떆 ?ъ깮??Run/Walk ?곹깭 ?대쫫.")]
    [SerializeField] private string runStateName = "warrior run";

    [Tooltip("怨듦꺽????利됱떆 泥섏쓬遺???ъ깮??Animator ?곹깭 ?대쫫.")]
    [SerializeField] private string attackStateName = "warrior single swing1";

    [Header("Facing")]
    [Tooltip("?먮낯 ?ㅽ봽?쇱씠?멸? ?ㅻⅨ履쎌쓣 蹂닿퀬 ?덉쑝硫?true, ?쇱そ??蹂닿퀬 ?덉쑝硫?false.")]
    [SerializeField] private bool faceRightByDefault = true;

    [Header("怨듦꺽 ?좊땲硫붿씠???띾룄")]
    [Tooltip("怨듦꺽 ?좊땲硫붿씠??諛곗냽????媛믩낫???믪븘吏吏 ?딄쾶 ?쒗븳?⑸땲??")]
    [SerializeField, Min(1f)] private float maxAttackAnimationSpeed = 12f;

    [Tooltip("怨듦꺽 ??Animator speed瑜?湲곕낯媛믪쑝濡??섎룎由ш린源뚯? 湲곕떎由щ뒗 ?쒓컙?낅땲??")]
    [SerializeField, Min(0.01f)] private float attackSpeedResetDelay = 0.25f;

    [Tooltip("怨듦꺽 ?좊땲硫붿씠?섏씠 ?덈Т 吏㏃븘???꾩삁 ??蹂댁씠吏 ?딅룄濡?蹂댁옣??理쒖냼 ?쒖떆 ?쒓컙?낅땲??")]
    [SerializeField, Min(0.01f)] private float minAttackAnimationDuration = 0.08f;

    [Tooltip("而⑦듃濡ㅻ윭?먯꽌 怨듦꺽 ?대┰ 湲몄씠瑜?李얠? 紐삵뻽?????ъ슜??湲곕낯 湲몄씠?낅땲??")]
    [SerializeField, Min(0.01f)] private float attackClipLengthFallback = 1.1f;

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private GridPlayer _player;
    private Coroutine _attackSpeedCoroutine;
    private bool _isDead;
    private bool _isFacingRight;
    private bool _isSubscribed;
    private bool _isMoveVisualActive;
    private bool _isAttackVisualActive;
    private float _cachedAttackClipLength = -1f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _player = GetComponent<GridPlayer>();
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

        _isMoveVisualActive = false;
        _isAttackVisualActive = false;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (_isDead || !_isSubscribed || _player == null || _player.Movement == null) return;
        if (!_isMoveVisualActive || _player.Movement.IsMoving) return;

        // 怨좎냽 ????대룞 以묒뿉???대룞 肄붾（???ъ씠 ???꾨젅?꾩뿉 Idle濡??吏 ?딄쾶 ?좎??쒕떎.
        // ?? ?ㅻ? ?볦븯嫄곕굹 ?ㅼ쓬 移몄씠 踰쎌씠硫?利됱떆 Idle濡??섎룎??嫄룰린 紐⑥뀡???⑥? ?딄쾶 ?쒕떎.
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

        // GridPlayer.Awake ?댄썑?먮쭔 援щ룆??而댄룷?뚰듃 珥덇린???쒖꽌 臾몄젣瑜??쇳븳??
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

        // Trigger媛 ?⑥븘????쾶 ?ъ깮?섎뒗 臾몄젣瑜?留됯린 ?꾪빐 ?대룞 ?곹깭??吏곸젒 ?ъ깮?쒕떎.
        PlayStateIfConfigured(runStateName, keepCurrentRunTime: true);
    }

    private void OnMoveFinished()
    {
        ResetTriggerIfConfigured(moveTriggerName);
        SetBoolIfConfigured(isMovingBoolName, false);
        _isMoveVisualActive = false;
        if (_isAttackVisualActive)
            return;

        if (_animator != null)
            _animator.speed = 1f;

        // 嫄룰린 ?대┰???앸굹湲?湲곕떎由ъ? ?딄퀬 ?꾩갑 利됱떆 Idle濡??먮Ⅸ??
        PlayStateIfConfigured(idleStateName, keepCurrentRunTime: false);
    }

    private void OnMoveSpeedChanged(float speedMultiplier)
    {
        if (_isDead || _animator == null) return;

        // GridPlayerMovement媛 怨꾩궛??諛곗냽留??곸슜?쒕떎.
        // MoveSpeed 5 ?댄썑 ?좊땲硫붿씠??諛곗냽 怨좎젙 ?뺤콉???대룞 ?ㅽ겕由쏀듃?먯꽌 寃곗젙?쒕떎.
        _animator.speed = speedMultiplier;
    }

    private void OnAttackPerformed(Vector2Int direction)
    {
        if (_isDead) return;

        UpdateHorizontalFacing(direction);
        ResetTriggerIfConfigured(hurtTriggerName);
        ApplyAttackAnimationSpeed();

        if (!PlayStateIfConfigured(attackStateName, keepCurrentRunTime: false))
            SetTriggerIfConfigured(attackTriggerName);
    }

    private void OnDamaged(int damage)
    {
        if (_isDead || _player.Health.IsDead) return;
        if (_isAttackVisualActive) return;

        SetTriggerIfConfigured(hurtTriggerName);
    }

    private void OnDied()
    {
        if (_isDead) return;

        _isDead = true;
        _isMoveVisualActive = false;
        _isAttackVisualActive = false;
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
        // ?먮낯 ?ㅽ봽?쇱씠?멸? ?ㅻⅨ履쎌쓣 蹂몃떎???꾩젣濡?flipX留??ㅼ쭛??醫뚯슦 由ъ냼??以묐났??以꾩씤??
        _spriteRenderer.flipX = faceRightByDefault ? !_isFacingRight : _isFacingRight;
    }

    private void ApplyAttackAnimationSpeed()
    {
        if (_animator == null) return;

        if (_attackSpeedCoroutine != null)
            StopCoroutine(_attackSpeedCoroutine);

        float targetDuration = GetAttackAnimationTargetDuration();
        _isAttackVisualActive = true;
        _animator.speed = GetAttackAnimationSpeed(targetDuration);
        _attackSpeedCoroutine = StartCoroutine(RestoreAnimatorSpeedAfterAttack(targetDuration));
    }

    private IEnumerator RestoreAnimatorSpeedAfterAttack(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (_animator != null && !_isDead)
            _animator.speed = _isMoveVisualActive ? _animator.speed : 1f;

        _isAttackVisualActive = false;
        _attackSpeedCoroutine = null;
    }

    private float GetAttackAnimationSpeed(float targetDuration)
    {
        float clipLength = GetAttackClipLength();
        float safeDuration = Mathf.Max(0.01f, targetDuration);
        float speed = clipLength / safeDuration;
        return Mathf.Clamp(speed, 1f, maxAttackAnimationSpeed);
    }

    private float GetAttackAnimationTargetDuration()
    {
        float actionInterval = _player != null && _player.Combat != null
            ? _player.Combat.GetMeleeActionInterval()
            : attackSpeedResetDelay;

        return Mathf.Max(minAttackAnimationDuration, actionInterval);
    }

    private float GetAttackClipLength()
    {
        if (_cachedAttackClipLength > 0f)
            return _cachedAttackClipLength;

        _cachedAttackClipLength = attackClipLengthFallback;

        if (_animator == null || _animator.runtimeAnimatorController == null)
            return _cachedAttackClipLength;

        foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == attackStateName)
            {
                _cachedAttackClipLength = Mathf.Max(0.01f, clip.length);
                break;
            }
        }

        return _cachedAttackClipLength;
    }

    private bool PlayStateIfConfigured(string stateName, bool keepCurrentRunTime)
    {
        if (string.IsNullOrEmpty(stateName) || !CanUseAnimator())
            return false;

        int stateHash = Animator.StringToHash(stateName);
        if (!_animator.HasState(baseLayerIndex, stateHash))
            return false;

        float normalizedTime = 0f;
        if (keepCurrentRunTime && IsCurrentState(stateHash))
            normalizedTime = _animator.GetCurrentAnimatorStateInfo(baseLayerIndex).normalizedTime;

        _animator.Play(stateHash, baseLayerIndex, normalizedTime);
        return true;
    }

    private bool IsCurrentState(int stateHash)
    {
        if (_animator == null) return false;
        return _animator.GetCurrentAnimatorStateInfo(baseLayerIndex).shortNameHash == stateHash;
    }

    private void SetTriggerIfConfigured(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName) || !CanUseAnimator() || !HasParameter(triggerName, AnimatorControllerParameterType.Trigger)) return;
        _animator.SetTrigger(triggerName);
    }

    private void ResetTriggerIfConfigured(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName) || !CanUseAnimator() || !HasParameter(triggerName, AnimatorControllerParameterType.Trigger)) return;
        _animator.ResetTrigger(triggerName);
    }

    private void SetBoolIfConfigured(string boolName, bool value)
    {
        if (string.IsNullOrEmpty(boolName) || !CanUseAnimator() || !HasParameter(boolName, AnimatorControllerParameterType.Bool)) return;
        _animator.SetBool(boolName, value);
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (!CanUseAnimator()) return false;

        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.type == parameterType && parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private bool CanUseAnimator()
    {
        // 씬 전환/사망 처리 중 비활성화된 Animator에 Play/Trigger를 보내면 Unity 경고가 발생한다.
        return _animator != null
            && _animator.runtimeAnimatorController != null
            && _animator.isActiveAndEnabled
            && _animator.gameObject.activeInHierarchy;
    }
}

