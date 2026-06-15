using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWorm : EnemyBase
{
    private Coroutine _moveCoroutine;
    private bool _isMovingVisual = false;

    public override void TakeDamage(int amount, DamageType damageType)
    {
        if (IsDead || amount <= 0) return;

        // 이동 중(시각적 보간 중이거나 Walk 중)에 원거리(투척/마법) 공격을 받으면 완전 무효화
        if ((_isMovingVisual || (_animator != null && _animator.GetBool("Walk"))) && damageType == DamageType.Ranged)
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
        _moveCoroutine = StartCoroutine(SmoothMoveVisual(targetWorldPos, 0.2f));

        return true;
    }

    private IEnumerator SmoothMoveVisual(Vector3 targetWorldPos, float duration)
    {
        _isMovingVisual = true;
        if (_animator != null) _animator.SetBool("Walk", true);

        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, targetWorldPos, t);
            yield return null;
        }

        transform.position = targetWorldPos;

        if (_animator != null) _animator.SetBool("Walk", false);
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
}

