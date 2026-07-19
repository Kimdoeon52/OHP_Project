using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 불의 정령 (EnemyFireSpirit) [Elite] — Area 3 (지옥, 11~15층)
///
/// 핵심 메커니즘:
///   1. 4방향 직선 화염 마법 공격: 상/하/좌/우 직선상에 플레이어가 사거리 내에 포착되면
///      해당 라인에 붉은 텔레그래프를 표시하고 다음 턴에 화염을 뿜어 공격합니다.
///   2. 플레이어가 사거리(visionRange) 내 직선 상에 있으면 정지하여 조준하고,
///      직선 상에 없으면 플레이어 방향으로 접근합니다.
/// </summary>
public class EnemyFireSpirit : EnemyBase
{
    private List<Vector2Int> _pendingAttackLine;

    protected override IEnumerator ExecutePendingAttack(Vector2Int targetCell, float delaySeconds)
    {
        _isAttacking = true;

        if (_pendingAttackLine != null)
        {
            if (_pendingAttackLine.Count > 0)
            {
                UpdateSpriteDirection(_pendingAttackLine[0]);
            }

            yield return new WaitForSeconds(delaySeconds);

            if (!IsDead)
            {
                if (_animator != null)
                {
                    _animator.SetTrigger("Attack");
                }

                yield return new WaitForSeconds(0.2f); // 공격 애니메이션 딜레이

                // 발사 경로 위에 플레이어가 있으면 피해 적용
                if (_pendingAttackLine.Contains(Player.GridPosition))
                {
                    int damage = RollAttackDamage();
                    TryHitPlayer(damage);
                }
            }
            _pendingAttackLine = null;
        }

        ClearTelegraphs();
        _isAttacking = false;
        _pendingAttackCell = null;
    }

    protected override void PerformMovement()
    {
        // 공격 예약 중이거나 조준 선 상에 플레이어가 포착되면 정지
        if (_pendingAttackLine != null || ComputeLineToPlayer() != null)
        {
            return;
        }

        base.PerformMovement();
    }

    protected override void CheckAttackTelegraph()
    {
        if (IsDead || _isAttacking || _pendingAttackLine != null) return;

        var line = ComputeLineToPlayer();
        if (line != null)
        {
            _pendingAttackLine = line;
            if (line.Count > 0)
            {
                UpdateSpriteDirection(line[0]);
            }

            foreach (var cell in line)
            {
                ShowTelegraph(cell);
            }

            StartCoroutine(ExecutePendingAttack(_pendingAttackLine[0], 0.6f));
        }
    }

    private List<Vector2Int> ComputeLineToPlayer()
    {
        var diff = Player.GridPosition - GridPosition;

        Vector2Int dir;
        if (diff.x == 0 && diff.y != 0) dir = new Vector2Int(0, System.Math.Sign(diff.y));
        else if (diff.y == 0 && diff.x != 0) dir = new Vector2Int(System.Math.Sign(diff.x), 0);
        else return null;

        int maxRange = data != null ? (int)data.visionRange : 6;
        var result = new List<Vector2Int>();
        var current = GridPosition + dir;

        for (int step = 0; step < maxRange; step++)
        {
            if (!GridSystem.Instance.IsInBounds(current)) return null;
            if (!GridSystem.Instance.IsWalkable(current)) return null;

            // 문(Door) 위치는 화염 줄기가 방 밖/복도로 넘어가지 못하도록 공격 라인을 차단합니다.
            if (GridSystem.Instance.GetDoorAt(current) != null) return null;

            result.Add(current);

            if (current == Player.GridPosition) return result;

            current += dir;
        }

        return null;
    }
}
