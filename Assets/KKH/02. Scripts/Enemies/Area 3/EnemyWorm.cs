using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Area 3 지렁이 몬스터 (EnemyWorm)
/// 
/// 핵심 기믹:
/// 1. 벽을 통과하여 땅속으로 이동 (WallPass BFS 최단경로, 턴당 최대 3칸)
/// 2. 프레임 연출:
///    - 땅 파고들기(Burrow): 13번 ~ 30번 프레임 (18프레임)
///    - 땅속 이동(Underground): 30번 프레임 고정 상태로 이동 (데미지 면역)
///    - 솟구쳐 나오기(Emerge): 31번 ~ 44번 프레임 (14프레임)
/// 3. 피격 판정:
///    - 파고드는 중(13~30) & 나오는 중(31~44): 피격 가능
///    - 땅속 이동 중 & 벽 속 대기 중: 데미지 면역 (Immune)
/// </summary>
public class EnemyWorm : EnemyBase
{
    [Header("Burrow Animation Settings")]
    [SerializeField] private string burrowStateName = "big worm 1 retreat";
    [SerializeField] private string idleStateName = "big worm 1 idle";
    [SerializeField] private string travelBoolName = "Walk";

    [Header("Animation Frame Ranges (Retreat / Emerge)")]
    [Tooltip("애니메이션 초당 프레임 수 (FPS)")]
    [SerializeField] private float animationFPS = 12f;

    [Tooltip("Retreat 애니메이션 전체 프레임 수 (기본 44)")]
    [SerializeField] private int totalClipFrames = 44;

    [Tooltip("땅굴 파고들기(Burrow Dive) 시작 프레임 번호 (기본 13)")]
    [SerializeField] private int burrowStartFrame = 13;

    [Tooltip("땅굴 파고들기(Burrow Dive) 완료 프레임 번호 (기본 30 - 18프레임 분량)")]
    [SerializeField] private int burrowEndFrame = 30;

    [Tooltip("땅굴에서 나오는(Emerge) 시작 프레임 번호 (기본 31)")]
    [SerializeField] private int emergeStartFrame = 31;

    [Tooltip("땅굴에서 나오는(Emerge) 완료 프레임 번호 (기본 44 - 14프레임 분량)")]
    [SerializeField] private int emergeEndFrame = 44;

    [Header("Speeds & Timings")]
    [Tooltip("굴로 들어갈 때 애니메이션 속도 배율")]
    [SerializeField] private float burrowSpeedMultiplier = 1.0f;

    [Tooltip("굴에서 나올 때 애니메이션 속도 배율")]
    [SerializeField] private float emergeSpeedMultiplier = 1.7f;

    [Tooltip("땅속 이동 시 1칸 당 이동 시간(초)")]
    [SerializeField] private float travelDurationPerCell = 0.3f;

    [Tooltip("공격 예고 후 실제 타격까지의 대기 시간(초)")]
    [SerializeField] private float attackTelegraphDelay = 0.6f;

    // 이동 코루틴 및 상태
    private Coroutine _moveCoroutine;
    private bool _isMovingVisual = false;
    private bool _isUnderground = false; // 땅속 완전 수중 이동 중인가? (이때만 데미지 면역)

    protected override void Update()
    {
        // 벽 안에 정지해 있는 경우 애니메이션 정지 및 굴속 상태 유지
        if (!_isMovingVisual)
        {
            bool isInWall = GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(GridPosition);
            if (isInWall && _animator != null)
            {
                _animator.speed = 0f;
                SafeSetBool(travelBoolName, true);
            }
        }

        // 이동 중이거나 공격 중이면 턴 행동 업데이트 정지
        if (_isMovingVisual || _isAttacking || _pendingAttackCell != null) return;

        base.Update();
    }

    // ── 데미지 판정 ──
    public override void TakeDamage(int amount, DamageType damageType)
    {
        if (IsDead || amount <= 0) return;

        // 오직 땅속 완전 이동 중(_isUnderground)이거나 벽 속 대기 중일 때만 데미지 면역
        bool isInWall = GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(GridPosition);
        if (_isUnderground || (isInWall && !_isMovingVisual))
        {
            GameEvents.OnCombatTextRequested?.Invoke(
                new CombatTextRequest(transform.position, "Immune", 0, CombatTextType.Miss)
            );
            return;
        }

        // 땅굴 이동/다이빙/솟구치기 연출 진행 중 피격 시:
        // 데미지, 타격 사운드, 피격 VFX 및 사망 처리는 정상 진행하되 애니메이터의 Hit 트리거 연출은 억제
        if (_isMovingVisual)
        {
            int finalDamage = ApplyDefense(amount);
            CurrentHp -= finalDamage;

            if (finalDamage > 0)
            {
                GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.MonsterHit);
                GameEvents.OnPlayVFX?.Invoke(transform.position, VFXType.MonsterHit);
            }

            if (CurrentHp <= 0) Die();
        }
        else
        {
            base.TakeDamage(amount);
        }
    }

    public override void TakeDamage(int amount)
    {
        TakeDamage(amount, DamageContext.CurrentDamageType);
    }

    protected override void Die()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        _isMovingVisual = false;
        _isUnderground = false;

        if (_animator != null) _animator.speed = 1f;
        if (SpriteRenderer != null) SpriteRenderer.enabled = true;

        base.Die();
    }

    // ── 이동 처리 ──
    protected override bool TryStep(Vector2Int dir)
    {
        if (_isMovingVisual) return false;

        var next = GridPosition + dir;
        if (!CanOccupyCell(next)) return false;

        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(BurrowMoveSequence(new List<Vector2Int> { next }));
        return true;
    }

    protected override void PerformMovement()
    {
        if (IsDead || _isMovingVisual || _isAttacking || _pendingAttackCell != null) return;

        // 타겟 셀 탐색 (플레이어 인근 혹은 배회)
        Vector2Int? targetCell = FindMoveTarget();
        if (!targetCell.HasValue) return;

        // 최단 경로 계산 (벽 통과 가능)
        List<Vector2Int> path = FindPathTo(targetCell.Value);
        if (path == null || path.Count == 0) return;

        if (path.Count > 3)
            path = path.GetRange(0, 3);

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(BurrowMoveSequence(path));
    }

    // ── 땅굴 이동 시퀀스 (Burrow ➔ Underground Travel ➔ Emerge) ──
    private IEnumerator BurrowMoveSequence(List<Vector2Int> path)
    {
        _isMovingVisual = true;

        bool startInWall = GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(GridPosition);

        float diveDuration = GetBurrowDiveTime();
        float emergeDuration = GetEmergeTime();

        // 1. Burrow (13번~30번 프레임: 땅 파고들기 다이빙)
        if (!startInWall)
        {
            _isUnderground = false; // 파고드는 동안 지상에 몸이 보이므로 피격 가능!

            if (_animator != null)
            {
                _animator.speed = burrowSpeedMultiplier;
                SafeSetBool(travelBoolName, true);

                // 13번 프레임 시작 위치로 재생 (normalizedTime 계산)
                float startNormalizedTime = (totalClipFrames > 0) ? (float)(burrowStartFrame - 1) / totalClipFrames : 0f;
                _animator.Play(burrowStateName, 0, startNormalizedTime);
                _animator.Update(0f);
            }

            yield return new WaitForSeconds(diveDuration);
        }

        // 2. Underground Travel (30번 프레임 고정 상태로 땅속 이동) - 데미지 면역
        _isUnderground = true;
        UpdateVisibility();

        if (_animator != null)
        {
            _animator.speed = 0f; // 30번 프레임(굴속 완전 매몰) 지점에서 프레임 정지
        }

        foreach (var cell in path)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = GridSystem.Instance.GridToWorld(cell);
            float elapsed = 0f;

            UpdateSpriteDirection(cell);
            bool updatedGrid = false;

            while (elapsed < travelDurationPerCell)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / travelDurationPerCell;
                transform.position = Vector3.Lerp(startPos, targetPos, t);

                // 50% 지점에서 격자 위치(GridPosition) 갱신하여 타격 판정 동기화
                if (!updatedGrid && t >= 0.5f)
                {
                    SetGridPosition(cell);
                    updatedGrid = true;
                }

                yield return null;
            }

            transform.position = targetPos;
            if (!updatedGrid) SetGridPosition(cell);
            TrapTerrainController.TryApplyTerrainAt(this, cell);
        }

        bool endInWall = GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(path[path.Count - 1]);

        // 3. Emerge (31번~44번 프레임: 솟구쳐 나와서 복구) - 솟구치는 시점부터 피격 가능
        if (!endInWall)
        {
            _isUnderground = false;

            if (_animator != null)
            {
                _animator.speed = emergeSpeedMultiplier;

                // 31번 프레임 시작 위치부터 이어서 재생
                float emergeStartNormalizedTime = (totalClipFrames > 0) ? (float)(emergeStartFrame - 1) / totalClipFrames : 0.68f;
                _animator.Play(burrowStateName, 0, emergeStartNormalizedTime);
                _animator.Update(0f);
            }

            UpdateVisibility();
            yield return new WaitForSeconds(emergeDuration);

            if (_animator != null)
            {
                SafeSetBool(travelBoolName, false);
                _animator.speed = 1f;
            }
        }
        else
        {
            if (_animator != null)
            {
                _animator.speed = 0f;
            }
        }

        _isMovingVisual = false;
        UpdateVisibility();
    }

    private float GetBurrowDiveTime()
    {
        float frames = Mathf.Max(1, burrowEndFrame - burrowStartFrame + 1); // 30 - 13 + 1 = 18프레임
        return (frames / Mathf.Max(1f, animationFPS)) / Mathf.Max(0.1f, burrowSpeedMultiplier);
    }

    private float GetEmergeTime()
    {
        float frames = Mathf.Max(1, emergeEndFrame - emergeStartFrame + 1); // 44 - 31 + 1 = 14프레임
        return (frames / Mathf.Max(1f, animationFPS)) / Mathf.Max(0.1f, emergeSpeedMultiplier);
    }

    // ── 헬퍼 메서드 ──
    private Vector2Int? FindMoveTarget()
    {
        Vector2Int playerPos = Player.GridPosition;

        // 시야 내 플레이어 존재 시 습격 위치
        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            Vector2Int[] adjacentDirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            List<Vector2Int> validCells = new List<Vector2Int>();

            foreach (var dir in adjacentDirs)
            {
                var cell = playerPos + dir;
                if (CanOccupyCell(cell) && cell != GridPosition)
                {
                    validCells.Add(cell);
                }
            }

            if (validCells.Count > 0)
                return validCells[Random.Range(0, validCells.Count)];
        }

        // 주변 2칸 배회 (Wander)
        List<Vector2Int> wanderCells = new List<Vector2Int>();
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                if (x == 0 && y == 0) continue;
                var cell = GridPosition + new Vector2Int(x, y);
                if (cell != GridPosition && CanOccupyCell(cell))
                {
                    wanderCells.Add(cell);
                }
            }
        }

        if (wanderCells.Count > 0)
            return wanderCells[Random.Range(0, wanderCells.Count)];

        return null;
    }

    private bool CanOccupyCell(Vector2Int cell)
    {
        if (IsSafeRoomCell(cell)) return false;
        if (!GridSystem.Instance.IsInBounds(cell)) return false;
        if (cell == Player.GridPosition) return false;
        if (GridSystem.Instance.IsCellOccupied(cell)) return false;

        var door = GridSystem.Instance.GetDoorAt(cell);
        if (door != null && !door.IsOpen) return false;

        return true;
    }

    private List<Vector2Int> FindPathTo(Vector2Int target)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(GridPosition);
        visited.Add(GridPosition);

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        bool found = false;

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            if (curr == target)
            {
                found = true;
                break;
            }

            foreach (var dir in dirs)
            {
                var next = curr + dir;
                if (GridSystem.Instance.IsInBounds(next) && !visited.Contains(next))
                {
                    bool isWalkableForWorm = true;
                    if (next != target && GridSystem.Instance.IsCellOccupied(next))
                        isWalkableForWorm = false;

                    var door = GridSystem.Instance.GetDoorAt(next);
                    if (door != null && !door.IsOpen)
                        isWalkableForWorm = false;

                    if (isWalkableForWorm)
                    {
                        visited.Add(next);
                        parent[next] = curr;
                        queue.Enqueue(next);
                    }
                }
            }
        }

        if (!found) return null;

        List<Vector2Int> path = new List<Vector2Int>();
        var temp = target;
        while (temp != GridPosition)
        {
            path.Add(temp);
            temp = parent[temp];
        }
        path.Reverse();
        return path;
    }

    protected override void UpdateVisibility()
    {
        if (_isMovingVisual)
        {
            bool isInWall = GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(GridPosition);
            if (_isUnderground && isInWall)
            {
                if (SpriteRenderer != null) SpriteRenderer.enabled = false;
            }
            else
            {
                if (SpriteRenderer != null) SpriteRenderer.enabled = true;
            }
            return;
        }

        bool isInWallStationary = GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(GridPosition);
        if (isInWallStationary)
        {
            if (SpriteRenderer != null) SpriteRenderer.enabled = false;
        }
        else
        {
            base.UpdateVisibility();
        }
    }

    protected override void CheckAttackTelegraph()
    {
        if (IsDead || _isAttacking || _isMovingVisual) return;

        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            _pendingAttackCell = Player.GridPosition;
            UpdateSpriteDirection(_pendingAttackCell.Value);
            StartCoroutine(ExecutePendingAttack(_pendingAttackCell.Value, attackTelegraphDelay));
        }
    }

    private void SafeSetBool(string name, bool value)
    {
        if (_animator != null && !string.IsNullOrEmpty(name) && HasParameter(name))
        {
            _animator.SetBool(name, value);
        }
    }

    private bool HasParameter(string paramName)
    {
        if (_animator == null) return false;
        foreach (var param in _animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}
