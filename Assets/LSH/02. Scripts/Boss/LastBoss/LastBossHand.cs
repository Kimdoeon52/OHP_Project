using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;

public class LastBossHand : BossBase, IGridEntity, IDamageable
{
    public enum HandSide
    {
        Left,
        Right
    }

    public enum HandMode
    {
        OpenHand,
        Fist,
        Scratch
    }

    [Header("Hand Setup")]
    [SerializeField] private HandSide side;
    [SerializeField] private int roamStepDelayMs = 200;
    [SerializeField] private float roamMoveDuration = 0.12f;
    [SerializeField] private int minStepsBetweenAttacks = 4;
    [SerializeField] private int maxStepsBetweenAttacks = 7;
    [SerializeField] private float slamStayDuration = 2.0f;
    [SerializeField] private float sweepSpeed = 0.5f;
    [SerializeField] private float slamSpeed = 0.25f;
    [SerializeField] private float warningDuration = 1.5f;
    [SerializeField] private float fistWarningDuration = 1.0f;
    [SerializeField] private int knockbackDist = 3;
    [SerializeField] private bool onlyDamageableWhenSlammed = true;

    [Header("Damage Parameters")]
    [SerializeField] private int fistSlamDamage = 25;
    [SerializeField] private int sweepDamage = 15;
    [SerializeField] private int scratchDamage = 15;

    [Header("Visual Prefabs/Child Objects")]
    [SerializeField] private GameObject openHandVisual;
    [SerializeField] private GameObject fistVisual;
    [SerializeField] private GameObject scratchVisual;

    private LastBoss _ownerBoss;
    private Vector2Int _gridPos;
    private List<Vector2Int> _perimeterPath = new List<Vector2Int>();
    private int _currentPathIndex = 0;
    private bool _moveClockwise = true;
    private bool _isAttackable = false;
    private bool _isInitialized = false;
    private Vector2Int _lastRoamPos;

    // IGridEntity implementations
    public override Vector2Int GridPosition => _gridPos;

    // IDamageable implementations
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => currentHp <= 0;

    public void InitHand(LastBoss owner, HandSide handSide, int maxHpValue, List<Vector2Int> path, int startIndex, bool clockwise)
    {
        _ownerBoss = owner;
        side = handSide;
        maxHp = maxHpValue;
        currentHp = maxHpValue;
        _perimeterPath = path;
        _currentPathIndex = startIndex;
        _moveClockwise = clockwise;

        Debug.Log($"<color=green>[하데스 손 ({side})]</color> InitHand 호출됨. 경로 크기: {(path != null ? path.Count : 0)}, isActiveAndEnabled: {isActiveAndEnabled}");

        if (_perimeterPath != null && _perimeterPath.Count > 0)
        {
            _gridPos = _perimeterPath[_currentPathIndex];
            if (GridSystem.Instance != null)
            {
                transform.position = GridSystem.Instance.GridToWorld(_gridPos);
            }
        }

        InitVisuals();
        UpdateVisualForPosition(_gridPos);

        _isInitialized = true;

        if (isActiveAndEnabled)
        {
            Debug.Log($"<color=green>[하데스 손 ({side})]</color> InitHand에서 RoamAndAttackLoop 호출 시작.");
            RoamAndAttackLoop().Forget();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        Debug.Log($"<color=green>[하데스 손 ({side})]</color> OnEnable 호출됨. _isInitialized: {_isInitialized}");

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.Register(this);
        }

        if (_isInitialized)
        {
            Debug.Log($"<color=green>[하데스 손 ({side})]</color> OnEnable에서 RoamAndAttackLoop 호출 시작.");
            RoamAndAttackLoop().Forget();
        }
    }

    protected override void Update()
    {
        // Overridden to bypass the BossBase's automatic real-time PhaseState.
        // Roaming and attack patterns are managed inside RoamAndAttackLoop.
    }

    public void OnTurnUpdate()
    {
        // Hands run independently on their own asynchronous timelines.
    }

    private void SetVisualActive(GameObject visual, bool active)
    {
        if (visual == null) return;

        if (visual == gameObject)
        {
            var sr = visual.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = active;
            }
        }
        else
        {
            visual.SetActive(active);
        }
    }

    private void InitVisuals()
    {
        openHandVisual = EnsureVisualInstance(openHandVisual, "OpenHandVisual");
        fistVisual = EnsureVisualInstance(fistVisual, "FistVisual");
        scratchVisual = EnsureVisualInstance(scratchVisual, "ScratchVisual");

        SetVisualActive(openHandVisual, false);
        SetVisualActive(fistVisual, false);
        SetVisualActive(scratchVisual, false);
    }

    private GameObject EnsureVisualInstance(GameObject visualPrefab, string name)
    {
        if (visualPrefab == null) return null;

        if (visualPrefab.transform.IsChildOf(transform))
        {
            return visualPrefab;
        }

        GameObject instance = Instantiate(visualPrefab, transform);
        instance.name = name;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        return instance;
    }

    private async UniTaskVoid RoamAndAttackLoop()
    {
        Debug.Log($"<color=green>[하데스 손 ({side})]</color> RoamAndAttackLoop 진입 완료!");
        // Delay slightly for initial setup
        await UniTask.Delay(100, cancellationToken: cancelToken.Token);

        while (!isDying && !cancelToken.IsCancellationRequested)
        {
            // 1. Roam a random number of steps
            int stepsToTake = Random.Range(minStepsBetweenAttacks, maxStepsBetweenAttacks + 1);
            Debug.Log($"<color=green>[하데스 손 ({side})]</color> {stepsToTake}걸음 이동 결정. 경로 길이: {(_perimeterPath != null ? _perimeterPath.Count : 0)}");
            for (int i = 0; i < stepsToTake; i++)
            {
                if (isDying || cancelToken.IsCancellationRequested) break;
                if (_perimeterPath == null || _perimeterPath.Count == 0)
                {
                    Debug.LogWarning($"<color=red>[하데스 손 ({side})]</color> 이동 경로가 비어 있습니다!");
                    break;
                }

                if (_moveClockwise)
                    _currentPathIndex = (_currentPathIndex + 1) % _perimeterPath.Count;
                else
                    _currentPathIndex = (_currentPathIndex - 1 + _perimeterPath.Count) % _perimeterPath.Count;

                Vector2Int nextPos = _perimeterPath[_currentPathIndex];
                Debug.Log($"<color=green>[하데스 손 ({side})]</color> {i + 1}/{stepsToTake} 걸음: {nextPos} 셀로 이동.");
                await MoveToCell(nextPos);

                await UniTask.Delay(roamStepDelayMs, cancellationToken: cancelToken.Token);
            }

            if (isDying || cancelToken.IsCancellationRequested) break;

            // 2. Perform attack based on current position
            Debug.Log($"<color=green>[하데스 손 ({side})]</color> Roam 완료. 현재 위치: {_gridPos}, 공격 패턴 실행 시작.");
            await ExecuteAttackAtCurrentPosition();
        }
    }

    private async UniTask MoveToCell(Vector2Int nextPos)
    {
        _gridPos = nextPos;
        UpdateVisualForPosition(_gridPos);

        if (GridSystem.Instance != null)
        {
            Vector3 targetWorld = GridSystem.Instance.GridToWorld(nextPos);
            await transform.DOMove(targetWorld, roamMoveDuration).SetEase(Ease.InOutSine).AsyncWaitForCompletion();
        }
    }

    private void UpdatePathIndexToCurrentPosition()
    {
        if (_perimeterPath == null || _perimeterPath.Count == 0) return;

        Vector2Int currentGridPos = _gridPos;
        if (GridSystem.Instance != null)
        {
            currentGridPos = GridSystem.Instance.WorldToGrid(transform.position);
        }

        int closestIndex = 0;
        int minDistance = int.MaxValue;

        for (int i = 0; i < _perimeterPath.Count; i++)
        {
            int dist = Mathf.Abs(_perimeterPath[i].x - currentGridPos.x) + Mathf.Abs(_perimeterPath[i].y - currentGridPos.y);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }

        _currentPathIndex = closestIndex;
        _gridPos = _perimeterPath[_currentPathIndex];

        if (GridSystem.Instance != null)
        {
            transform.position = GridSystem.Instance.GridToWorld(_gridPos);
        }
        UpdateVisualForPosition(_gridPos);
    }

    private void UpdateVisualForPosition(Vector2Int position)
    {
        HandMode mode = GetModeForPosition(position);
        UpdateVisualMode(mode);
    }

    private HandMode GetModeForPosition(Vector2Int position)
    {
        if (!hasMapBounds) CacheMapBounds();

        if (position.y >= mapBounds.yMax)
        {
            return HandMode.Fist;
        }
        else if (position.y < mapBounds.yMin)
        {
            return HandMode.Scratch;
        }
        else
        {
            return HandMode.OpenHand;
        }
    }

    private void UpdateVisualMode(HandMode mode)
    {
        SetVisualActive(openHandVisual, mode == HandMode.OpenHand);
        SetVisualActive(fistVisual, mode == HandMode.Fist);
        SetVisualActive(scratchVisual, mode == HandMode.Scratch);
    }

    private async UniTask ExecuteAttackAtCurrentPosition()
    {
        isBossAttacking = true;
        _lastRoamPos = _gridPos;

        HandMode mode = GetModeForPosition(_gridPos);

        switch (mode)
        {
            case HandMode.Fist:
                await ExecuteFistMode();
                break;
            case HandMode.OpenHand:
                await ExecuteOpenHandMode();
                break;
            case HandMode.Scratch:
                await ExecuteScratchMode();
                break;
        }

        isBossAttacking = false;
    }

    private async UniTask ExecuteFistMode()
    {
        if (player == null || player.IsDead) return;

        Vector2Int targetCenter = player.GridPosition;

        // 3x3 cells centered on player
        List<Vector2Int> targetCells = new List<Vector2Int>();
        for (int x = targetCenter.x - 1; x <= targetCenter.x + 1; x++)
        {
            for (int y = targetCenter.y - 1; y <= targetCenter.y + 1; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (GridSystem.Instance.IsInBounds(cell))
                {
                    targetCells.Add(cell);
                }
            }
        }

        // 1. Telegraph
        ShowWarning(targetCells);
        UpdateVisualMode(HandMode.Fist);
        await UniTask.Delay((int)(fistWarningDuration * 1000), cancellationToken: cancelToken.Token);
        ClearWarning(targetCells);

        if (isDying || cancelToken.IsCancellationRequested) return;

        // 2. Slam Down
        _gridPos = targetCenter;
        Vector3 slamTargetWorld = GridSystem.Instance.GridToWorld(targetCenter);
        Vector3 slamStartWorld = slamTargetWorld + new Vector3(0, 8f, 0);

        transform.position = slamStartWorld;

        // Drop fast
        await transform.DOMove(slamTargetWorld, slamSpeed).SetEase(Ease.InQuad).AsyncWaitForCompletion();

        if (isDying || cancelToken.IsCancellationRequested) return;

        // Camera Shake
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.Shake(0.3f, 0.4f);
        }

        // Hit check & knockback
        if (player != null && !player.IsDead && targetCells.Contains(player.GridPosition))
        {
            player.TakeDamage(fistSlamDamage);

            Vector2Int pushDir = player.GridPosition - targetCenter;
            if (pushDir == Vector2Int.zero) pushDir = Vector2Int.down;
            Vector2Int kbTarget = FindKnockbackTargetOutsideRange(player.GridPosition, targetCells, pushDir);
            player.Movement.Knockback(kbTarget, 0.2f);
        }

        // 3. Stay down and be attackable
        _isAttackable = true;
        await UniTask.Delay((int)(slamStayDuration * 1000), cancellationToken: cancelToken.Token);
        _isAttackable = false;

        if (isDying || cancelToken.IsCancellationRequested) return;

        // 4. Return to perimeter
        Vector3 returnWorldPos = GridSystem.Instance.GridToWorld(_lastRoamPos);
        await transform.DOMove(returnWorldPos, 0.4f).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
        _gridPos = _lastRoamPos;
        UpdateVisualForPosition(_gridPos);
    }

    private async UniTask ExecuteOpenHandMode()
    {
        if (!hasMapBounds) CacheMapBounds();

        int targetY = _gridPos.y;

        List<Vector2Int> targetCells = new List<Vector2Int>();
        for (int x = mapBounds.xMin; x <= mapBounds.xMax; x++)
        {
            Vector2Int cell = new Vector2Int(x, targetY);
            if (IsInAttackFloor(cell))
            {
                targetCells.Add(cell);
            }
        }

        // 1. Telegraph
        ShowWarning(targetCells);
        UpdateVisualMode(HandMode.OpenHand);
        await UniTask.Delay((int)(warningDuration * 1000), cancellationToken: cancelToken.Token);
        ClearWarning(targetCells);

        if (isDying || cancelToken.IsCancellationRequested) return;

        // 2. Sweep: Left Hand sweeps Right -> Left, Right Hand sweeps Left -> Right
        int startX = (side == HandSide.Left) ? mapBounds.xMax + 2 : mapBounds.xMin - 2;
        int endX = (side == HandSide.Left) ? mapBounds.xMin - 2 : mapBounds.xMax + 2;
        Vector2Int sweepDir = (side == HandSide.Left) ? Vector2Int.left : Vector2Int.right;

        Vector3 startWorld = GridSystem.Instance.GridToWorld(new Vector2Int(startX, targetY));
        Vector3 endWorld = GridSystem.Instance.GridToWorld(new Vector2Int(endX, targetY));

        transform.position = startWorld;

        // Start sweep
        transform.DOMove(endWorld, sweepSpeed).SetEase(Ease.Linear);

        float hitDelay = 0.25f;
        if (player != null && !player.IsDead && targetCells.Contains(player.GridPosition))
        {
            float totalDist = Mathf.Abs(endX - startX);
            if (totalDist > 0f)
            {
                float playerDist = Mathf.Abs(player.GridPosition.x - startX);
                float progress = playerDist / totalDist;
                hitDelay = progress * sweepSpeed;
            }
        }

        await UniTask.Delay((int)(hitDelay * 1000), cancellationToken: cancelToken.Token);

        if (isDying || cancelToken.IsCancellationRequested) return;

        if (player != null && !player.IsDead && player.GridPosition.y == targetY && targetCells.Contains(player.GridPosition))
        {
            player.TakeDamage(sweepDamage);
            Vector2Int kbTarget = FindKnockbackTargetOutsideRange(player.GridPosition, targetCells, sweepDir);
            player.Movement.Knockback(kbTarget, 0.2f);
        }

        float remaining = Mathf.Max(0f, sweepSpeed - hitDelay);
        if (remaining > 0f)
        {
            await UniTask.Delay((int)(remaining * 1000), cancellationToken: cancelToken.Token);
        }

        if (isDying || cancelToken.IsCancellationRequested) return;

        // Stay at the end of the sweep and update the path index
        UpdatePathIndexToCurrentPosition();
    }

    private async UniTask ExecuteScratchMode()
    {
        if (!hasMapBounds) CacheMapBounds();

        int targetX = _gridPos.x;

        // Target cells: Left Hand sweeps only 4 cells, Right Hand sweeps full column
        List<Vector2Int> targetCells = new List<Vector2Int>();
        int startY = mapBounds.yMin;
        int endY = (side == HandSide.Left) ? mapBounds.yMin + 3 : mapBounds.yMax;

        for (int y = startY; y <= endY; y++)
        {
            Vector2Int cell = new Vector2Int(targetX, y);
            if (IsInAttackFloor(cell))
            {
                targetCells.Add(cell);
            }
        }

        // 1. Telegraph
        ShowWarning(targetCells);
        UpdateVisualMode(HandMode.Scratch);
        await UniTask.Delay((int)(warningDuration * 1000), cancellationToken: cancelToken.Token);
        ClearWarning(targetCells);

        if (isDying || cancelToken.IsCancellationRequested) return;

        // 2. Sweep Bottom -> Top
        int sweepStartY = mapBounds.yMin - 2;
        int sweepEndY = endY + 2;

        Vector3 startWorld = GridSystem.Instance.GridToWorld(new Vector2Int(targetX, sweepStartY));
        Vector3 endWorld = GridSystem.Instance.GridToWorld(new Vector2Int(targetX, sweepEndY));

        transform.position = startWorld;

        // Start sweep
        transform.DOMove(endWorld, sweepSpeed).SetEase(Ease.Linear);

        float hitDelay = 0.25f;
        if (player != null && !player.IsDead && targetCells.Contains(player.GridPosition))
        {
            float totalDist = Mathf.Abs(sweepEndY - sweepStartY);
            if (totalDist > 0f)
            {
                float playerDist = Mathf.Abs(player.GridPosition.y - sweepStartY);
                float progress = playerDist / totalDist;
                hitDelay = progress * sweepSpeed;
            }
        }

        await UniTask.Delay((int)(hitDelay * 1000), cancellationToken: cancelToken.Token);

        if (isDying || cancelToken.IsCancellationRequested) return;

        if (player != null && !player.IsDead && player.GridPosition.x == targetX && targetCells.Contains(player.GridPosition))
        {
            player.TakeDamage(scratchDamage);
            Vector2Int kbTarget = FindKnockbackTargetOutsideRange(player.GridPosition, targetCells, Vector2Int.up);
            player.Movement.Knockback(kbTarget, 0.2f);
        }

        float remaining = Mathf.Max(0f, sweepSpeed - hitDelay);
        if (remaining > 0f)
        {
            await UniTask.Delay((int)(remaining * 1000), cancellationToken: cancelToken.Token);
        }

        if (isDying || cancelToken.IsCancellationRequested) return;

        // Stay at the end of the sweep and update the path index
        UpdatePathIndexToCurrentPosition();
    }

    private Vector2Int FindKnockbackTargetOutsideRange(Vector2Int playerPos, List<Vector2Int> attackCells, Vector2Int preferredDir)
    {
        for (int dist = 1; dist <= knockbackDist + 2; dist++)
        {
            Vector2Int candidate = playerPos + preferredDir * dist;
            if (GridSystem.Instance != null && GridSystem.Instance.IsWalkable(candidate) && !attackCells.Contains(candidate))
            {
                return candidate;
            }
        }

        Vector2Int[] dirs = { preferredDir, new Vector2Int(-preferredDir.y, preferredDir.x), new Vector2Int(preferredDir.y, -preferredDir.x), -preferredDir };
        for (int dist = 1; dist <= knockbackDist + 2; dist++)
        {
            foreach (var dir in dirs)
            {
                Vector2Int candidate = playerPos + dir * dist;
                if (GridSystem.Instance != null && GridSystem.Instance.IsWalkable(candidate) && !attackCells.Contains(candidate))
                {
                    return candidate;
                }
            }
        }
        return playerPos;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        if (onlyDamageableWhenSlammed && !_isAttackable)
        {
            Debug.Log("<color=gray>[하데스 손]</color> 내려쳤을 때만 공격할 수 있습니다!");
            return;
        }

        int beforeHp = currentHp;
        currentHp = Mathf.Max(0, currentHp - amount);
        int finalDamage = Mathf.Max(0, beforeHp - currentHp);

        if (finalDamage > 0)
        {
            GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(
                transform.position,
                finalDamage.ToString(),
                finalDamage,
                CombatTextType.Damage
            ));
        }

        if (hpBar != null) hpBar.UpdateBar();

        if (_ownerBoss != null)
        {
            _ownerBoss.OnHandDamaged(finalDamage);
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        if (hpBar != null) hpBar.UpdateBar();
    }

    private void Die()
    {
        isDying = true;
        ClearAllWarnings();

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.Unregister(this);
        }

        if (_ownerBoss != null)
        {
            _ownerBoss.OnHandDefeated(this);
        }

        transform.DOScale(0f, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.Unregister(this);
        }
    }
}
