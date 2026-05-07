using System.Collections.Generic;
using UnityEngine.Tilemaps;

/// <summary>
/// 위치가 확정된 방 리스트를 받아 floorTilemap / wallTilemap 에 실제 타일을 배치한다.
/// Floor Feeling 이 함께 전달되며 페인터는 이를 참고해 타일 종류/비율을 조정할 수 있다.
/// fogTilemap 은 건드리지 않음 (FogRenderer 가 담당).
/// </summary>
public interface IRegionPainter
{
    void Paint(
        List<RoomData> rooms,
        Tilemap floorTilemap,
        Tilemap wallTilemap,
        FloorFeeling feeling,
        DungeonRandom rng
    );
}
