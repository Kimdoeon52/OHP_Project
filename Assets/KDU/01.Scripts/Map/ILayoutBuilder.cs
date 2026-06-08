using System.Collections.Generic;


// 방 리스트를 받아 위치와 연결
public interface ILayoutBuilder
{
    List<RoomData> Build(List<RoomData> rooms, DungeonRandom rng);
}
