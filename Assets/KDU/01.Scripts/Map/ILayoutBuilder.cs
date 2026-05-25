using System.Collections.Generic;

/// <summary>
/// Initialize 단계에서 만들어진 방 리스트를 받아 위치와 연결
/// </summary>
public interface ILayoutBuilder
{
    List<RoomData> Build(List<RoomData> rooms, DungeonRandom rng);
}
