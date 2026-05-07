# MAP_GENERATION.md — 맵 생성 시스템 명세

> Claude Code가 맵 생성 작업을 할 때 참조할 전용 명세 문서.
> `CLAUDE.md`의 코드 규칙을 따른 상태에서, 이 문서의 시스템 명세를 구현한다.

---

## 0. 작업 시작 전 필수 확인

- `CLAUDE.md`의 **티어 구분**(Tier 1/2)과 **파일 크기 가이드** 준수
- `60sec_GDD_v0.1.docx`의 4장(맵 생성 시스템) 참조
- 기존 `_Core` 시스템(GridSystem, VisionSystem 등)과의 호환성 우선
- **새 파일 생성 시 항상 디렉토리 가이드(`CLAUDE.md` 디렉토리 구조)에 맞게 배치**

---

## 1. 시스템 개요

### 핵심 컨셉
- 매 층마다 절차적으로 새 맵 생성
- 시드 기반 결정론적 생성 (동일 시드 = 동일 맵)
- Safe Room이 항상 시작점, Exit Room이 항상 출구
- Floor Feeling이 층별 테마 결정 (50% 확률 발동)

### 영감
Shattered Pixel Dungeon v0.6.0+ "room-first" 방식.
**SPD 코드는 절대 가져오지 말 것** (GPLv3). 디자인 원리만 차용.

---

## 2. 3단계 파이프라인

```
[Initialize]  →  [Build]  →  [Paint]  →  [Spawn]
방 풀 결정       방 배치/연결    타일 배치     몬스터/아이템 배치
```

### 2.1 Initialize 단계
- 방 종류·개수·크기 결정
- `RoomData` 인스턴스 리스트 생성
- 출력: `List<RoomData>` (위치 미정 상태)

### 2.2 Build 단계
- `ILayoutBuilder`가 방 위치 결정 + 그래프 연결
- 복도(Tunnel) 추가
- 연결성 검증 (entrance → exit 도달 가능한가)
- 출력: 위치가 확정된 `List<RoomData>` + 복도 정보

### 2.3 Paint 단계
- `IRegionPainter`가 Tilemap에 타일 배치
- 방 내부 타일, 벽, 문, 복도 그리기
- Floor Feeling 적용 (물/풀/함정 비율 조정)
- 출력: 완성된 floorTilemap + wallTilemap

### 2.4 Spawn 단계
- `EnemySpawner`가 방 타입별 몬스터 배치
- `ItemSpawner`가 아이템 배치
- Safe Room은 스폰 제외

---

## 3. 핵심 클래스 명세

### 3.1 Tier 1 (코어 — `_Core/Map/`)

#### `RoomData`
```csharp
// _Core/Map/RoomData.cs
public class RoomData {
    public RoomType type;                 // Safe / Normal / Special / Secret / Exit
    public RectInt bounds;                 // 방 위치·크기
    public SizeCategory sizeCategory;      // Normal / Large / Giant
    public List<Vector2Int> floorCells;    // 방이 점유하는 격자 셀 목록
    public List<Vector2Int> doorCells;     // 입구 셀 (스폰 제외용)
    public List<RoomData> connections;     // 연결된 방
    public int distanceFromSafe;           // Safe Room으로부터 그래프 거리

    // 빌더가 채움
}

public enum RoomType { Safe, Normal, Special, Secret, Exit }
public enum SizeCategory { Normal, Large, Giant }
```

#### `ILayoutBuilder`
```csharp
// _Core/Map/ILayoutBuilder.cs
public interface ILayoutBuilder {
    List<RoomData> Build(List<RoomData> rooms, DungeonRandom rng);
}
```

#### `IRegionPainter`
```csharp
// _Core/Map/IRegionPainter.cs
public interface IRegionPainter {
    void Paint(
        List<RoomData> rooms,
        Tilemap floorTilemap,
        Tilemap wallTilemap,
        FloorFeeling feeling,
        DungeonRandom rng
    );
}
```

#### `FloorFeeling` (추상)
```csharp
// _Core/Map/FloorFeeling.cs
public abstract class FloorFeeling {
    public abstract string Id { get; }
    public abstract void ApplyToInit(List<RoomData> rooms);
    public abstract void ApplyToPaint(IRegionPainter painter);
    public abstract void ApplyToSpawn(EnemySpawner spawner);
}
```

#### `DungeonRandom`
```csharp
// _Core/Map/DungeonRandom.cs
public class DungeonRandom {
    private System.Random rng;
    public int Seed { get; private set; }

    public void Init(int seed) { Seed = seed; rng = new System.Random(seed); }
    public int Range(int minInclusive, int maxExclusive);
    public float Value();                  // [0, 1)
    public T Pick<T>(IList<T> list);
    public bool Chance(float probability);
}
```

### 3.2 Tier 2 (구현체 — `_Game/`)

#### `LoopBuilder` (`_Game/Builders/`)
- 주요 방을 루프 형태로 배치
- entrance(Safe)와 exit를 루프 반대편에 배치
- 나머지 방은 브랜치로 연결
- 방 간 직선/L자 복도 추가

#### `DefaultPainter` (`_Game/Painters/`)
- 기본 floor + wall 타일 배치
- RuleTile로 벽 자동 연결
- doorCells에 문 타일

#### `WaterFeeling`, `GrassFeeling` 등 (`_Game/Feelings/`)
- 각 Feeling이 자체적으로 페인터/스폰러를 어떻게 변경할지 결정
- 새 Feeling 추가 = 클래스 1개 + 풀 등록

---

## 4. 방 타입별 규칙

| 타입 | 수량 | 핵심 규칙 |
|------|------|-----------|
| Safe | 1 | 시작점, 타이머 정지, 몬스터 진입 불가, 입구 1개 |
| Normal | 가변 | 일반 전투, 크기별 몬스터 수 차등 |
| Special | 1~2 | 입구 1개, 잠금 가능, 고유 보상 |
| Secret | 0~1 | 숨겨진 입구, SECRETS Feeling 시 확률↑ |
| Exit | 1 | Safe Room과 최대 그래프 거리 배치 |

### 방 크기 카테고리
| 카테고리 | 크기 범위 |
|---------|-----------|
| Normal | 5×5 ~ 7×7 |
| Large | 8×8 ~ 12×12 |
| Giant | 12×12 ~ 16×16 |

---

## 5. Floor Feeling 명세

### 5.1 발동 규칙
- 50% 확률로 발동
- 발동 시 풀에서 1개 랜덤 선택
- `DungeonRandom`만 사용 (UnityEngine.Random 금지)

### 5.2 Feeling 풀

| Feeling | 영향 |
|---------|------|
| Water | 물 타일 40% 이상, 이동 슬로우 |
| Grass | 풀 타일 다수, 시야 차단, 이슬 드롭률↑ |
| Chasm | 낭떨어지 다수, 우회 강제 |
| Traps | 함정 30% 이상, 몬스터 수 -1 |
| Dark | 시야 반경 -2, 횃불 가치↑ |

### 5.3 확장 방법

새 Feeling 추가 시:
1. `_Game/Feelings/MyNewFeeling.cs` 생성
2. `FloorFeeling` 상속 + 3개 Apply 메서드 구현
3. `FloorFeelingPool.asset`에 등록

기존 코드 수정 없이 확장 가능해야 함.

---

## 6. 시드 시스템

### 6.1 시드 흐름
```
Run Seed (전체 런)
    ↓
Floor Seed = Run Seed XOR (depth * 31)
    ↓
DungeonRandom.Init(Floor Seed)
    ↓
Initialize / Build / Paint / Spawn 순차 호출
```

### 6.2 결정론 보장 규칙
- **모든 무작위 호출은 `DungeonRandom` 통일**
- `UnityEngine.Random`, `System.Random` 직접 호출 금지
- 호출 순서가 곧 결과를 결정 — 리팩토링 시 호출 순서 보존
- 동일 시드 + 동일 depth → 항상 동일 맵

### 6.3 시드 디버그
```csharp
public class GenerationDebug {
    public int seed;
    public int depth;
    public int rngCallCount;
    public string roomGraphHash;
    public string tilemapHash;
}
```
버그 리포트 재현용으로 위 정보 저장.

---

## 7. Tilemap 구조

### 7.1 GridSystem과의 호환
기존 GridSystem이 두 Tilemap 구조를 사용:
```
Grid
├── floorTilemap  (HasTile = 통행 가능)
└── wallTilemap   (HasTile = 통행 불가, LOS 차단)
```

페인터는 이 두 Tilemap에 직접 SetTile.

### 7.2 RuleTile 사용
- 벽은 RuleTile로 자동 연결
- 비트마스크 직접 구현 X
- 지역별로 다른 RuleTile 에셋 분리 (SewerWall.asset 등)

### 7.3 Fog 시스템
- 페인터는 fogTilemap 건드리지 않음
- FogRenderer가 GetAllCells() 호출하여 자동 갱신

---

## 8. Archer 배치 제약 (중요)

기존 `GridEnemyArcher`는 4방향 직선 LOS만 지원.
스폰 시 다음 조건 검사 필수:

```csharp
bool CanPlaceArcher(Vector2Int cell) {
    // 4방향 중 최소 1방향이 3칸 이상 개방되어야 함
    foreach (var dir in fourDirections) {
        int openCells = CountOpenCells(cell, dir, maxRange: enemy.visionRange);
        if (openCells >= 3) return true;
    }
    return false;
}
```

이 검사 안 하면 Archer가 벽 보고 멍 때림.

---

## 9. 작업 순서 (Claude Code에 시킬 순서)

### Phase 1 — 코어 인프라 (Tier 1)
1. `RoomData`, `RoomType`, `SizeCategory` 정의
2. `DungeonRandom` 구현
3. `ILayoutBuilder`, `IRegionPainter` 인터페이스
4. `FloorFeeling` 추상 클래스
5. `MapGenerator` 메인 진입점 (Initialize → Build → Paint 호출)

### Phase 2 — 최소 동작 (Tier 2)
6. `LoopBuilder` 1종
7. `DefaultPainter` 1종
8. Safe Room / Exit Room 자동 배치
9. 통합 테스트: 시드 입력 → 맵 생성 → Tilemap 출력

### Phase 3 — Feeling 시스템
10. `WaterFeeling` 1종 (테스트용)
11. Feeling 풀 SO 등록 구조
12. 나머지 Feeling 4종

### Phase 4 — 스폰 통합
13. `EnemySpawner` 방 타입별 룰
14. Archer LOS 검증 통과
15. 거리 기반 난이도 차등

### Phase 5 — 다양성/폴리싱
16. `FigureEightBuilder` 추가
17. 지역별 페인터 (컨셉 1/2/3)
18. Special/Secret 방 종류 추가

---

## 10. 테스트 체크리스트

각 Phase 완료 후 확인:

- [ ] 동일 시드 2회 생성 → 완전히 동일한 맵 (Tilemap diff = 0)
- [ ] 100개 시드 생성 → 모두 entrance ~ exit 연결됨 (BFS 검증)
- [ ] Safe Room에 몬스터 절대 안 들어감
- [ ] 입구 인접 셀에 몬스터 스폰 0건
- [ ] 새 Feeling 추가가 기존 코드 수정 없이 동작
- [ ] 새 Builder/Painter 추가가 기존 코드 수정 없이 동작

---

## 11. 금지 사항

- ❌ `UnityEngine.Random` 또는 `System.Random` 직접 호출 (반드시 `DungeonRandom`)
- ❌ Builder/Painter 안에 게임 로직 (몬스터 스폰 등) 섞기
- ❌ Tilemap 직접 참조 (GridSystem 경유)
- ❌ SPD 코드 복붙 (GPLv3, 디자인만 참고)
- ❌ Phase를 건너뛰고 한 번에 모든 Feeling 구현
- ❌ 한 클래스가 여러 책임 (예: Builder가 Painter 역할까지)
- ❌ 매직 넘버 (50%, 5칸 등은 GameConfig 또는 const)
