using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;

public class LastBossHead : BossBase, IGridEntity, IMultiCellGridEntity, IDamageable
{
    private LastBoss _owner;
    private Vector2Int _gridPos;
    private int _moveDir = 1;

    [Header("배회 설정")]
    [SerializeField] private float wanderMoveDuration = 0.25f;
    [SerializeField] private int wanderStepIntervalMs = 500;
    [SerializeField] private int wanderStepsBeforePattern = 3;

    [Header("패턴1 레이저 설정")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private float laserVisualThickness = 3.5f;
    [SerializeField] private int laserDamage = 20;
    [SerializeField] private int laserTelegraphMs = 1500;
    [SerializeField] private int laserActiveMs = 800;

    [Header("패턴2 운석 설정")]
    [SerializeField] private int meteorDamage = 30;
    [SerializeField] private int meteorTelegraphMs = 1500;
    [SerializeField] private GameObject meteorPrefab; // Optional meteor visual effect

    private int _minX;
    private int _maxX;
    private int _topY;
    private bool _isRegisteredToTurnManager = false;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => currentHp <= 0;

    public override Vector2Int GridPosition => _gridPos;
    public override Vector2Int BossSize => new Vector2Int(5, 1);

    public IEnumerable<Vector2Int> OccupiedCells
    {
        get
        {
            if (IsDead) yield break;
            
            // 5x1 horizontal hitbox starting from GridPosition
            for (int x = 0; x < 5; x++)
            {
                yield return _gridPos + new Vector2Int(x, 0);
            }
        }
    }

    public void Init(LastBoss owner, Vector2Int gridPos, int maxHpValue)
    {
        _owner = owner;
        _gridPos = gridPos;
        maxHp = maxHpValue;
        currentHp = maxHpValue;

        // Position the head visually. The 5x1 cells extend to the right of _gridPos.
        // Therefore, the middle cell (index 2) represents the horizontal pivot center.
        if (GridSystem.Instance != null)
        {
            Vector3 centerWorldPos = GridSystem.Instance.GridToWorld(_gridPos + new Vector2Int(2, 0));
            transform.position = centerWorldPos;
            
            // Register occupancy database
            RegisterGridOccupancy();
        }

        // Cache map width bounds of AttackFloor for horizontal movement range
        CacheAttackFloorWidthBounds();

        // Register to TurnManager
        RegisterBossGridTarget();

        // Add action cases for BossBase pattern random rolling
        actionCases.Clear();
        AddActionCase(BossAction.Pattern1, 50); // Laser pattern
        AddActionCase(BossAction.Pattern2, 50); // Meteor + Lava pattern

        Debug.Log($"<color=red>[하데스 머리]</color> 소환 완료! 위치: {_gridPos}, 5x1 영역 등록.");
    }

    private void CacheAttackFloorWidthBounds()
    {
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        bool found = false;

        if (GridSystem.Instance != null)
        {
            foreach (var cell in GridSystem.Instance.GetAllWalkableCells())
            {
                if (IsInAttackFloor(cell))
                {
                    found = true;
                    if (cell.x < minX) minX = cell.x;
                    if (cell.x > maxX) maxX = cell.x;
                    if (cell.y > maxY) maxY = cell.y;
                }
            }
        }

        if (!found)
        {
            minX = mapBounds.xMin;
            maxX = mapBounds.xMax - 1;
            maxY = mapBounds.yMax - 1;
        }

        _minX = minX;
        _maxX = maxX;
        _topY = _gridPos.y;
    }

    private void RegisterBossGridTarget()
    {
        if (_isRegisteredToTurnManager) return;
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.Register(this);
            _isRegisteredToTurnManager = true;
        }
    }

    private void UnregisterBossGridTarget()
    {
        if (_isRegisteredToTurnManager && TurnManager.Instance != null)
        {
            TurnManager.Instance.Unregister(this);
        }
        _isRegisteredToTurnManager = false;
    }

    private void RegisterGridOccupancy()
    {
        if (GridSystem.Instance != null)
        {
            for (int x = 0; x < 5; x++)
            {
                GridSystem.Instance.RegisterOccupancy(_gridPos + new Vector2Int(x, 0), this);
            }
        }
    }

    private void UnregisterGridOccupancy()
    {
        if (GridSystem.Instance != null)
        {
            for (int x = 0; x < 5; x++)
            {
                GridSystem.Instance.UnRegisterOccupancy(_gridPos + new Vector2Int(x, 0));
            }
        }
    }

    public void TakeDamage(int amount)
    {
        BossTakeDamage(amount);
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        if (hpBar != null) hpBar.UpdateBar();
    }

    protected override async UniTask PhaseState()
    {
        // Force evaluation of Phase1 patterns for head combat loop
        await Phase1BossPattern();
    }

    protected override async UniTask Phase1BossPattern()
    {
        isBossAttacking = true;
        
        if (!hasMapBounds) CacheMapBounds();

        // 1. Horizontal movement steps
        await WanderStepsAsync(wanderStepsBeforePattern);

        if (cancelToken.IsCancellationRequested || isDying)
        {
            isBossAttacking = false;
            return;
        }

        // 2. Select pattern based on chance
        BossAction bossAction = BossPatternPer();
        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);

        if (cancelToken.IsCancellationRequested || isDying)
        {
            isBossAttacking = false;
            return;
        }

        // 3. Execute pattern
        switch (bossAction)
        {
            case BossAction.Pattern1:
                await HeadLaserPattern();
                break;
            case BossAction.Pattern2:
                await HeadMeteorPattern();
                break;
            default:
                await HeadLaserPattern();
                break;
        }

        isBossAttacking = false;
    }

    private async UniTask WanderStepsAsync(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            if (cancelToken.IsCancellationRequested || isDying) break;

            int nextX = _gridPos.x + _moveDir;
            if (nextX < _minX || nextX > _maxX - 4)
            {
                _moveDir = -_moveDir;
                nextX = _gridPos.x + _moveDir;
            }

            if (nextX >= _minX && nextX <= _maxX - 4)
            {
                UnregisterGridOccupancy();
                _gridPos.x = nextX;
                RegisterGridOccupancy();

                Vector3 targetWorld = GridSystem.Instance.GridToWorld(_gridPos + new Vector2Int(2, 0));
                
                await transform.DOMove(targetWorld, wanderMoveDuration)
                    .SetEase(Ease.InOutSine)
                    .AsyncWaitForCompletion();
            }

            await UniTask.Delay(wanderStepIntervalMs, cancellationToken: cancelToken.Token);
        }
    }

    // 패턴 1: 좌측 2줄 혹은 우측 2줄 레이저 공격
    private async UniTask HeadLaserPattern()
    {
        if (player == null || player.IsDead) return;

        bool isLeftLaser = Random.value < 0.5f;
        List<Vector2Int> laserCells = new List<Vector2Int>();

        int targetMinX = isLeftLaser ? _minX : _maxX - 1;
        int targetMaxX = isLeftLaser ? _minX + 1 : _maxX;

        for (int x = targetMinX; x <= targetMaxX; x++)
        {
            for (int y = mapBounds.yMin; y < mapBounds.yMax; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (GridSystem.Instance.IsInBounds(cell))
                {
                    laserCells.Add(cell);
                }
            }
        }

        ShowWarning(laserCells);

        try
        {
            await UniTask.Delay(laserTelegraphMs, cancellationToken: cancelToken.Token);

            if (cancelToken.IsCancellationRequested || isDying) return;

            // Spawn laser visual effect (using rotated horizontal lasers vertically)
            GameObject laserObj = null;
            if (laserPrefab != null)
            {
                Vector3 vStart = GridSystem.Instance.GridToWorld(new Vector2Int(targetMinX, mapBounds.yMin));
                Vector3 vEnd = GridSystem.Instance.GridToWorld(new Vector2Int(targetMaxX, mapBounds.yMax - 1));
                Vector3 vCenter = (vStart + vEnd) * 0.5f;

                laserObj = Instantiate(laserPrefab, vCenter, Quaternion.Euler(0, 0, 90));
                float vLength = Vector3.Distance(vStart, vEnd);
                laserObj.transform.localScale = new Vector3(vLength, laserVisualThickness, 1f);
            }

            // Deal damage
            DamagePlayerIfInCells(laserCells, laserDamage);

            await UniTask.Delay(laserActiveMs, cancellationToken: cancelToken.Token);

            if (laserObj != null) Destroy(laserObj);
        }
        finally
        {
            ClearWarning(laserCells);
        }
    }

    // 패턴 2: 3x3 운석 낙하
    private async UniTask HeadMeteorPattern()
    {
        if (player == null || player.IsDead) return;

        Vector2Int playerCell = player.GridPosition;
        List<Vector2Int> targetCells = new List<Vector2Int>();

        for (int x = playerCell.x - 1; x <= playerCell.x + 1; x++)
        {
            for (int y = playerCell.y - 1; y <= playerCell.y + 1; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (GridSystem.Instance.IsInBounds(cell))
                {
                    targetCells.Add(cell);
                }
            }
        }

        ShowWarning(targetCells);

        try
        {
            await UniTask.Delay(meteorTelegraphMs, cancellationToken: cancelToken.Token);

            if (cancelToken.IsCancellationRequested || isDying) return;

            // Optional Meteor spawn visual
            if (meteorPrefab != null)
            {
                Vector3 spawnPos = GridSystem.Instance.GridToWorld(playerCell);
                GameObject effect = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
                Destroy(effect, 1.5f);
            }

            // Deal damage to player
            DamagePlayerIfInCells(targetCells, meteorDamage);

            await UniTask.Delay(500, cancellationToken: cancelToken.Token);
        }
        finally
        {
            ClearWarning(targetCells);
        }
    }

    protected override async UniTask OnDeath()
    {
        UnregisterBossGridTarget();
        UnregisterGridOccupancy();
        
        Debug.Log("<color=red>[하데스 머리]</color> 완전히 패배함. 소멸 연출 시작.");

        // Clear boss battle exit barriers
        await base.OnDeath();

        // Destroy owner controller if alive
        if (_owner != null)
        {
            Destroy(_owner.gameObject);
        }

        // Dissolve animation
        await transform.DOScale(0f, 0.8f).SetEase(Ease.InBack).AsyncWaitForCompletion();
        Destroy(gameObject);
    }

    public void OnTurnUpdate()
    {
        // Runs independently on real-time loops, satisfying IGridEntity.
    }

    private void OnDisable()
    {
        UnregisterBossGridTarget();
        UnregisterGridOccupancy();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        UnregisterBossGridTarget();
        UnregisterGridOccupancy();
    }
}

