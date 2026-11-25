using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 마법사 스켈레톤 (EnemyMageSkeleton) — 4방향 직선 마법 예고·사격 및 방패 가드
public class EnemyMageSkeleton : EnemyBase
{
    private List<Vector2Int> _pendingAttackLine;
    private bool _shieldRaised = false;

    public override void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        if (_shieldRaised)
        {
            int shieldReduction = GetRng().Range(0, 3);
            int rawDamageAfterShield = Mathf.Max(0, amount - shieldReduction);
            _shieldRaised = false;
            base.TakeDamage(rawDamageAfterShield);
            return;
        }

        base.TakeDamage(amount);
    }

    protected override IEnumerator ExecutePendingAttack(Vector2Int targetCell, float delaySeconds)
    {
        _isAttacking = true;

        if (_pendingAttackLine != null)
        {
            if (_pendingAttackLine.Count > 0)
                UpdateSpriteDirection(_pendingAttackLine[0]);

            yield return WaitGameSeconds(delaySeconds);

            if (!IsDead)
            {
                if (_animator != null)
                    _animator.SetTrigger("Attack");

                if (_pendingAttackLine.Contains(Player.GridPosition))
                {
                    int damage = RollAttackDamage();
                    TryHitPlayer(damage);
                }
            }

            _pendingAttackLine = null;
        }

        ClearTelegraphs();
        _attackActionPoints = 0f;
        _isAttacking = false;
        _pendingAttackCell = null;
    }

    protected override void PerformMovement()
    {
        if (data != null && data.hasShield && !IsDead)
        {
            if (GetRng().Chance(0.5f))
                _shieldRaised = true;
        }

        if (_pendingAttackLine != null || ComputeLineToPlayer() != null)
            return;

        base.PerformMovement();
    }

    protected override void CheckAttackTelegraph()
    {
        if (IsDead || _isAttacking || _pendingAttackLine != null)
            return;

        var line = ComputeLineToPlayer();
        if (line != null)
        {
            isDetected = true;
            _pendingAttackLine = line;
            if (line.Count > 0)
                UpdateSpriteDirection(line[0]);

            foreach (var cell in line)
                ShowTelegraph(cell);

            StartCoroutine(ExecutePendingAttack(_pendingAttackLine[0], 0.8f));
        }
    }

    private List<Vector2Int> ComputeLineToPlayer()
    {
        if (SafeRoomTimerGate.PlayerInSafeRoom)
            return null;

        var diff = Player.GridPosition - GridPosition;

        Vector2Int dir;
        if (diff.x == 0 && diff.y != 0)
            dir = new Vector2Int(0, System.Math.Sign(diff.y));
        else if (diff.y == 0 && diff.x != 0)
            dir = new Vector2Int(System.Math.Sign(diff.x), 0);
        else
            return null;

        int maxRange = data != null ? (int)data.visionRange : 6;
        var result = new List<Vector2Int>();
        var current = GridPosition + dir;

        for (int step = 0; step < maxRange; step++)
        {
            if (!GridSystem.Instance.IsInBounds(current))
                return null;

            if (!GridSystem.Instance.IsWalkable(current))
                return null;

            if (GridSystem.Instance.GetDoorAt(current) != null)
                return null;

            if (IsSafeRoomCell(current))
                return null;

            result.Add(current);

            if (current == Player.GridPosition)
                return result;

            current += dir;
        }

        return null;
    }
}
