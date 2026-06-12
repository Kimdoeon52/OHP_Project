using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마법사 스켈레톤 (EnemyMageSkeleton) [Common] — Area 2 (던전, 6~10층)
///
/// 핵심 메커니즘:
///   1. 4방향 직선 마법 공격: 상/하/좌/우 직선상에 플레이어가 포착되면
///      그 사이 모든 타일에 붉은 텔레그래프 표시 후 다음 턴에 발사
///   2. 조건부 추적 이동: 마법 조준 중이거나 직선 시야가 확보되면 제자리 유지,
///      아닐 때만 플레이어 방향으로 1칸 이동
///   3. 뼈 방패: data.hasShield == true이면 매 턴 시작 시 50% 확률로 방패 활성화
///      → 다음 피격 1회를 0~2만큼 추가 경감
///
/// override 목록:
///   OnTurnUpdate()          → 턴 시작 시 방패 확률 롤 후 부모 OnTurnUpdate 호출
///   TakeDamage()            → 방패 활성 시 추가 피해 경감
///   ExecutePendingAttack()  → 마법 라인 위 플레이어에게 원거리 피해
///   PerformMovement()       → 마법 조준 중이면 정지, 아니면 추적
///   CheckAttackTelegraph()  → 직선 시야 연산 후 마법 예고
/// </summary>
public class EnemyMageSkeleton : EnemyBase
{
    /// <summary>
    /// 이전 턴에 예약된 마법 공격 경로 셀 목록.
    /// null이면 예약된 마법 없음.
    /// CheckAttackTelegraph에서 설정, ExecutePendingAttack에서 소비.
    /// </summary>
    private List<Vector2Int> _pendingAttackLine;

    /// <summary>
    /// 이번 피격을 흡수할 뼈 방패 활성화 플래그.
    /// OnTurnUpdate에서 50% 확률로 true가 되고, TakeDamage에서 소비 후 즉시 false로 리셋.
    /// </summary>
    private bool _shieldRaised = false;

    /// <summary>
    /// 턴 시작 override.
    /// 방패 소유(data.hasShield) 시 50% 확률로 _shieldRaised = true 설정 후 부모 OnTurnUpdate 실행.
    /// </summary>
    public override void OnTurnUpdate()
    {
        if (data != null && data.hasShield && !IsDead)
        {
            if (Random.value <= 0.5f)
            {
                _shieldRaised = true;
            }
        }

        // AP 충전 → ExecutePendingAttack → PerformMovement → CheckAttackTelegraph 순 실행
        base.OnTurnUpdate();
    }

    /// <summary>
    /// 피해 처리 override.
    /// 방패가 올라가 있으면 0~2 추가 경감 후 base.TakeDamage 호출.
    /// 방패는 1회 사용 후 즉시 해제.
    /// </summary>
    public override void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        if (_shieldRaised)
        {
            int shieldReduction = Random.Range(0, 3); // 0~2 경감
            int rawDamageAfterShield = Mathf.Max(0, amount - shieldReduction);

            Debug.Log($"{name} 방패 발동: 피해 {shieldReduction} 경감 (남은 피해: {rawDamageAfterShield})");

            _shieldRaised = false; // 1회성
            base.TakeDamage(rawDamageAfterShield);
        }
        else
        {
            base.TakeDamage(amount);
        }
    }

    /// <summary>
    /// 마법 공격 발동 override.
    /// _pendingAttackLine 상에 플레이어가 있으면 원거리 피해 + 상태이상 적용.
    /// 발동 후 _pendingAttackLine 초기화 + 텔레그래프 정리.
    /// </summary>
    protected override IEnumerator ExecutePendingAttack(Vector2Int targetCell, float delaySeconds)
    {
        _isAttacking = true;

        if (_pendingAttackLine != null)
        {
            // 발사 방향(라인 첫 번째 셀)으로 스프라이트 방향 전환
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
    }

    /// <summary>
    /// 이동 override.
    /// 조건:
    ///   _pendingAttackLine != null (마법 예약 중)  → 정지
    ///   ComputeLineToPlayer() != null (직선 시야 확보) → 정지 (마법 조준 준비)
    ///   그 외 → visionRange 내면 TryStepTowardPlayer로 1칸 추적
    /// </summary>
    protected override void PerformMovement()
    {
        // 마법 예약 중이거나 직선 시야 확보 상태면 이동하지 않음
        if (_pendingAttackLine != null || ComputeLineToPlayer() != null)
        {
            return;
        }

        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            TryStepTowardPlayer();
        }
    }

    /// <summary>
    /// 마법 예고 override.
    /// ComputeLineToPlayer로 직선 경로 계산 후 경로 전체에 텔레그래프 생성.
    /// 이미 마법 예약 중이면 신규 텔레그래프 생성 안 함.
    /// </summary>
    protected override void CheckAttackTelegraph()
    {
        // 이미 예약된 마법이 있으면 새 예고 차단
        if (IsDead || _isAttacking || _pendingAttackLine != null) return;

        var line = ComputeLineToPlayer();
        if (line != null)
        {
            _pendingAttackLine = line;
            if (line.Count > 0)
            {
                UpdateSpriteDirection(line[0]);
            }

            // 경로 전체에 붉은 타일 표시
            foreach (var cell in line)
            {
                ShowTelegraph(cell);
            }
            StartCoroutine(ExecutePendingAttack(_pendingAttackLine[0], 0.8f));
        }
    }

    /// <summary>
    /// 현재 위치에서 플레이어 방향으로 4방향 직선 탐색.
    /// 플레이어가 같은 행 또는 열에 있고 사이에 벽이 없으면 경로 셀 목록 반환.
    /// 대각선이거나 벽에 막히면 null 반환.
    /// </summary>
    /// <returns>플레이어까지 이어지는 셀 목록 (플레이어 포함), 불가 시 null</returns>
    private List<Vector2Int> ComputeLineToPlayer()
    {
        var diff = Player.GridPosition - GridPosition;

        // 4방향 정렬 판별 (대각선이면 null)
        Vector2Int dir;
        if (diff.x == 0 && diff.y != 0) dir = new Vector2Int(0, System.Math.Sign(diff.y));
        else if (diff.y == 0 && diff.x != 0) dir = new Vector2Int(System.Math.Sign(diff.x), 0);
        else return null; // 대각선 또는 동일 칸

        int maxRange = data != null ? (int)data.visionRange : 6;
        var result = new List<Vector2Int>();
        var current = GridPosition + dir;

        for (int step = 0; step < maxRange; step++)
        {
            if (!GridSystem.Instance.IsInBounds(current)) return null; // 맵 경계 밖
            if (!GridSystem.Instance.IsWalkable(current)) return null; // 벽이 라인 차단

            result.Add(current);

            if (current == Player.GridPosition) return result; // 플레이어 도달

            current += dir;
        }

        return null; // 사정거리 내에 플레이어 없음
    }
}
