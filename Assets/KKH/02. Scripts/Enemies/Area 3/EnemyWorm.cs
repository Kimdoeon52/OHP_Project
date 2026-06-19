using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWorm : EnemyBase
{
    [Header("Underground Move Animation Timing")]
    [SerializeField] private string burrowTriggerName = "Burrow";
    [SerializeField] private string emergeTriggerName = "Emerge";
    [SerializeField] private string travelBoolName = "Walk";
    
    [SerializeField] private float burrowDuration = 0.15f;
    [SerializeField] private float travelDuration = 0.25f;
    [SerializeField] private float emergeDuration = 0.15f;

    private Coroutine _moveCoroutine;
    private bool _isMovingVisual = false;

    // Detailed states for underground movement
    private bool _isBurrowing = false;
    private bool _isUnderground = false;
    private bool _isEmerging = false;

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
        var next = GridPosition + dir;

        if (IsSafeRoomCell(next)) return false; // 세이프룸 침범 방지

        // 지렁이는 벽을 통과(WallPass)할 수 있으므로, GridSystem.Instance.IsWalkable 대신 
        // 맵 영역 내부(IsInBounds)이고 완전히 블록(IsCellBlocked)되지 않았는지만 검사합니다.
        if (!GridSystem.Instance.IsInBounds(next) || GridSystem.Instance.IsCellBlocked(next))
            return false;

        // 플레이어 위치 체크
        if (next == Player.GridPosition)
            return false;

        // 타 엔티티 점유 체크
        if (GridSystem.Instance.IsCellOccupied(next))
            return false;

        // 문이 닫혀 있으면 지나가지 않음
        var door = GridSystem.Instance.GetDoorAt(next);
        if (door != null && !door.IsOpen)
            return false;

        // 이동 시작 시 비주얼 보간을 위해 이전 위치 보관
        Vector3 startWorldPos = transform.position;

        SetGridPosition(next);

        TrapTerrainController.TryApplyTerrainAt(this, next);
        UpdateVisibility();
        UpdateSpriteDirection(next);

        // 비주얼상 이전 위치에서 새 위치로 Lerp하도록 원위치 설정
        transform.position = startWorldPos;

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        Vector3 targetWorldPos = GridSystem.Instance.GridToWorld(next);
        _moveCoroutine = StartCoroutine(SmoothMoveVisual(targetWorldPos));

        return true;
    }

    protected override void UpdateVisibility()
    {
        // 땅속(Underground)에 있는 동안에는 강제로 보이지 않게 처리
        if (_isUnderground)
        {
            if (SpriteRenderer != null) SpriteRenderer.enabled = false;
        }
        else
        {
            base.UpdateVisibility();
        }
    }

    private IEnumerator SmoothMoveVisual(Vector3 targetWorldPos)
    {
        _isMovingVisual = true;

        // 1단계: Burrow (땅속으로 들어감)
        _isBurrowing = true;
        SafeSetTrigger(burrowTriggerName);
        yield return new WaitForSeconds(burrowDuration);
        _isBurrowing = false;

        // 2단계: Travel (땅속 이동)
        _isUnderground = true;
        UpdateVisibility(); // 비주얼 숨김
        SafeSetBool(travelBoolName, true);

        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelDuration;
            transform.position = Vector3.Lerp(startPos, targetWorldPos, t);
            yield return null;
        }

        transform.position = targetWorldPos;
        SafeSetBool(travelBoolName, false);
        _isUnderground = false;

        // 3단계: Emerge (땅 위로 나옴)
        _isEmerging = true;
        UpdateVisibility(); // 비주얼 복원
        SafeSetTrigger(emergeTriggerName);
        yield return new WaitForSeconds(emergeDuration);
        _isEmerging = false;

        _isMovingVisual = false;
    }

    protected override void CheckAttackTelegraph()
    {
        if (IsDead || _isAttacking) return;

        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            _pendingAttackCell = Player.GridPosition;
            UpdateSpriteDirection(_pendingAttackCell.Value);
            StartCoroutine(ExecutePendingAttack(_pendingAttackCell.Value, 1.5f));
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
