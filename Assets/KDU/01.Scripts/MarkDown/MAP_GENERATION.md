# MAP_GENERATION.md — 맵 생성 시스템 명세

> Claude Code가 맵 생성 작업을 할 때 참조할 전용 명세 문서.
> `CLAUDE.md`의 코드 규칙을 따른 상태에서, 이 문서의 시스템 명세를 구현한다.
> **디자인 수치·규칙의 원본은 GDD다. 이 문서와 GDD가 충돌하면 GDD를 따른다.**

---

## 0. 작업 시작 전 필수 확인

- `CLAUDE.md`의 **티어 구분**(Tier 1/2)과 **파일 크기 가이드** 준수
- `Stolen_Time_GDD_v1_3.docx`의 **6장(맵 생성 시스템)** 참조 (관련: 5장 레벨 디자인, 11장 Safe Room)
- 기존 코어 시스템(GridSystem, VisionSystem 등)과의 호환성 우선
- **새 파일 생성 시 항상 디렉토리 가이드(`CLAUDE.md` 디렉토리 구조)에 맞게 배치** — 모든 맵 관련 파일은 `Assets/KDU/01.Scripts/` 아래 기능별 폴더 + `[티어 1]`/`[티어 2]` 구분

---

## 1. 시스템 개요

### 핵심 컨셉
- 매 층마다 절차적으로 새 맵 생성
- 시드 기반 결정론적 생성 (동일 시드 = 동일 맵)
- Safe Room이 항상 시작점, Exit Room이 항상 출구
- Floor Feeling이 층별 테마 결정 (50% 확률 발동)
- **보스층(5/10/15)은 절차생성 제외** — 사전 제작 고정 맵 로드 (아래 1.1)

### 1.1 보스층 예외 (중요)
5/10/15층은 아래 3단계 파이프라인을 **타지 않는다.** `MapGenerator`가 depth를 보고 보스층이면 절차생성을 스킵하고 `BossRoomLoader`로 분기하여 고정 프리팹/레이아웃을 로드한다. (GDD 5.1 / 6.2 — Boss 방 = 단일 거대 방, 사전 제작)

```csharp
// MapGenerator.Generate() 진입부 의사코드
if (IsBossDepth(depth)) {           // depth == 5 || 10 || 15
    return bossRoomLoader.Load(depth);
}
// 그 외 층만 Initialize → Build → Paint → Spawn
```

### 1.2 보스 prefab 규격

```
Root (GameObject)
├── Floor       (Tilemap)        — 바닥 타일
├── Wall        (Tilemap)        — 벽 타일
├── Hazard      (Tilemap, 옵션)   — 위험 바닥
└── SafeAnchor  (빈 Transform)   — 플레이어 스폰 셀 1개
```

- 자식 이름은 정확히 `Floor` / `Wall` / `Hazard` / `SafeAnchor` (BossRoomLoader 가 이름으로 탐색).
- `BossRoomLoader` 는 Tilemap **데이터만 복사** 하고 prefab 인스턴스는 destroy. 그래서 prefab Root 에 GridExit/보스 게임오브젝트를 박아도 같이 사라짐 — 보스/Exit 인스턴스화는 별도 시스템(전투 종료 트리거 등) 에서 처리할 것.
- `SafeAnchor` 는 보스층의 플레이어 스폰 지점 한 셀을 가리킨다. `BossRoomLoader` 가 그 셀로 `RoomData(type=Safe).floorCells = [anchor cell]` 을 추가 생성하여 반환. `FloorTransitionController` 가 그 셀로 플레이어 텔레포트.

#### Safe 영역을 "단일 셀" 로 두는 이유 (출품 빌드 결정)

보스층 한정으로는 영역 분리 대신 **앵커 1셀 = Safe RoomData 1셀** 방식을 택한다.

- 보스층은 보스 처치 흐름이 단순(Spawn 즉시 교전) — Safe 영역 안에서 적 회피 등 *영역* 의미가 거의 없음.
- 일반 층의 Safe Room 정식 동작(GDD 11.1: 한 번 나가면 재입장 불가) 은 문 시스템 도입 시 같이 처리되므로, 보스층도 그때 **flood fill 영역 인식 방식** 으로 마이그레이션 가능.
- 출품 빌드까지 문 시스템 미도입 결정 — 상세는 `BACKLOG.md` "문 시스템" 항목.

### 영감
Shattered Pixel Dungeon v0.6.0+ "room-first" 방식.
**SPD 코드는 절대 가져오지 말 것** (GPLv3). 디자인 원리만 차용.

---

## 2. 3단계 파이프라인 (일반 층 전용 — 보스층 제외)

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
- Floor Feeling 적용 (Hazard 바닥 / 함정 비율 / 시야 조정)
- 출력: 완성된 floorTilemap + wallTilemap

### 2.4 Spawn 단계
- `EnemySpawner`가 방 타입별 몬스터 배치
- `ItemSpawner`가 아이템 배치
- Safe Room은 스폰 제외

---

## 3. 핵심 클래스 명세

### 3.1 Tier 1 (코어 — `Map/`, `Data/`)

#### `RoomData`
```csharp
// Map/RoomData.cs
public class RoomData {
    public RoomType type;                 // Safe / Normal / Special / Secret / Exit / Boss
    public RectInt bounds;                 // 방 위치·크기
    public SizeCategory sizeCategory;      // Normal / Large / Giant
    public List<Vector2Int> floorCells;    // 방이 점유하는 격자 셀 목록
    public List<Vector2Int> doorCells;     // 입구 셀 (스폰 제외용)
    public List<RoomData> connections;     // 연결된 방
    public int distanceFromSafe;           // Safe Room으로부터 그래프 거리

    // 빌더가 채움
}

public enum RoomType { Safe, Normal, Special, Secret, Exit, Boss }
public enum SizeCategory { Normal, Large, Giant }
```

#### `ILayoutBuilder`
```csharp
// Map/ILayoutBuilder.cs
public interface ILayoutBuilder {
    List<RoomData> Build(List<RoomData> rooms, DungeonRandom rng);
}
```

#### `IRegionPainter`
```csharp
// Map/IRegionPainter.cs
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
// Map/FloorFeeling.cs
public abstract class FloorFeeling : ScriptableObject {  // 옵션 B: SO 자식
    public abstract string Id { get; }
    public abstract void ApplyToInit(List<RoomData> rooms);
    public abstract void ApplyToSpawn(IEnemySpawner spawner);
    // ApplyToPaint 제거 — Painter가 Paint() 인자로 받은 feeling을 직접 읽음 (Phase 3 Q1 결정 A)
}
```

#### `BossRoomLoader`
```csharp
// Map/BossRoomLoader.cs
public class BossRoomLoader {
    // depth(5/10/15)에 대응하는 고정 맵 프리팹/레이아웃을 로드.
    // 절차생성 파이프라인을 타지 않음. 03.Prefab의 보스층 프리팹 참조.
    public GeneratedMap Load(int depth);
}
```

#### `DungeonRandom`
```csharp
// Map/DungeonRandom.cs
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

### 3.2 Tier 2 (구현체 — `Builders/`, `Painters/`, `Feelings/`)

#### `LoopBuilder` (`Builders/`)
- 주요 방을 루프 형태로 배치
- entrance(Safe)와 exit를 루프 반대편에 배치
- 나머지 방은 브랜치로 연결
- 방 간 직선/L자 복도 추가

#### `DefaultPainter` (`Painters/`)
- 기본 floor + wall 타일 배치
- RuleTile로 벽 자동 연결
- doorCells에 문 타일

#### Feeling 3종 (`Feelings/`)
- `HazardFeeling`, `TrapsFeeling`, `DarkFeeling`
- 각 Feeling이 자체적으로 페인터/스폰러를 어떻게 변경할지 결정
- 새 Feeling 추가 = 클래스 1개 + 풀 등록 (단, GDD 기준 3종 고정. 추가 X)

---

## 4. 방 타입별 규칙

| 타입 | 수량 | 핵심 규칙 |
|------|------|-----------|
| Safe | 1 (필수) | 시작점, 타이머 정지, 몬스터 진입 불가, 입구 1개. 한 번 나가면 재입장 불가 (GDD 11.1) |
| Normal | 가변 | 일반 전투, 크기별 몬스터 수 차등 |
| Special | 0~1 | **열쇠로 열고 입장**(여는 데 1초), 종류별 고유 보상 (GDD 6.2.1) |
| Secret | 0~1 | 숨겨진 문(열쇠 불필요), 발견은 탐색 확률 기반 (GDD 6.2.2 / 8.6) |
| Exit | 1 (필수) | Safe Room과 최대 그래프 거리 배치 |
| Boss | 5/10/15만 | **절차생성 제외, 고정 맵.** 단일 거대 방 |

### 4.1 Special 방 종류 (GDD 6.2.1 — 랜덤 1개 등장, 열쇠 필요)
랜덤 상자 방 / 몬스터 방(고대 골렘) / 반지 상자 방 / 골드 상자 방 / 스크롤 방

### 4.2 Secret 방 종류 (GDD 6.2.2 — 랜덤 1개 등장, 열쇠 불필요)
포션 방 / 장비 상자 방 / 골드 방 / 인챈트 방

### 방 크기 카테고리
| 카테고리 | 크기 범위 |
|---------|-----------|
| Normal | 5×5 ~ 7×7 |
| Large | 8×8 ~ 12×12 |
| Giant | 12×12 ~ 16×16 |

---

## 5. Floor Feeling 명세 (GDD 6.3 — 3종 고정)

### 5.1 발동 규칙
- 50% 확률로 발동
- 발동 시 풀에서 1개 랜덤 선택
- `DungeonRandom`만 사용 (UnityEngine.Random 금지)
- 영역별 가중치는 GDD 5.2 참조 (Area 1: 40/40/20, Area 2: 40/30/30, Area 3: 30/30/40)

### 5.2 Feeling 풀

| Feeling | 영향 | 구현 메모 |
|---------|------|-----------|
| Hazard | 위험 바닥 생성. **영역별 리스킨**: 하수구=오염수, 던전=함정바닥, 지옥=용암. 해당 바닥은 이동 시간 2배 + 올라가 있는 동안 데미지 | area 파라미터로 타일/연출만 분기. 통과는 가능(통과 불가 아님). 부유 포션·공허 발걸음 마법으로 무시 (GDD 8.5.1 / 9.16) |
| Traps | 함정 추가 배치 (고위험 경로) + 몬스터 수 -1 | Phase 4 spawn 의존 |
| Dark | 기본 시야 반경 -2 | `VisionSystem.visionRadius` 인자 1개. 횃불·심안 포션·지도 스크롤 가치↑ |

> **주의**: Water/Grass/Chasm은 옛 모델이다. GDD엔 없다. Hazard 하나가 영역별로 리스킨되어 물/용암을 표현하므로 별도 Feeling 클래스를 만들지 말 것.

### 5.3 확장 방법

새 Feeling 추가 시(원칙적으로 출품 범위에선 추가 X):
1. `Feelings/MyNewFeeling.cs` 생성
2. `FloorFeeling` 상속 + Apply 메서드 구현
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
Initialize / Build / Paint / Spawn 순차 호출 (보스층은 스킵)
```

### 6.2 결정론 보장 규칙
- **모든 무작위 호출은 `DungeonRandom` 통일**
- `UnityEngine.Random`, `System.Random` 직접 호출 금지
- 호출 순서가 곧 결과를 결정 — 리팩토링 시 호출 순서 보존
- 동일 시드 + 동일 depth → 항상 동일 맵
- 시드 시스템은 챌린지/시드 챌린지(GDD 13.4)의 기반이므로 결정론은 필수

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

### 7.2 Hazard 바닥 처리 (통과 가능 + 비용 부과)
Hazard 타일은 **통과 불가가 아니다.** floorTilemap 위에 올라가지만 진입 시 페널티를 받는다:
- 이동 시간 2배
- 올라가 있는 동안 데미지
- 이동/데미지 판정은 GridSystem 또는 이동 처리부에서 "이 셀이 Hazard 인가"를 조회. Hazard 셀 좌표 집합을 맵 데이터에 보관(예: `HashSet<Vector2Int> hazardCells`).
- 부유 상태/공허 발걸음 마법이면 페널티 무시 (상태 조회).

### 7.3 RuleTile 사용
- 벽은 RuleTile로 자동 연결
- 비트마스크 직접 구현 X
- 지역별로 다른 RuleTile 에셋 분리 (SewerWall.asset, DungeonWall.asset, HellWall.asset)

### 7.4 Fog 시스템
- 페인터는 fogTilemap 건드리지 않음
- FogRenderer가 GetAllCells() 호출하여 자동 갱신

---

## 8. 마법사 스켈레톤 배치 제약 (중요)

GDD 10장의 **마법사 스켈레톤**은 4방향 직선 LOS 텔레그래프 후 마법을 발사한다(원거리). 직선 사거리가 안 나오는 셀에 배치되면 공격이 무의미해지므로, 스폰 시 다음 조건 검사 필수:

```csharp
bool CanPlaceRangedCaster(Vector2Int cell) {
    // 4방향 중 최소 1방향이 3칸 이상 개방되어야 함
    foreach (var dir in fourDirections) {
        int openCells = CountOpenCells(cell, dir, maxRange: enemy.visionRange);
        if (openCells >= 3) return true;
    }
    return false;
}
```

이 검사 안 하면 마법사 스켈레톤이 벽 보고 멍 때림. 통과 못하면 다른 셀 재시도, N회 실패 시 근접 스켈레톤으로 강등.

---

## 9. 작업 순서 (Claude Code에 시킬 순서)

### Phase 1 — 코어 인프라 (Tier 1)
1. `RoomData`, `RoomType`(Boss 포함), `SizeCategory` 정의
2. `DungeonRandom` 구현
3. `ILayoutBuilder`, `IRegionPainter` 인터페이스
4. `FloorFeeling` 추상 클래스
5. `MapGenerator` 메인 진입점 (보스층 분기 + Initialize → Build → Paint 호출)
6. `BossRoomLoader` 스텁 (고정 맵 로드만, 내용은 LSH 협업)

### Phase 2 — 최소 동작 (Tier 2)
7. `LoopBuilder` 1종
8. `DefaultPainter` 1종
9. Safe Room / Exit Room 자동 배치
10. 통합 테스트: 시드 입력 → 맵 생성 → Tilemap 출력

### Phase 3 — Feeling 시스템 (3종)
11. `HazardFeeling` (테스트용 첫 1종 — Hazard 셀 집합 + 페인터 분기)
12. Feeling 풀 SO 등록 구조
13. `DarkFeeling` (VisionSystem 인자 1개)
14. `TrapsFeeling` (Phase 4 spawn 의존 — 후순위)

### Phase 4 — 스폰 통합
15. `EnemySpawner` 방 타입별 룰
16. 마법사 스켈레톤 LOS 검증 통과
17. 거리 기반 난이도 차등

### Phase 5 — 다양성/폴리싱
18. `FigureEightBuilder` 추가
19. 영역별 페인터 (SewerPainter / DungeonPainter / HellPainter)
20. Special/Secret 방 종류 (GDD 6.2.1 / 6.2.2 의 실제 종류)

---

## 10. 테스트 체크리스트

각 Phase 완료 후 확인:

- [ ] 동일 시드 2회 생성 → 완전히 동일한 맵 (Tilemap diff = 0)
- [ ] 100개 시드 생성 → 모두 entrance ~ exit 연결됨 (BFS 검증)
- [ ] 보스층(5/10/15)은 절차생성 스킵, 고정 맵 로드
- [ ] Safe Room에 몬스터 절대 안 들어감
- [ ] 입구 인접 셀에 몬스터 스폰 0건
- [ ] 새 Feeling 추가가 기존 코드 수정 없이 동작
- [ ] 새 Builder/Painter 추가가 기존 코드 수정 없이 동작

---

## 11. 금지 사항

- ❌ `UnityEngine.Random` 또는 `System.Random` 직접 호출 (반드시 `DungeonRandom`)
- ❌ Builder/Painter 안에 게임 로직 (몬스터 스폰 등) 섞기
- ❌ Tilemap 직접 참조 (GridSystem 경유)
- ❌ 보스층을 절차생성 파이프라인에 태우기 (반드시 BossRoomLoader 분기)
- ❌ Water/Grass/Chasm Feeling 만들기 (옛 모델 — Hazard/Traps/Dark 3종만)
- ❌ SPD 코드 복붙 (GPLv3, 디자인만 참고)
- ❌ Phase를 건너뛰고 한 번에 모든 Feeling 구현
- ❌ 한 클래스가 여러 책임 (예: Builder가 Painter 역할까지)
- ❌ 매직 넘버 (50%, 3칸 등은 GameConfig 또는 const)
