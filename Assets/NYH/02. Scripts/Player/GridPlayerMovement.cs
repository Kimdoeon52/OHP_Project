using System;
using System.Collections;
using UnityEngine;

public enum PlayerGridMoveMode
{
    LegacyActionDelay,
    ContinuousGridMove
}

/// <summary>
/// ?뚮젅?댁뼱??4諛⑺뼢 洹몃━???대룞???대떦?쒕떎.
/// ?쇰━ ?꾩튂(GridPosition)???대룞 ?쒖옉 ?쒖젏??癒쇱? 諛붽씀怨?
/// ?ㅼ젣 Transform? 肄붾（?댁쑝濡??ㅼ쓬 移?以묒떖源뚯? 蹂닿컙?쒕떎.
/// </summary>
public class GridPlayerMovement : MonoBehaviour
{
    /// <summary>
    /// ?대룞???ㅼ젣濡??쒖옉?먯쓣 ???몄텧?쒕떎.
    /// ?ъ슜 ?? ?좊땲硫붿씠??而⑦듃濡ㅻ윭媛 ???대깽?몃? 援щ룆?댁꽌 嫄룰린 ?좊땲硫붿씠?섏쓣 耳좊떎.
    /// </summary>
    public event Action<Vector2Int> OnMoveSucceeded;

    /// <summary>
    /// 蹂닿컙 ?대룞怨??꾩갑 ??異붽? ?湲곗떆媛꾩씠 紐⑤몢 ?앸궗?????몄텧?쒕떎.
    /// ?ъ슜 ?? ?좊땲硫붿씠??而⑦듃濡ㅻ윭媛 ???대깽?몃? 援щ룆?댁꽌 嫄룰린 ?좊땲硫붿씠?섏쓣 ?꾧퀬 Idle濡??뚮┛??
    /// </summary>
    public event Action OnMoveFinished;

    /// <summary>
    /// ?대룞?띾룄 ?ㅽ꺈???곕씪 ?대쾲 ?대룞???좊땲硫붿씠??諛곗냽??諛붾????몄텧?쒕떎.
    /// ?ъ슜 ?? ?좊땲硫붿씠??而⑦듃濡ㅻ윭媛 Animator.speed????媛믪쓣 ?곸슜?쒕떎.
    /// </summary>
    public event Action<float> OnMoveSpeedChanged;

    [Header("Grid Move Duration")]
    [Tooltip("MoveSpeed媛 0??????移??대룞??嫄몃━??湲곕낯 ?쒓컙(珥?.")]
    [SerializeField, Min(0.01f)] private float baseMoveDuration = 0.18f;

    [Tooltip("MoveSpeed 1???대룞 ?쒓컙??以꾩뼱?쒕뒗 怨꾩닔. 怨듭떇: duration = base / (1 + MoveSpeed * multiplier)")]
    [SerializeField, Min(0f)] private float moveSpeedDurationMultiplier = 0.15f;

    [Tooltip("MoveSpeed媛 ?믪븘????移??대룞 ?쒓컙????媛믩낫??吏㏃븘吏吏 ?딄쾶 ?쒕떎.")]
    [SerializeField, Min(0.0001f)] private float minMoveDuration = 0.001f;

    [Header("Move Mode")]
    [SerializeField] private PlayerGridMoveMode moveMode = PlayerGridMoveMode.ContinuousGridMove;

    [Header("Move Action Delay")]
    [Tooltip("GDD 湲곗? 理쒖냼 ?대룞 ?됰룞 ?쒕젅?댁엯?덈떎. ?덈꺼/MoveSpeed媛 ?믪븘?몃룄 ??媛믩낫????븘吏吏 ?딆뒿?덈떎.")]
    [SerializeField, Min(0.0005f)] private float minMoveActionDelay = 0.3f;

    [Tooltip("GDD 8.3 湲곗? ?덈꺼蹂??대룞 ?됰룞 ?쒕젅?댁엯?덈떎. index 0? 1?덈꺼, index 9??10?덈꺼?낅땲??")]
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

    [Tooltip("??MoveSpeed???꾨떖?섎㈃ ?대룞 ?됰룞 ?쒕젅?닿? minMoveActionDelay???꾨떖?⑸땲?? 湲곕낯媛?10? GDD??10?덈꺼 0.3珥?湲곗??낅땲??")]
    [SerializeField, Min(1)] private int moveSpeedForMinActionDelay = 10;

    [Header("Animation Speed")]
    [Tooltip("嫄룰린 ?좊땲硫붿씠??諛곗냽 怨꾩궛??諛섏쁺??理쒕? MoveSpeed. ?ㅼ젣 ?대룞?띾룄????媛??댄썑?먮룄 怨꾩냽 ?ㅻⅨ??")]
    [SerializeField, Min(0)] private int animationSpeedCapMoveSpeed = 5;

    public Vector2Int Facing { get; private set; } = Vector2Int.right;

    /// <summary>
    /// 洹몃━?쒖뿉???ㅼ쓬 洹몃━?쒕줈 ?섏뼱媛嫄곕굹 ?꾩갑 ??異붽? ?湲곗떆媛꾩쓣 泥섎━?섎뒗 ?숈븞 true媛 ?쒕떎.
    /// ??媛믪씠 ?대룞 以??낅젰 ?좉툑??湲곗??대떎.
    /// </summary>
    public bool IsMoving { get; private set; }
    public bool UsesActionDelayForMovement => moveMode == PlayerGridMoveMode.LegacyActionDelay;
    public bool IgnoresActionDelayForMovement => moveMode == PlayerGridMoveMode.ContinuousGridMove;

    /// <summary>
    /// ?꾩옱 吏꾪뻾 以묒씤 ??移??대룞???ㅼ젣 ?곸슜??蹂닿컙 ?쒓컙.
    /// ?붾쾭洹?UI???대룞媛??쒕떇 ?뺤씤?⑹쑝濡??쎌쓣 ???덈떎.
    /// </summary>
    public float CurrentMoveDuration { get; private set; }

    private GridPlayer _player;
    private NYHPlayerStats _stats;
    private PlayerLevelSystem _levelSystem;
    private Coroutine _moveCoroutine;

    public void Init(GridPlayer player)
    {
        _player = player;
        _stats = GetComponent<NYHPlayerStats>();
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;

        _levelSystem = GetComponent<PlayerLevelSystem>();
        if (_levelSystem == null)
            _levelSystem = PlayerLevelSystem.Instance;
    }

    /// <summary>
    /// 怨듦꺽泥섎읆 ?ㅼ젣 ?대룞 ?놁씠 諛붾씪蹂대뒗 諛⑺뼢留?諛붽퓭???????ъ슜?쒕떎.
    /// ?몃? ?몄텧 ?? GridPlayerCombat??怨듦꺽 諛⑺뼢??留욎텧 ???몄텧?쒕떎.
    /// </summary>
    public void SetFacing(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return;
        Facing = dir;
    }

    /// <summary>
    /// ?대룞 醫낅즺 吏곹썑?먮룄 嫄룰린 ?좊땲硫붿씠?섏쓣 ?좎??대룄 ?섎뒗吏 ?먮떒?쒕떎.
    /// ?ㅻ? ?꾨Ⅴ怨??덉뼱???ㅼ쓬 移몄씠 踰쎌씠硫?嫄룰린 紐⑥뀡???⑥? ?딅룄濡?walkable???ㅼ떆 ?뺤씤?쒕떎.
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

    private bool CanContinueMoveAnimationInto(Vector2Int cell)
    {
        if (!GridSystem.Instance.IsWalkable(cell)) return false;
        if (TurnManager.Instance != null && TurnManager.Instance.GetEntityAt(cell) != null) return false;
        return !GridSystem.Instance.IsCellOccupied(cell);
    }

    private void FinishMoveVisual()
    {
        OnMoveFinished?.Invoke();
        OnMoveSpeedChanged?.Invoke(1f);
    }

    /// <summary>
    /// ?뚮젅?댁뼱瑜?direction 諛⑺뼢???ㅼ쓬 洹몃━?쒕줈 ?대룞?쒗궓??
    /// ?몃? ?몄텧 ?? GridPlayerInput??WASD/諛⑺뼢???낅젰??諛쏆븘 ?몄텧?쒕떎.
    /// ?대룞 以?IsMoving == true)?먮뒗 ???대룞??臾댁떆?댁꽌 ?꾩튂? ??泥섎━媛 瑗ъ씠吏 ?딄쾶 ?쒕떎.
    /// </summary>
    public void TryMove(Vector2Int direction)
    {
        // 洹몃━???대룞 以??낅젰 ?좉툑? ?ш린??1李⑤줈 嫄몃┛??
        // GridPlayerInput?먯꽌??IsMoving??蹂닿퀬 ?낅젰 ?먯껜瑜?留됱븘 以묐났 ?몄텧??以꾩씤??
        if (IsMoving) return;
        if (UsesActionDelayForMovement && _player.ActionDelay != null && !_player.ActionDelay.CanAct) return;

        Facing = direction;

        Vector2Int target = _player.GridPosition + direction;

        // 洹몃━???대룞 以?怨듦꺽 媛??entity媛 ?덈떎硫??대룞???덊븯怨?target??怨듦꺽 ?쒕떎.
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
        float totalCost = GetMoveActionDelay();
        StatusController statusController = _player.GetComponent<StatusController>();
        if (statusController != null)
            totalCost = statusController.ModifyMoveTimeCost(totalCost);

        Vector3 startWorld = transform.position;
        _player.SetGridPosition(target);
        Vector3 targetWorld = transform.position;

        // EntityBase.SetGridPosition? 利됱떆 ?쒓컙?대룞?섎?濡? ?붾㈃ ?꾩튂留??섎룎由???吏곸젒 蹂닿컙?쒕떎.
        transform.position = startWorld;

        _moveCoroutine = StartCoroutine(MoveToCell(direction, target, startWorld, targetWorld, totalCost));
    }

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

        // ?꾩갑 ?꾩뿉 ?댁쓣 ?섍꺼 ???됰룞???뚮젅?댁뼱 ?대룞 ?곗텧 以??쇱뼱?ㅼ? ?딄쾶 ?쒕떎.
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

    private float GetMoveDuration()
    {
        int moveSpeed = GetMoveSpeed();

        // ?ㅼ젣 洹몃━???대룞 ?쒓컙 怨듭떇:
        // MoveSpeed媛 5瑜??섏뼱??怨꾩냽 ?꾩껜 ?섏튂瑜?諛섏쁺?쒕떎.
        float speedFactor = 1f + Mathf.Max(0, moveSpeed) * moveSpeedDurationMultiplier;
        return Mathf.Max(minMoveDuration, baseMoveDuration / speedFactor);
    }

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

    private bool TryGetLevelMoveActionDelay(out float delay)
    {
        delay = 0f;

        if (_levelSystem == null)
            _levelSystem = PlayerLevelSystem.Instance;

        if (_levelSystem == null || levelMoveActionDelays == null || levelMoveActionDelays.Length == 0)
            return false;

        // GDD 8.3???덈꺼蹂??대룞 荑⑦????쒕? ?곗꽑 ?곸슜?쒕떎.
        // ???댄썑 ?덈꺼? 留덉?留?媛믪뿉 怨좎젙??怨좊젅踰??대룞??怨쇰룄?섍쾶 鍮⑤씪吏??寃껋쓣 留됰뒗??
        int index = Mathf.Clamp(_levelSystem.Level - 1, 0, levelMoveActionDelays.Length - 1);
        delay = levelMoveActionDelays[index];
        return delay > 0f;
    }

    private float GetAnimationSpeedMultiplier()
    {
        int cappedMoveSpeed = Mathf.Min(GetMoveSpeed(), animationSpeedCapMoveSpeed);

        // ?좊땲硫붿씠??諛곗냽 怨듭떇:
        // MoveSpeed 5 ?댄썑?먮뒗 cappedMoveSpeed媛 怨좎젙?섏뼱 嫄룰린 ?좊땲硫붿씠?섏씠 ??鍮⑤씪吏吏 ?딅뒗??
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

    private void OnValidate()
    {
        minMoveActionDelay = Mathf.Max(0.0005f, minMoveActionDelay);

        if (levelMoveActionDelays == null || levelMoveActionDelays.Length == 0)
            return;

        for (int i = 0; i < levelMoveActionDelays.Length; i++)
            levelMoveActionDelays[i] = Mathf.Max(minMoveActionDelay, levelMoveActionDelays[i]);
    }
}

