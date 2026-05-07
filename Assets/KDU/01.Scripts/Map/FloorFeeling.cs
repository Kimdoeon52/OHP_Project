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
/// FloorFeeling.ApplyToSpawn 시그니처용 Phase 1 placeholder.
/// Phase 4 에서 _Game/Spawning/EnemySpawner 가 이 인터페이스를 구현한다.
/// </summary>
public interface IEnemySpawner { }
