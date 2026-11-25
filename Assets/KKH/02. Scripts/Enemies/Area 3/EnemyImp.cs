using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 작은 악마 (EnemyImp) — 3x3 화염 장판 예고 및 1초간 화염 지형 생성
public class EnemyImp : EnemyBase
{
    private readonly List<Vector2Int> _targetArea = new List<Vector2Int>();

    protected override void CheckAttackTelegraph()
    {
        if (IsDead || _isAttacking)
            return;

        if (data != null && SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            if (VisionSystem.Instance != null && !VisionSystem.Instance.HasLineOfSight(GridPosition, Player.GridPosition))
                return;

            isDetected = true;
            _pendingAttackCell = Player.GridPosition;
            UpdateSpriteDirection(_pendingAttackCell.Value);

            _targetArea.Clear();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int cell = _pendingAttackCell.Value + new Vector2Int(dx, dy);
                    if (GridSystem.Instance != null && (!GridSystem.Instance.IsInBounds(cell) || !GridSystem.Instance.IsWalkable(cell)))
                        continue;

                    if (IsSafeRoomCell(cell))
                        continue;

                    _targetArea.Add(cell);
                    ShowTelegraph(cell);
                }
            }

            if (_targetArea.Count > 0)
                StartCoroutine(ExecutePendingAttack(_pendingAttackCell.Value, 1.5f));
            else
                _pendingAttackCell = null;
        }
    }

    protected override IEnumerator ExecutePendingAttack(Vector2Int targetCell, float delaySeconds)
    {
        _isAttacking = true;
        yield return WaitGameSeconds(delaySeconds);

        if (!IsDead)
        {
            if (_animator != null)
                _animator.SetTrigger("Attack");

            if (_targetArea.Contains(Player.GridPosition))
            {
                int damage = RollAttackDamage();
                TryHitPlayer(damage);
            }

            foreach (var cell in _targetArea)
            {
                if (GridSystem.Instance != null && GridSystem.Instance.IsWalkable(cell))
                    GameEvents.OnTrapTerrainRequested?.Invoke(new TrapTerrainRequest(cell, 0, 1, TrapTerrainType.Fire));
            }
        }

        ClearTelegraphs();
        _attackActionPoints = 0f;
        _isAttacking = false;
        _pendingAttackCell = null;
    }
}
