using System.Collections.Generic;
using UnityEngine;

// 2x2, 3x2, 3x3처럼 여러 격자 칸을 동시에 차지하는 보스/대형 엔티티용 인터페이스

public interface IMultiCellGridEntity : IGridEntity
{
    // 몬스터가 맞게 될 때 검사할 칸
    IEnumerable<Vector2Int> OccupiedCells { get; }
}
