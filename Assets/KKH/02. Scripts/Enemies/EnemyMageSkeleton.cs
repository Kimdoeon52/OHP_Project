using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마법사 스켈레톤 (방패 X/O) [Common] — Area 2 (던전, 6~10층)
/// 
/// 이동: 기본 추적, moveSpeed=0.75 (직선상에 플레이어가 없을 때만 이동)
/// 공격: 원거리 마법 (4방향 직선 LOS) — 텔레그래프 예고 후 다음 턴 발사
/// 특수(방패O): 50% 확률로 방패 들기 → 다음 피격 0~2 추가 감소
/// 
/// 방패 소지 여부는 EnemyData의 hasShield 플래그로 결정됩니다.
/// </summary>
public class EnemyMageSkeleton : EnemyBase
{
    private List<Vector2Int> _pendingAttackLine;
    private bool _shieldRaised = false;

    public override void OnTurnUpdate()
    {
        // 턴 시작 시 방패를 들고 있고 죽지 않았다면 50% 확률로 방패 들기
        if (data != null && data.hasShield && !IsDead)
        {
            if (Random.value <= 0.5f)
            {
                _shieldRaised = true;
            }
        }

        base.OnTurnUpdate();
    }

    public override void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        if (_shieldRaised)
        {
            int shieldReduction = Random.Range(0, 3); // 0~2 감소
            int rawDamageAfterShield = Mathf.Max(0, amount - shieldReduction);
            
            Debug.Log($"{name}의 방패가 마법 마비 피해를 {shieldReduction} 감소시켰습니다. (남은 피해: {rawDamageAfterShield})");
            
            _shieldRaised = false; // 일회성 차감 후 해제
            base.TakeDamage(rawDamageAfterShield);
        }
        else
        {
            base.TakeDamage(amount);
        }
    }

    protected override void ExecutePendingAttack()
    {
        if (_pendingAttackLine != null)
        {
            if (_pendingAttackLine.Count > 0)
            {
                UpdateSpriteDirection(_pendingAttackLine[0]);
            }

            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }

            if (_pendingAttackLine.Contains(Player.GridPosition))
            {
                int damage = RollAttackDamage();
                Player.TakeDamage(damage);
                TryApplyOnHitEffect(Player);
            }
            _pendingAttackLine = null;
        }
        ClearTelegraphs();
    }

    protected override void PerformMovement()
    {
        // 이미 마법을 준비 중이거나 직선 상에 플레이어가 있으면 움직이지 않고 조준 유지
        if (_pendingAttackLine != null || ComputeLineToPlayer() != null)
        {
            return;
        }

        // 직선 상에 플레이어가 없으면 플레이어 방향으로 추적 이동
        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            TryStepTowardPlayer();
        }
    }

    protected override void CheckAttackTelegraph()
    {
        // 이미 마법을 준비 중이면 추가 조준선 표시 안 함
        if (_pendingAttackLine != null) return;

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
        }
    }

    /// <summary>
    /// 4방향 직선상에 플레이어가 있고, 사이에 벽이 없으면 라인 셀 리스트 반환. 아니면 null.
    /// </summary>
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
            if (!GridSystem.Instance.IsWalkable(current)) return null;  // 벽이 시야 차단

            result.Add(current);

            if (current == Player.GridPosition) return result;

            current += dir;
        }

        return null;
    }
}
