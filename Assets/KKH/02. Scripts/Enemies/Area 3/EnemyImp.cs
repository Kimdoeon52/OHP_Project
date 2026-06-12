using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyImp : EnemyBase
{
    private readonly List<Vector2Int> _targetArea = new List<Vector2Int>();

    protected override void CheckAttackTelegraph()
    {
        if (IsDead || _isAttacking)
        {
            return;
        }

        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            _pendingAttackCell = Player.GridPosition;
            UpdateSpriteDirection(_pendingAttackCell.Value);

            _targetArea.Clear();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    _targetArea.Add(_pendingAttackCell.Value + new Vector2Int(x, y));
                }
            }

            foreach (var cell in _targetArea)
            {
                ShowTelegraph(cell);
            }

            StartCoroutine(ExecutePendingAttack(_pendingAttackCell.Value, 1.5f));
        }
    }

    protected override IEnumerator ExecutePendingAttack(Vector2Int targetCell, float delaySeconds)
    {
        _isAttacking = true;
        yield return new WaitForSeconds(delaySeconds);

        if (!IsDead)
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }

            if (_targetArea.Contains(Player.GridPosition))
            {
                int damage = RollAttackDamage();
                TryHitPlayer(damage);
            }

            foreach (var cell in _targetArea)
            {
                if (GridSystem.Instance.IsWalkable(cell))
                {
                    // TODO: TrapTerrainController 또는 Hazard 지형 생성기 연동하여 1초 유지되는 화염 생성
                    // TrapTerrainController.SpawnTemporaryHazard(cell, HazardType.Fire, 1f);
                }
            }
        }
        ClearTelegraphs();
        _isAttacking = false;
    }

}
