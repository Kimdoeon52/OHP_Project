using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWorm : EnemyBase
{
    [Header("Underground Move Animation Timing")]
    [SerializeField] private string burrowTriggerName = "Burrow";
    [SerializeField] private string emergeTriggerName = "Emerge";
    [SerializeField] private string travelBoolName = "Walk";    

    [Header("Animator State Names for Script Control")]
    [Tooltip("Animator 내부의 Burrow 애니메이션 상태(State) 이름입니다.")]
    [SerializeField] private string burrowStateName = "big worm 1 retreat";
    [Tooltip("굴에서 나온 후 돌아갈 기본 상태(State) 이름입니다.")]
    [SerializeField] private string idleStateName = "big worm 1 idle";
    
    [Header("Animation Speeds & Timings (Frame-based)")]
    [Tooltip("애니메이션의 초당 프레임 수(FPS)입니다. 보통 12, 24 등이 쓰입니다.")]
    [SerializeField] private float animationFPS = 12f;

    [Tooltip("굴로 들어갈 때의 애니메이션 재생 속도 배율입니다.")]
    [SerializeField] private float burrowSpeedMultiplier = 1.8f;
    [Tooltip("굴에서 나올 때의 애니메이션 재생 속도 배율입니다.")]
    [SerializeField] private float emergeSpeedMultiplier = 1.5f;

    [Tooltip("굴 속으로 완전히 들어가서 정지할 프레임 번호입니다.")]
    [SerializeField] private int burrowFreezeFrame = 12;

    [Tooltip("땅속을 통해 이동하는 데 걸리는 시간(초)입니다.")]
    [SerializeField] private float travelDuration = 1.0f;

    [Tooltip("굴에서 나와서 기본 상태로 복구될 때까지 재생할 프레임 수입니다. (남은 연출 프레임 수)")]
    [SerializeField] private int emergeFrames = 8;

    [Tooltip("공격 예고 장판이 생성된 후 실제 타격이 가해지기까지의 대기 시간(초)입니다.")]
    [SerializeField] private float attackTelegraphDelay = 0.6f;

    private Coroutine _moveCoroutine;
    private bool _isMovingVisual = false;

    // Detailed states for underground movement
    private bool _isBurrowing = false;
    private bool _isUnderground = false;
    private bool _isEmerging = false;

    protected override void Update()
    {
        // 이동 연출 중이거나 공격 진행 중(경고 장판 대기 및 실제 공격 연출 중)일 때는 
        // 턴 행동 로직 업데이트(AP 누적 및 판단)를 완전히 일시 정지합니다.
        // 이를 통해 "이동 -> 솟아나옴 -> 공격 예고 장판 -> 공격 완료 -> 다시 굴파고 다음 이동" 순서가
        // 서로 겹치거나 뒤섞이지 않고 완벽하게 상호 배제되며 단방향으로 흘러가도록 보장합니다.
        if (_isMovingVisual || _isAttacking || _pendingAttackCell != null) return;

        base.Update();
    }

    public override void TakeDamage(int amount, DamageType damageType)
    {
        if (IsDead || amount <= 0) return;

        // 땅속 이동 프로세스 중(Burrow, Underground, Emerge)이거나 Walk 중일 때 원거리 면역
        bool isImmuneState = _isMovingVisual || _isBurrowing || _isUnderground || _isEmerging || 
                             (_animator != null && HasParameter(travelBoolName) && _animator.GetBool(travelBoolName));

        if (isImmuneState && damageType == DamageType.Ranged)
        {
            // 이펙트 혹은 텍스트 요청 ("Immune" 등)
            GameEvents.OnCombatTextRequested?.Invoke(
                new CombatTextRequest(transform.position, "Immune", 0, CombatTextType.Miss)
            );
            Debug.Log($"{name}이 이동 중 원거리 공격을 면역했습니다.");
            return;
        }

        base.TakeDamage(amount);
    }

    public override void TakeDamage(int amount)
    {
        TakeDamage(amount, DamageContext.CurrentDamageType);
    }

    protected override bool TryStep(Vector2Int dir)
    {
        // 넉백 등 예외적인 1칸 이동 시에도 이동 플래그 방어 적용
        if (_isMovingVisual) return false;

        var next = GridPosition + dir;

        if (IsSafeRoomCell(next)) return false;

        if (!GridSystem.Instance.IsInBounds(next) || GridSystem.Instance.IsCellBlocked(next))
            return false;

        if (next == Player.GridPosition)
            return false;

        if (GridSystem.Instance.IsCellOccupied(next))
            return false;

        var door = GridSystem.Instance.GetDoorAt(next);
        if (door != null && !door.IsOpen)
            return false;

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        Vector3 startWorldPos = transform.position;
        _isMovingVisual = true;

        SetGridPosition(next);
        TrapTerrainController.TryApplyTerrainAt(this, next);
        UpdateVisibility();
        UpdateSpriteDirection(next);

        transform.position = startWorldPos;

        Vector3 targetWorldPos = GridSystem.Instance.GridToWorld(next);
        _moveCoroutine = StartCoroutine(SmoothMoveVisualMultiple(new List<Vector2Int> { next }));

        return true;
    }

    protected override void PerformMovement()
    {
        if (IsDead || _isMovingVisual || _isAttacking || _pendingAttackCell != null) return;

        // 플레이어 위치
        Vector2Int playerPos = Player.GridPosition;
        Vector2Int targetCell = GridPosition;
        bool foundTarget = false;

        // 1. 플레이어가 시야 범위 안에 있다면 습격할 인접 위치 탐색
        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            // 플레이어 주변 상하좌우 4칸 중 비어있고 침입 가능한 타겟 셀 수집
            Vector2Int[] adjacentDirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            List<Vector2Int> validEmergeCells = new List<Vector2Int>();

            foreach (var dir in adjacentDirs)
            {
                var cell = playerPos + dir;
                if (GridSystem.Instance.IsInBounds(cell) && !IsSafeRoomCell(cell))
                {
                    // 플레이어 셀이 아니며, 타 엔티티가 점유하지 않은 곳
                    if (cell != playerPos && !GridSystem.Instance.IsCellOccupied(cell))
                    {
                        var door = GridSystem.Instance.GetDoorAt(cell);
                        if (door == null || door.IsOpen)
                        {
                            validEmergeCells.Add(cell);
                        }
                    }
                }
            }

            if (validEmergeCells.Count > 0)
            {
                // 플레이어 인근 랜덤한 셀을 최종 기습 Emerge 지점으로 선정
                targetCell = validEmergeCells[Random.Range(0, validEmergeCells.Count)];
                foundTarget = true;
            }
        }

        // 2. 플레이어가 탐지되지 않았거나 주변에 기습할 수 있는 빈 자리가 없으면 배회(Wander)
        if (!foundTarget)
        {
            // 주변 2칸 반경 내의 임의의 점유되지 않은 셀 수집
            List<Vector2Int> wanderCells = new List<Vector2Int>();
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    if (x == 0 && y == 0) continue;
                    var cell = GridPosition + new Vector2Int(x, y);
                    if (GridSystem.Instance.IsInBounds(cell) && !IsSafeRoomCell(cell))
                    {
                        if (!GridSystem.Instance.IsCellOccupied(cell))
                        {
                            var door = GridSystem.Instance.GetDoorAt(cell);
                            if (door == null || door.IsOpen)
                            {
                                wanderCells.Add(cell);
                            }
                        }
                    }
                }
            }

            if (wanderCells.Count > 0)
            {
                targetCell = wanderCells[Random.Range(0, wanderCells.Count)];
                foundTarget = true;
            }
        }

        if (!foundTarget) return;

        // 3. 목적지까지 벽을 통과(WallPass)할 수 있는 최단 경로 계산
        List<Vector2Int> path = FindPathTo(targetCell);
        if (path == null || path.Count == 0) return;

        // 게임 흐름을 깨지 않도록 턴당 최대 이동 거리를 3칸으로 제한
        if (path.Count > 3)
        {
            path = path.GetRange(0, 3);
        }

        // 4. 연속 이동 비주얼 연출 시작
        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        Vector3 startWorldPos = transform.position;
        _isMovingVisual = true;

        // 최종 목적 격자 좌표 설정
        Vector2Int finalGridPos = path[path.Count - 1];
        SetGridPosition(finalGridPos);

        TrapTerrainController.TryApplyTerrainAt(this, finalGridPos);
        UpdateVisibility(); // _isMovingVisual = true 상태이므로 스프라이트 활성화 상태 강제 유지
        UpdateSpriteDirection(finalGridPos);

        transform.position = startWorldPos;

        _moveCoroutine = StartCoroutine(SmoothMoveVisualMultiple(path));
    }

    // BFS를 이용하여 벽을 통과(WallPass)하는 지렁이 전용 최단 경로 찾기
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

                    // 다른 적이나 플레이어가 점유 중이면 우회 (최종 목적지는 예외)
                    if (next != target && GridSystem.Instance.IsCellOccupied(next))
                        isWalkableForWorm = false;

                    // 문이 닫혀 있으면 지나가지 못함
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
            if (SpriteRenderer != null) SpriteRenderer.enabled = true;
            return;
        }

        bool isInWall = GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(GridPosition);

        if (isInWall)
        {
            if (SpriteRenderer != null) SpriteRenderer.enabled = false;
        }
        else
        {
            base.UpdateVisibility();
        }
    }

    private IEnumerator SmoothMoveVisualMultiple(List<Vector2Int> path)
    {
        _isMovingVisual = true;

        // 프레임 번호와 FPS를 기준으로 실제 대기할 시간(초) 계산
        float burrowFreezeTime = (animationFPS > 0) ? (burrowFreezeFrame / animationFPS) : 0f;
        float emergeDuration = (animationFPS > 0) ? (emergeFrames / animationFPS) : 0f;

        // 1단계: Burrow (땅속으로 들어감)
        _isBurrowing = true;
        if (_animator != null)
        {
            _animator.speed = burrowSpeedMultiplier;
            // Walk 파라미터를 true로 설정 -> Animator가 트랜지션을 타고 big worm 1 retreat 상태 재생 시작
            SafeSetBool(travelBoolName, true);

            // 즉시 굴 파기 상태의 시작 부분(0프레임)으로 강제 진입시켜 
            // 애니메이터 전이 지연(Transition Duration) 때문에 지렁이가 땅 밖으로 솟아난 채 움직이는 현상을 방지합니다.
            _animator.Play(burrowStateName, 0, 0f);
            _animator.Update(0f);
        }
        // 굴 속으로 완전히 들어갈 때(burrowFreezeTime)까지 대기
        yield return new WaitForSeconds(burrowFreezeTime);
        _isBurrowing = false;

        // 2단계: Travel (땅속 연속 이동)
        _isUnderground = true;
        UpdateVisibility(); // 비주얼 보장

        if (_animator != null)
        {
            // Walk = true를 유지한 채, 재생 속도만 0으로 만들어 정지시킵니다.
            _animator.speed = 0f;
        }

        // 경로를 따라 차례대로 Lerp 이동
        foreach (var cell in path)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = GridSystem.Instance.GridToWorld(cell);
            float elapsed = 0f;

            // 이동 시 격자 방향으로 스프라이트 방향 업데이트
            UpdateSpriteDirection(cell);

            while (elapsed < travelDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / travelDuration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            transform.position = targetPos;
        }

        _isUnderground = false;

        // 3단계: Emerge (땅 위로 나옴 - 정지했던 지점부터 이어서 정방향 재생)
        _isEmerging = true;

        if (_animator != null)
        {
            // Walk = true 상태를 유지한 채, 속도를 다시 켜서 남은 애니메이션이 이어서 재생되게 합니다.
            _animator.speed = emergeSpeedMultiplier;
        }

        UpdateVisibility(); // 비주얼 보장
        yield return new WaitForSeconds(emergeDuration);

        if (_animator != null)
        {
            // 4단계: 완료 및 Idle 복귀
            // Walk 파라미터를 false로 설정 -> Animator가 트랜지션을 타고 big worm 1 idle로 복귀
            SafeSetBool(travelBoolName, false);
            _animator.speed = 1f;
        }

        _isEmerging = false;
        _isMovingVisual = false;

        // 이동 완료 후 최종 가시성 업데이트
        UpdateVisibility();
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

    // --- 안전한 애니메이터 파라미터 세팅 헬퍼 ---
    private void SafeSetTrigger(string name)
    {
        if (_animator != null && !string.IsNullOrEmpty(name) && HasParameter(name))
        {
            _animator.SetTrigger(name);
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
