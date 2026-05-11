using System.Collections.Generic;

/// <summary>
/// 층별 테마 (물/풀/낭떨어지/함정/어둠 등) 추상 베이스.
/// Initialize / Paint / Spawn 각 단계에 훅을 제공해 한 곳에서 층 전체 변형을 관리한다.
/// 새 Feeling 추가 = 이 클래스 상속 + FloorFeelingPool.asset 에 등록 (기존 코드 수정 X).
/// </summary>
public abstract class FloorFeeling
{
    public abstract string Id { get; }

    // 각 훅은 가상 no-op. 영향이 없는 단계는 오버라이드 안 해도 됨.
    public virtual void ApplyToInit(List<RoomData> rooms) { }
    public virtual void ApplyToPaint(IRegionPainter painter) { }
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
