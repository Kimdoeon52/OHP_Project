using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 층별 테마 (Hazard/Traps/Dark — GDD 6.3 3종 고정) 추상 베이스.
/// Initialize / Spawn 단계에 훅을 제공하며, Paint 단계는 페인터가 인자로 받은 feeling 을 직접 읽어 처리한다
/// </summary>
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
