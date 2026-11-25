using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 지렁이 (EnemyWorm) — 벽 통과 땅굴 이동 및 이동 중 원거리 공격 면역
public class EnemyWorm : EnemyBase
{
    [Header("Burrow Animation Settings")]
    [SerializeField] private string burrowStateName = "big worm 1 retreat";
    [SerializeField] private string travelBoolName = "Walk";

    [Header("Animation Frame Ranges (Retreat / Emerge)")]
    [Tooltip("초당 프레임 수")]
    [SerializeField] private float animationFPS = 12f;

    [Tooltip("Retreat 총 프레임 수")]
    [SerializeField] private int totalClipFrames = 44;

    [Tooltip("다이빙 시작 프레임")]
    [SerializeField] private int burrowStartFrame = 13;

    [Tooltip("다이빙 완료 프레임")]
    [SerializeField] private int burrowEndFrame = 30;

    [Tooltip("솟구치기 시작 프레임")]
    [SerializeField] private int emergeStartFrame = 31;

    [Tooltip("솟구치기 완료 프레임")]
    [SerializeField] private int emergeEndFrame = 44;

    [Header("Speeds & Timings")]
    [Tooltip("입수 애니메이션 속도 배율")]
    [SerializeField] private float burrowSpeedMultiplier = 1.0f;

    [Tooltip("출수 애니메이션 속도 배율")]
    [SerializeField] private float emergeSpeedMultiplier = 1.7f;

    [Tooltip("땅속 칸당 이동 시간")]
    [SerializeField] private float travelDurationPerCell = 0.3f;

    [Tooltip("공격 예고 대기 시간")]
    [SerializeField] private float attackTelegraphDelay = 0.6f;

    private Coroutine _moveCoroutine;
    private bool _isMovingVisual = false;
    private bool _isUnderground = false;

    protected override void Update()
    {
        if (!_isMovingVisual)
        {
            bool isInWall = GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(GridPosition);
            if (isInWall && _animator != null)
            {
                _animator.speed = 0f;
                SafeSetBool(travelBoolName, true);
            }
        }

        if (_isMovingVisual || _isAttacking || _pendingAttackCell != null)
            return;

        base.Update();
    }

    public override void TakeDamage(int amount, DamageType damageType)
    {
        if (IsDead || amount <= 0)
            return;

        isDetected = true;

        if ((_isMovingVisual || _isUnderground) && damageType == DamageType.Ranged)
        {
            GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(transform.position, "Immune", 0, CombatTextType.Miss));
            return;
        }

        bool isInWall = GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(GridPosition);
        if (_isUnderground || (isInWall && !_isMovingVisual))
        {
            GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(transform.position, "Immune", 0, CombatTextType.Miss));
            return;
        }

        if (_isMovingVisual)
        {
            int finalDamage = ApplyDefense(amount);
            CurrentHp -= finalDamage;

            if (finalDamage > 0)
            {
                GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.MonsterHit);
                GameEvents.OnPlayVFX?.Invoke(transform.position, VFXType.MonsterHit);
            }

            if (CurrentHp <= 0)
                Die();
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

        if (_animator != null)
            _animator.speed = 1f;

        if (SpriteRenderer != null)
            SpriteRenderer.enabled = true;

        base.Die();
    }

    protected override bool TryStep(Vector2Int dir)
    {
        if (_isMovingVisual)
            return false;

        var next = GridPosition + dir;
        if (!CanOccupyCell(next))
            return false;

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(BurrowMoveSequence(new List<Vector2Int> { next }));
        return true;
    }

    protected override void PerformMovement()
    {
        if (IsDead || _isMovingVisual || _isAttacking || _pendingAttackCell != null)
            return;

        Vector2Int? targetCell = FindMoveTarget();
        if (!targetCell.HasValue)
            return;

        List<Vector2Int> path = FindPathTo(targetCell.Value);
        if (path == null || path.Count == 0)
            return;

        if (path.Count > 3)
            path = path.GetRange(0, 3);

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(BurrowMoveSequence(path));
    }

    private IEnumerator BurrowMoveSequence(List<Vector2Int> path)
    {
        _isMovingVisual = true;

        bool startInWall = GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(GridPosition);
        float diveDuration = GetBurrowDiveTime();
        float emergeDuration = GetEmergeTime();

        if (!startInWall)
        {
            _isUnderground = false;

            if (_animator != null)
            {
                _animator.speed = burrowSpeedMultiplier;
                SafeSetBool(travelBoolName, true);

                float startNormalizedTime = (totalClipFrames > 0) ? (float)(burrowStartFrame - 1) / totalClipFrames : 0f;
                _animator.Play(burrowStateName, 0, startNormalizedTime);
                _animator.Update(0f);
            }

            yield return WaitGameSeconds(diveDuration);
        }

        _isUnderground = true;
        UpdateVisibility();

        if (_animator != null)
            _animator.speed = 0f;

        Vector2Int lastCell = GridPosition;
        foreach (var cell in path)
        {
            if (cell == Player.GridPosition || (GridSystem.Instance != null && GridSystem.Instance.IsCellOccupied(cell)))
                break;

            Vector3 startPos = transform.position;
            Vector3 targetPos = GridSystem.Instance.GridToWorld(cell);
            float elapsed = 0f;

            UpdateSpriteDirection(cell);
            bool updatedGrid = false;

            while (elapsed < travelDurationPerCell)
            {
                if (TimeSystem.Instance == null || TimeSystem.Instance.IsRunning)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / travelDurationPerCell;
                    transform.position = Vector3.Lerp(startPos, targetPos, t);

                    if (!updatedGrid && t >= 0.5f)
                    {
                        if (cell == Player.GridPosition || GridSystem.Instance.IsCellOccupied(cell))
                        {
                            transform.position = startPos;
                            break;
                        }

                        SetGridPosition(cell);
                        lastCell = cell;
                        updatedGrid = true;
                    }
                }

                yield return null;
            }

            if (!updatedGrid)
                break;

            transform.position = targetPos;
            TrapTerrainController.TryApplyTerrainAt(this, cell);
        }

        bool endInWall = GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(lastCell);

        if (!endInWall)
        {
            _isUnderground = false;

            if (_animator != null)
            {
                _animator.speed = emergeSpeedMultiplier;

                float emergeStartNormalizedTime = (totalClipFrames > 0) ? (float)(emergeStartFrame - 1) / totalClipFrames : 0.68f;
                _animator.Play(burrowStateName, 0, emergeStartNormalizedTime);
                _animator.Update(0f);
            }

            UpdateVisibility();
            yield return WaitGameSeconds(emergeDuration);

            if (_animator != null)
            {
                SafeSetBool(travelBoolName, false);
                _animator.speed = 1f;
            }
        }
        else if (_animator != null)
        {
            _animator.speed = 0f;
        }

        _isMovingVisual = false;
        UpdateVisibility();
    }

    private float GetBurrowDiveTime()
    {
        float frames = Mathf.Max(1, burrowEndFrame - burrowStartFrame + 1);
        return (frames / Mathf.Max(1f, animationFPS)) / Mathf.Max(0.1f, burrowSpeedMultiplier);
    }

    private float GetEmergeTime()
    {
        float frames = Mathf.Max(1, emergeEndFrame - emergeStartFrame + 1);
        return (frames / Mathf.Max(1f, animationFPS)) / Mathf.Max(0.1f, emergeSpeedMultiplier);
    }

    private Vector2Int? FindMoveTarget()
    {
        Vector2Int playerPos = Player.GridPosition;
        DungeonRandom rng = GetRng();

        if (data != null && SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            isDetected = true;
            Vector2Int[] adjacentDirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            List<Vector2Int> validCells = new List<Vector2Int>();

            foreach (var dir in adjacentDirs)
            {
                var cell = playerPos + dir;
                if (CanOccupyCell(cell) && cell != GridPosition)
                    validCells.Add(cell);
            }

            if (validCells.Count > 0)
                return validCells[rng.Range(0, validCells.Count)];
        }

        List<Vector2Int> wanderCells = new List<Vector2Int>();
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                var cell = GridPosition + new Vector2Int(x, y);
                if (cell != GridPosition && CanOccupyCell(cell))
                    wanderCells.Add(cell);
            }
        }

        if (wanderCells.Count > 0)
            return wanderCells[rng.Range(0, wanderCells.Count)];

        return null;
    }

    private bool CanOccupyCell(Vector2Int cell)
    {
        if (IsSafeRoomCell(cell))
            return false;

        if (IsInsideClosedRestrictedRoom(cell))
            return false;

        if (!GridSystem.Instance.IsInBounds(cell))
            return false;

        if (cell == Player.GridPosition)
            return false;

        if (GridSystem.Instance.IsCellOccupied(cell))
            return false;

        var door = GridSystem.Instance.GetDoorAt(cell);
        if (door != null && !door.IsOpen)
            return false;

        return true;
    }

    // 잠긴 Special/Secret 방 내부 셀인지 확인하여 벽 관통 무단 침투 차단
    private bool IsInsideClosedRestrictedRoom(Vector2Int cell)
    {
        if (MapGenerator.Instance == null || MapGenerator.Instance.CurrentRooms == null)
            return false;

        for (int i = 0; i < MapGenerator.Instance.CurrentRooms.Count; i++)
        {
            var room = MapGenerator.Instance.CurrentRooms[i];
            if (room.type != RoomType.Special && room.type != RoomType.Secret)
                continue;

            if (!room.floorCells.Contains(cell))
                continue;

            if (room.doorCells == null)
                continue;

            foreach (var doorPos in room.doorCells)
            {
                var door = GridSystem.Instance != null ? GridSystem.Instance.GetDoorAt(doorPos) : null;
                if (door != null && !door.IsOpen)
                    return true;
            }
        }

        return false;
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
                if (!GridSystem.Instance.IsInBounds(next) || visited.Contains(next))
                    continue;

                if (IsSafeRoomCell(next) || IsInsideClosedRestrictedRoom(next))
                    continue;

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

        if (!found)
            return null;

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
                if (SpriteRenderer != null)
                    SpriteRenderer.enabled = false;
            }
            else if (SpriteRenderer != null)
            {
                SpriteRenderer.enabled = true;
            }
            return;
        }

        bool isInWallStationary = GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(GridPosition);
        if (isInWallStationary)
        {
            if (SpriteRenderer != null)
                SpriteRenderer.enabled = false;
        }
        else
        {
            base.UpdateVisibility();
        }
    }

    protected override void CheckAttackTelegraph()
    {
        if (IsDead || _isAttacking || _isMovingVisual)
            return;

        if (!SafeRoomTimerGate.PlayerInSafeRoom && IsAdjacentToPlayer())
        {
            isDetected = true;
            _pendingAttackCell = Player.GridPosition;
            UpdateSpriteDirection(_pendingAttackCell.Value);
            StartCoroutine(ExecutePendingAttack(_pendingAttackCell.Value, attackTelegraphDelay));
        }
    }

    private void SafeSetBool(string name, bool value)
    {
        if (_animator != null && !string.IsNullOrEmpty(name) && HasParameter(name))
            _animator.SetBool(name, value);
    }

    private bool HasParameter(string paramName)
    {
        if (_animator == null)
            return false;

        foreach (var param in _animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }

        return false;
    }
}
