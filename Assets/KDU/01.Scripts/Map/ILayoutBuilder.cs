using System.Collections.Generic;

/// <summary>
/// Initialize 단계에서 만들어진 방 리스트를 받아 위치(bounds)와 연결(connections)을 결정한다.
/// 복도 추가 및 entrance → exit 연결성 보장은 구현체 책임.
/// </summary>
public interface ILayoutBuilder
{
    List<RoomData> Build(List<RoomData> rooms, DungeonRandom rng);
}
