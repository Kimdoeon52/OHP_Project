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
    [SerializeField] private GameObject laserWarningPrefab; // Laser warning prefab
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
    private bool _isInitialized = false;

    [Header("프록시 설정")]
    [SerializeField] private Vector2Int bossSize = new Vector2Int(5, 1);
    private Vector2Int gridPos => _gridPos;

    // 피격 이펙트 관련 필드
    private SpriteRenderer[] _spriteRenderers;
    private Dictionary<SpriteRenderer, Color> _originalColors = new Dictionary<SpriteRenderer, Color>();
    private List<Tween> _activeTweens = new List<Tween>();

    #region 플레이어 이동공격 대상 등록 프록시
    public override Vector2Int BossSize => bossSize;
    public override Vector2Int GridPosition => gridPos;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => currentHp <= 0;

    public IEnumerable<Vector2Int> OccupiedCells
    {
        get
        {
            if (IsDead)
                yield break;

            // 플레이어 공격이 닿는 범위(maxY)를 포함하여 3라인 판정 구성
            for (int x = 0; x < bossSize.x; x++)
            {
                yield return _gridPos + new Vector2Int(x, 0);   // 몸통 줄 (maxY + 1)
                yield return _gridPos + new Vector2Int(x, -1);  // 플레이어 피격 가능 줄 (maxY)
                yield return _gridPos + new Vector2Int(x, 1);   // 머리 윗 줄 (maxY + 2)
            }
        }
    }

    private Vector3 BossPivotOffset => new Vector3((bossSize.x - 1) * 0.5f, (bossSize.y - 1) * 0.5f, 0f);



    public void TakeDamage(int amount)
    {
        if (hpBar == null)
        {
            hpBar = FindFirstObjectByType<BossHpBar>(FindObjectsInactive.Include);
        }

        if (hpBar != null)
        {
            hpBar.Init(this);
        }

        BossTakeDamage(amount);
        PlayHitEffect();
    }

    public virtual void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        if (hpBar == null)
        {
            hpBar = FindFirstObjectByType<BossHpBar>(FindObjectsInactive.Include);
        }
        if (hpBar != null) hpBar.UpdateBar();
    }

    public void OnTurnUpdate()
    {
    }

    private void RegisterBossGridTarget()
    {
        if (TurnManager.Instance == null)
        {
            Debug.LogWarning("BossB: TurnManager가 없어 플레이어 이동공격 대상으로 등록하지 못했습니다.");
            return;
        }

        TurnManager.Instance.Register(this);
    }

    private void UnregisterBossGridTarget()
    {
        TurnManager.Instance?.Unregister(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnregisterBossGridTarget();
        UnregisterGridOccupancy();
        UnblockGridOccupancy();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_isInitialized)
        {
            RegisterBossGridTarget();
            RegisterGridOccupancy();
            BlockGridOccupancy();
            Debug.Log($"<color=red>[하데스 머리]</color> 재활성화로 인해 TurnManager, 격자 점유 및 진입 블록 복구 완료.");
        }
    }
    #endregion

    public void Init(LastBoss owner, Vector2Int gridPos, int maxHpValue, UnityEngine.Tilemaps.Tilemap warningMap, UnityEngine.Tilemaps.TileBase warningTileAsset)
    {
        _owner = owner;
        _gridPos = gridPos;
        maxHp = maxHpValue;
        currentHp = maxHpValue;

        // Assign warning tilemap references passed from owner boss to resolve missing inspector references
        this.warningTilemap = warningMap;
        this.warningTile = warningTileAsset;

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

        // 플레이어가 최상단 바닥 줄로 올라서서 머리와 겹치거나 통과해버리는 오작동을 차단하기 위해,
        // 머리가 움직이는 가로 영역 범위 전체의 Y = _gridPos.y - 1 (즉, maxY) 줄을 블록 처리하여 플레이어 진입을 완전히 가로막습니다.
        BlockGridOccupancy();

        // Register to TurnManager
        RegisterBossGridTarget();

        // Add action cases for BossBase pattern random rolling
        actionCases.Clear();
        AddActionCase(BossAction.Pattern1, 50); // Laser pattern
        AddActionCase(BossAction.Pattern2, 50); // Meteor pattern

        _isInitialized = true;
        Debug.Log($"<color=red>[하데스 머리]</color> 소환 완료! 위치: {_gridPos}, 5x1 영역 등록 및 최상단 바닥 라인 진입 블록 완료.");
    }

    private void CacheAttackFloorWidthBounds()
    {
        if (!hasMapBounds) CacheMapBounds();

        _minX = mapBounds.xMin;
        _maxX = mapBounds.xMax - 1;
        _topY = _gridPos.y;
    }

    private void RegisterGridOccupancy()
    {
        if (GridSystem.Instance != null)
        {
            for (int x = 0; x < bossSize.x; x++)
            {
                // 몸통 줄 (maxY + 1) 점유 등록
                GridSystem.Instance.RegisterOccupancy(_gridPos + new Vector2Int(x, 0), this);
                // 플레이어가 때릴 수 있는 최상단 바닥 줄 (maxY) 점유 등록하여 플레이어 진입을 막고 정상 이동공격(TryMeleeAt) 유도
                GridSystem.Instance.RegisterOccupancy(_gridPos + new Vector2Int(x, -1), this);
            }
        }
    }

    private void UnregisterGridOccupancy()
    {
        if (GridSystem.Instance != null)
        {
            for (int x = 0; x < bossSize.x; x++)
            {
                GridSystem.Instance.UnRegisterOccupancy(_gridPos + new Vector2Int(x, 0));
                GridSystem.Instance.UnRegisterOccupancy(_gridPos + new Vector2Int(x, -1));
            }
        }
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
                await HeadLaserSweepPattern();
                break;
            case BossAction.Pattern2:
                await HeadLavaPattern();
                break;
            default:
                await HeadLaserSweepPattern();
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
                Vector3 targetWorld = GridSystem.Instance.GridToWorld(new Vector2Int(nextX, _gridPos.y) + new Vector2Int(2, 0));
                
                await transform.DOMove(targetWorld, wanderMoveDuration)
                    .SetEase(Ease.InOutSine)
                    .AsyncWaitForCompletion();

                UnregisterGridOccupancy();
                _gridPos.x = nextX;
                RegisterGridOccupancy();

                Debug.Log($"<color=red>[하데스 머리 이동 완료]</color> 현재 머리 격자 점유 영역: (X: {_gridPos.x} ~ {_gridPos.x + 4}, Y: {_gridPos.y})");
            }

            await UniTask.Delay(wanderStepIntervalMs, cancellationToken: cancelToken.Token);
        }
    }

    // 패턴 1: 보스가 한쪽 끝으로 이동 후 레이저를 뿜은 채 반대편 끝으로 스윕
    private async UniTask HeadLaserSweepPattern()
    {
        if (player == null || player.IsDead) return;
        GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.LastBoss_HeadLaser);

        // 1. 좌측 혹은 우측 중 하나를 결정해 보스를 해당 끝으로 빠르게 이동
        bool moveToLeft = Random.value < 0.5f;
        int startX = moveToLeft ? _minX : _maxX - 4;
        int oppositeX = moveToLeft ? _maxX - 4 : _minX;

        Vector3 startWorld = GridSystem.Instance.GridToWorld(new Vector2Int(startX, _gridPos.y) + new Vector2Int(2, 0));
        // 빠르게 (0.5초) 이동
        await transform.DOMoveX(startWorld.x, 0.5f)
            .SetEase(Ease.OutQuad)
            .AsyncWaitForCompletion();

        UnregisterGridOccupancy();
        _gridPos.x = startX;
        RegisterGridOccupancy();

        if (cancelToken.IsCancellationRequested || isDying) return;

        // 2. 머리 중앙 컬럼(startX + 2)에 레이저 경고 표시
        int laserStartX = startX + 2;
        List<Vector2Int> laserCells = new List<Vector2Int>();
        for (int y = mapBounds.yMin; y < mapBounds.yMax; y++)
        {
            Vector2Int cell = new Vector2Int(laserStartX, y);
            if (GridSystem.Instance.IsInBounds(cell))
            {
                laserCells.Add(cell);
            }
        }

        ShowWarning(laserCells);

        GameObject warningLaserObj = null;
        try
        {
            // Spawn laser warning prefab (similar to BossA / LazerPattern)
            if (laserWarningPrefab != null)
            {
                Vector3 vStart = GridSystem.Instance.GridToWorld(new Vector2Int(laserStartX, mapBounds.yMin));
                Vector3 vEnd = GridSystem.Instance.GridToWorld(new Vector2Int(laserStartX, mapBounds.yMax - 1));
                Vector3 vCenter = (vStart + vEnd) * 0.5f;

                warningLaserObj = Instantiate(laserWarningPrefab, vCenter, Quaternion.Euler(0, 0, 90));
                float vLength = Vector3.Distance(vStart, vEnd);
                warningLaserObj.transform.localScale = new Vector3(vLength, laserVisualThickness, 1f);
            }

            await UniTask.Delay(laserTelegraphMs, cancellationToken: cancelToken.Token);

            if (cancelToken.IsCancellationRequested || isDying) return;

            // Destroy warning prefab before firing the real laser
            if (warningLaserObj != null)
            {
                Destroy(warningLaserObj);
                warningLaserObj = null;
            }
            ClearWarning(laserCells); // 주황색 경고 장판도 제거

            // 레이저 공격 애니메이션 시작
            if (bossAnimation != null)
            {
                bossAnimation.SetBool("HeadAttack", true);
            }

            // 3. 진짜 레이저 생성 및 반대편 끝으로 부드럽게 쓸기 (1.5초 소요)
            GameObject laserObj = null;
            if (laserPrefab != null)
            {
                Vector3 vStart = GridSystem.Instance.GridToWorld(new Vector2Int(laserStartX, mapBounds.yMin));
                Vector3 vEnd = GridSystem.Instance.GridToWorld(new Vector2Int(laserStartX, mapBounds.yMax - 1));
                Vector3 vCenter = (vStart + vEnd) * 0.5f;

                laserObj = Instantiate(laserPrefab, vCenter, Quaternion.Euler(0, 0, 90));
                float vLength = Vector3.Distance(vStart, vEnd);
                laserObj.transform.localScale = new Vector3(vLength, laserVisualThickness, 1f);
            }

            Vector3 oppositeWorld = GridSystem.Instance.GridToWorld(new Vector2Int(oppositeX, _gridPos.y) + new Vector2Int(2, 0));
            bool hasDamagedPlayer = false;

            // DOTween 이동 (1.5초 동안 선형 이동)
            var sweepTween = transform.DOMoveX(oppositeWorld.x, 1.5f).SetEase(Ease.Linear);

            while (sweepTween.IsActive() && !sweepTween.IsComplete())
            {
                if (cancelToken.IsCancellationRequested || isDying)
                {
                    sweepTween.Kill();
                    break;
                }

                // 레이저 오브젝트 위치를 보스 머리 중앙 X축으로 실시간 갱신
                if (laserObj != null)
                {
                    float currentX = transform.position.x;
                    Vector3 vStart = GridSystem.Instance.GridToWorld(new Vector2Int(0, mapBounds.yMin));
                    Vector3 vEnd = GridSystem.Instance.GridToWorld(new Vector2Int(0, mapBounds.yMax - 1));
                    float vCenterY = (vStart.y + vEnd.y) * 0.5f;

                    laserObj.transform.position = new Vector3(currentX, vCenterY, laserObj.transform.position.z);
                }

                // 플레이어와 레이저 컬럼 충돌 판정
                if (!hasDamagedPlayer && player != null && !player.IsDead)
                {
                    // 현재 보스 X 좌표에 대응하는 그리드 컬럼 계산
                    Vector2Int currentHeadGrid = GridSystem.Instance.WorldToGrid(transform.position);
                    int currentLaserGridX = currentHeadGrid.x;

                    if (player.GridPosition.x == currentLaserGridX)
                    {
                        player.TakeDamage(laserDamage);
                        hasDamagedPlayer = true; // 스윕당 1회 피격 제한
                        Debug.Log($"<color=orange>[하데스 레이저 스윕]</color> 플레이어가 레이저 범위 X={currentLaserGridX} 에 닿았습니다!");
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: cancelToken.Token);
            }

            // 위치 정렬 보정
            transform.position = new Vector3(oppositeWorld.x, transform.position.y, transform.position.z);

            UnregisterGridOccupancy();
            _gridPos.x = oppositeX;
            RegisterGridOccupancy();

            if (laserObj != null) Destroy(laserObj);
        }
        finally
        {
            if (warningLaserObj != null) Destroy(warningLaserObj);
            if (bossAnimation != null)
            {
                bossAnimation.SetBool("HeadAttack", false);
            }
        }
    }

    // 패턴 2: 빠르게 하늘에서 용암이 5번 순차 낙하하며 바닥 안착 시 1.5배로 확대
    private async UniTask HeadLavaPattern()
    {
        if (player == null || player.IsDead) return;
        GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.LastBoss_Meteor);

        for (int i = 0; i < 5; i++)
        {
            if (cancelToken.IsCancellationRequested || isDying || player == null || player.IsDead) break;

            // 플레이어의 실시간 최신 위치를 타겟팅
            Vector2Int targetCell = player.GridPosition;

            // 3x3 범위 계산
            List<Vector2Int> targetCells = new List<Vector2Int>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2Int cell = targetCell + new Vector2Int(x, y);
                    if (GridSystem.Instance != null && GridSystem.Instance.IsInBounds(cell))
                    {
                        targetCells.Add(cell);
                    }
                }
            }

            // 1. 3x3 주황색 경고 장판 0.3초 노출
            ShowWarning(targetCells);
            await UniTask.Delay(300, cancellationToken: cancelToken.Token);

            if (cancelToken.IsCancellationRequested || isDying)
            {
                ClearWarning(targetCells);
                break;
            }

            // 2. 대각선 우상단 낙하 연출 (0.3초 소요)
            if (meteorPrefab != null)
            {
                Vector3 targetWorld = GridSystem.Instance.GridToWorld(targetCell);
                Vector3 spawnPos = targetWorld + new Vector3(3f, 10f, 0f);

                GameObject effect = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
                
                // 운석의 크기 Scale을 3배로 증가 (3x3 크기)
                Vector3 originalScale = effect.transform.localScale;
                Vector3 startScale = originalScale * 3f;
                effect.transform.localScale = startScale;
                
                // 낙하 진행
                await effect.transform.DOMove(targetWorld, 0.3f)
                    .SetEase(Ease.InQuad)
                    .AsyncWaitForCompletion();

                // 3. 충돌 효과 & 대미지 판정 & 스케일 확대 (1.5배)
                if (CameraFollow.Instance != null)
                {
                    CameraFollow.Instance.Shake(0.15f, 0.3f);
                }

                // 플레이어가 3x3 영역 내에 있는지 검사
                bool hit = false;
                foreach (var cell in targetCells)
                {
                    if (player != null && !player.IsDead && player.GridPosition == cell)
                    {
                        hit = true;
                        break;
                    }
                }

                if (hit)
                {
                    player.TakeDamage(meteorDamage);
                    Debug.Log($"<color=red>[용암 낙하 타격]</color> 플레이어가 3x3 범위 내에서 맞았습니다!");
                }

                // 바닥 안착 후 크기를 1.5배로 서서히(0.3초) 확대 (최종 4.5배)
                effect.transform.DOScale(startScale * 1.5f, 0.3f).SetEase(Ease.OutQuad);

                // 4. 경고 타일 및 이펙트 소멸 정리
                ClearWarning(targetCells);
                Destroy(effect, 0.5f); // 부풀고 0.5초 뒤 회수
            }
            else
            {
                // 프리팹이 없을 경우 0.3초 대기 후 데미지 처리
                await UniTask.Delay(300, cancellationToken: cancelToken.Token);
                
                bool hit = false;
                foreach (var cell in targetCells)
                {
                    if (player != null && !player.IsDead && player.GridPosition == cell)
                    {
                        hit = true;
                        break;
                    }
                }

                if (hit)
                {
                    player.TakeDamage(meteorDamage);
                }
                ClearWarning(targetCells);
            }

            // 각 용암 드롭 사이의 간격 대기 (0.2초)
            await UniTask.Delay(200, cancellationToken: cancelToken.Token);
        }
    }

    protected override async UniTask OnDeath()
    {
        UnregisterBossGridTarget();
        UnregisterGridOccupancy();
        UnblockGridOccupancy();
        
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

    private void PlayHitEffect()
    {
        // 자식 오브젝트를 포함하여 모든 SpriteRenderer 캐싱 및 최초 색상 저장
        if (_spriteRenderers == null)
        {
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in _spriteRenderers)
            {
                if (sr != null && !_originalColors.ContainsKey(sr))
                {
                    _originalColors[sr] = sr.color;
                }
            }
        }

        // 진행 중이던 기존 트윈 중단
        foreach (var t in _activeTweens)
        {
            if (t != null && t.IsActive()) t.Kill();
        }
        _activeTweens.Clear();

        // 빨간색으로 변경 후 0.1초 동안 원래 색상으로 복구
        foreach (var sr in _spriteRenderers)
        {
            if (sr == null) continue;
            if (_originalColors.TryGetValue(sr, out Color originColor))
            {
                sr.color = Color.red;
                Tween t = sr.DOColor(originColor, 0.1f).SetEase(Ease.OutQuad);
                _activeTweens.Add(t);
            }
        }
    }

    private void BlockGridOccupancy()
    {
        if (GridSystem.Instance != null)
        {
            for (int x = _minX; x <= _maxX; x++)
            {
                GridSystem.Instance.BlockCell(new Vector2Int(x, _gridPos.y - 1));
            }
        }
    }

    private void UnblockGridOccupancy()
    {
        if (GridSystem.Instance != null)
        {
            for (int x = _minX; x <= _maxX; x++)
            {
                GridSystem.Instance.UnblockCell(new Vector2Int(x, _gridPos.y - 1));
            }
        }
    }
}


