using System.Collections.Generic;


// 위치가 확정된 방 리스트를 받아 TerrainTilemaps 의 각 레이어에 실제 타일을 배치
public interface IRegionPainter
{
    void Paint(
        List<RoomData> rooms,
        TerrainTilemaps tilemaps,
        FloorFeeling feeling,
        DungeonRandom rng
    );
}
