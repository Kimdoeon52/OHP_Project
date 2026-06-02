using System.Collections.Generic;
using UnityEngine;

/*
 * 2x2, 3x2, 3x3처럼 여러 격자 칸을 동시에 차지하는 보스/대형 엔티티용 인터페이스입니다.
 *
 * 왜 필요한가:
 * - 기존 IGridEntity는 GridPosition 하나만 제공합니다.
 * - TurnManager.GetEntityAt(cell)은 현재 "그 칸에 어떤 대상이 있는가"를 찾을 때 사용됩니다.
 * - 1x1 몬스터는 GridPosition 하나만 비교해도 충분하지만, 큰 보스는 몸통이 여러 칸입니다.
 * - 예를 들어 2x2 보스의 기준점이 (10, 10)이고 실제 몸이
 *   (10,10), (11,10), (10,11), (11,11)을 차지한다면,
 *   플레이어가 (11,10)을 공격해도 같은 보스를 찾아야 합니다.
 *
 * 적용 방법:
 * 1. 큰 보스 클래스에 IGridEntity와 IDamageable처럼 이 인터페이스를 추가합니다.
 *    예:
 *    public class BigBoss : BossBase, IGridEntity, IMultiCellGridEntity, IDamageable
 *
 * 2. 보스의 기준 좌표는 기존처럼 GridPosition으로 유지합니다.
 *    보통 좌하단 칸 또는 보스 중심에 가까운 기준 칸 하나를 정합니다.
 *    중요한 점은 "기준 칸이 무엇인지"를 보스 코드 안에서 일관되게 쓰는 것입니다.
 *
 * 3. OccupiedCells에서 보스가 실제로 차지하는 모든 칸을 반환합니다.
 *    2x2 좌하단 기준 예:
 *    public IEnumerable<Vector2Int> OccupiedCells
 *    {
 *        get
 *        {
 *            yield return GridPosition + new Vector2Int(0, 0);
 *            yield return GridPosition + new Vector2Int(1, 0);
 *            yield return GridPosition + new Vector2Int(0, 1);
 *            yield return GridPosition + new Vector2Int(1, 1);
 *        }
 *    }
 *
 * 4. 크기를 인스펙터에서 바꾸고 싶다면 Vector2Int size를 둡니다.
 *    예:
 *    [SerializeField] private Vector2Int bossSize = new Vector2Int(2, 2);
 *
 *    public IEnumerable<Vector2Int> OccupiedCells
 *    {
 *        get
 *        {
 *            for (int x = 0; x < bossSize.x; x++)
 *            for (int y = 0; y < bossSize.y; y++)
 *                yield return GridPosition + new Vector2Int(x, y);
 *        }
 *    }
 *
 * 5. 보스가 이동하면 GridPosition만 갱신하면 됩니다.
 *    OccupiedCells는 GridPosition 기준으로 매번 계산되므로 별도 리스트를 직접 갱신할 필요가 없습니다.
 *
 * 주의할 점:
 * - OccupiedCells는 "공격/충돌 판정용 논리 칸"입니다.
 *   SpriteRenderer 크기나 Transform scale만 키운다고 자동으로 2x2 판정이 되지 않습니다.
 * - 보스의 공격 패턴에서 플레이어와의 거리, 인접 여부, 안전 칸 계산도 GridPosition 하나가 아니라
 *   OccupiedCells 전체 기준으로 계산해야 큰 보스답게 동작합니다.
 * - 지금 TurnManager는 이 인터페이스를 구현한 엔티티가 있으면 OccupiedCells를 먼저 검사합니다.
 *   그래서 플레이어가 큰 보스의 몸통 어느 칸을 공격해도 같은 IDamageable 보스를 찾을 수 있습니다.
 */
public interface IMultiCellGridEntity : IGridEntity
{
    /// <summary>
    /// 이 엔티티가 현재 차지하는 모든 격자 칸입니다.
    /// 반환되는 칸 중 하나라도 플레이어 공격 대상 칸과 같으면,
    /// TurnManager.GetEntityAt(cell)은 이 엔티티를 반환합니다.
    /// </summary>
    IEnumerable<Vector2Int> OccupiedCells { get; }
}
