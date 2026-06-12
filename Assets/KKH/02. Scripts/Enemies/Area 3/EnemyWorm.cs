using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWorm : EnemyBase
{
    private Coroutine _moveCoroutine;
    private bool _isMovingVisual = false;

    public void TakeDamage(int amount, DamageType damageType)
    {
        if (IsDead || amount <= 0) return;

        if ((_isMovingVisual || (_animator.GetBool("Walk"))) && damageType == DamageType.Ranged)
        {
            if (_isMovingVisual)
            {
                return;
            }
        }

        base.TakeDamage(amount);
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
