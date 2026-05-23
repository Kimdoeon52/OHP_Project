using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 층별 테마 (Hazard/Traps/Dark — GDD 6.3 3종 고정) 추상 베이스.
/// Initialize / Spawn 단계에 훅을 제공하며, Paint 단계는 페인터가 인자로 받은 feeling 을 직접 읽어 처리한다
/// (PHASE_3_5_PLAN.md Q1 결정 A — ApplyToPaint 제거).
/// 새 Feeling 추가 = 이 클래스 상속 + [CreateAssetMenu] + FloorFeelingPool.asset 에 등록 (기존 코드 수정 X).
/// </summary>
public abstract class FloorFeeling : ScriptableObject
{
    public abstract string Id { get; }

    // 각 훅은 가상 no-op. 영향이 없는 단계는 오버라이드 안 해도 됨.
    public virtual void ApplyToInit(List<RoomData> rooms) { }
    public virtual void ApplyToSpawn(IEnemySpawner spawner) { }
}

/// <summary>
/// 한 층의 모든 적을 스폰하는 책임. MapGenerator 가 Build/Paint 완료 후 Spawn(rooms, depth, rng) 호출.
/// Phase 4 에서 KKH/EnemySystem.EnemySpawner 가 이 인터페이스를 구현 (RoomSpawnRule 디스패치 + prefab 인스턴스화).
/// FloorFeeling.ApplyToSpawn 이 이 인터페이스를 받아 스폰 파라미터를 조정 (몬스터 -1 등).
/// </summary>
public interface IEnemySpawner
{
    void Spawn(System.Collections.Generic.List<RoomData> rooms, int depth, DungeonRandom rng);
}
