using System.Collections.Generic;
using UnityEngine;

// 층별 테마
public abstract class FloorFeeling : ScriptableObject
{
    public abstract string Id { get; }

    public virtual void ApplyToInit(List<RoomData> rooms) { }
    public virtual void ApplyToSpawn(IEnemySpawner spawner) { }
}

public interface IEnemySpawner
{
    void Spawn(System.Collections.Generic.List<RoomData> rooms, int depth, DungeonRandom rng);
}
