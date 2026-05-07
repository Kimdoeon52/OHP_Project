# PHASE 3~5 구현 계획

> Phase 1 (코어 인프라) / Phase 2 (LoopBuilder + DefaultPainter + 통합 테스트) 완료 후 진행할 단계별 계획.
> 실제 코딩은 각 Phase 시작 시점에 본 문서를 참조하여 점진적으로 수행.
> 본 문서는 작업 시작 전 점검표 + 설계 결정 기록용 — 실제 코드 작성 시 추가 변경/축소 가능.

---

## 공통 사항

- **디렉토리 위치**: `Assets/KDU/01.Scripts/{Map,Builders,Painters,Feelings,Spawning}` (CLAUDE.md 디렉토리 구조 참조)
- **티어 구분**: Builder/Painter/Feeling/Spawner = 티어 2 (구현체), 인터페이스 / 풀 SO / GameConfig 변경 = 티어 1
- **결정론**: 모든 Phase에서 무작위 호출은 `DungeonRandom`만 사용
- **Feeling 풀 / Builder 풀 / Painter 풀 = 모두 ScriptableObject** — 인스펙터 드래그-드롭으로 추가 가능해야 함

---

## Phase 3 — Feeling 시스템

### 작업 항목

1. **`FloorFeelingPool` SO** (`_Core/Data/FloorFeelingPool.cs`)
   - `List<FloorFeeling>` 보관 (실제 인스턴스는 자식 SO 또는 plain class 인스턴스)
   - `FloorFeeling Pick(DungeonRandom rng, float triggerChance)` — 50% 확률로 발동, 발동 시 1개 선택
2. **`WaterFeeling`** (`_Game/Feelings/WaterFeeling.cs`) — 테스트용 1종 먼저
3. **`MapGenerator` 통합** — Generate() 안에서 `pool?.Pick(rng, 0.5f)` 호출, 결과를 builder/painter에 전달
4. **나머지 4종** — `GrassFeeling`, `ChasmFeeling`, `TrapsFeeling`, `DarkFeeling`

### 핵심 설계 결정 (Phase 3 시작 시 확정)

#### Q1. `FloorFeeling` 의 Apply* 메서드는 무엇을 변경하나?
- `ApplyToInit(rooms)` — 방 리스트에 영향. 후보:
  - 방 추가 (Traps Feeling: 함정 방 1개 강제 추가)
  - 방 크기 카테고리 변경 (Giant 방 추가 등)
  - 방 메타데이터 부여 (Feeling별 플래그)
- `ApplyToPaint(painter)` — 페인터 설정 변경. **현 인터페이스의 모호점**:
  - Painter는 이미 `Paint(rooms, ..., feeling, rng)` 호출 시 `feeling` 인자를 직접 받음
  - 즉 Painter가 `feeling.GetWaterDensity()` 등을 직접 호출하는 게 더 깔끔
  - **결정 옵션 A (권장)**: `ApplyToPaint` 제거, Painter가 Feeling 을 직접 읽음
  - **결정 옵션 B**: `ApplyToPaint` 유지, painter 내부 SerializeField 를 변경 (구현 복잡, 디버그 어려움)
- `ApplyToSpawn(spawner)` — Phase 4 EnemySpawner 통합 시점에 정의

#### Q2. Feeling별 효과를 페인터가 어떻게 표현?
- `DefaultPainter`에 `floorTile`/`wallTile` 외에 `waterTile`/`grassTile`/`chasmTile` 추가? 또는 페인터 별도?
- **권장**: `DefaultPainter`가 Feeling 인자 보고 비율 계산 → 부분적 타일 교체 (방 면적의 X% 를 waterTile 로 덮음)
- Chasm 만 게임플레이 영향 큼 (통과 불가) → `GridSystem.IsWalkable` 에서 chasm 타일을 wall 처럼 취급할지 결정 필요

#### Q3. 풀 등록 메커니즘
- 옵션 A: `FloorFeelingPool`에 `List<FloorFeeling>` (직렬화 필요 → SerializeReference 또는 SO 자식)
- 옵션 B: 각 Feeling 자체를 `[CreateAssetMenu] ScriptableObject`로 만들고 풀이 `List<FeelingSO>` 보관
- **권장 B** — Phase 2 Builder/Painter 와 동일 패턴, 인스펙터 드래그-드롭, 자식별 파라미터 인스펙터에서 튜닝 가능

### 의존성 / 차순위

- **Water/Dark는 Phase 3 우선**: 가장 단순 (Water = 부분 타일 교체, Dark = `VisionSystem.visionRadius` 인자 1개)
- **Grass는 VisionSystem 확장 필요** (시야 차단 셀 개념) — VisionSystem 코드 한 번 손봐야 함
- **Chasm은 GridSystem.IsWalkable 확장 필요** (chasm = 통과 불가 floor 변형)
- **Traps는 Phase 4 의존** (몬스터 -1, 함정 30%↑ — 둘 다 spawn 단계 영향)
  - → Traps Feeling 은 Phase 4 통합 후 마무리해도 됨

### 검증 기준

- [ ] WaterFeeling만 풀에 등록한 상태에서 50% 확률 동작 (`runSeed` 100개 중 ~50개 발동)
- [ ] 같은 (runSeed, depth) → 항상 같은 Feeling 결과 (결정론)
- [ ] 새 Feeling 클래스 추가 + 풀에 SO 등록만으로 동작 (기존 코드 0줄 수정)
- [ ] Water 발동 층에서 floorTilemap 의 ~40% 가 water 타일

### Phase 3 출력물

| 파일 | 위치 | 비고 |
|------|------|------|
| `FloorFeelingPool.cs` | `_Core/Data/` | SO + .asset 인스턴스 |
| `WaterFeeling.cs` | `_Game/Feelings/` | SO (옵션 B) |
| `GrassFeeling.cs` | `_Game/Feelings/` | VisionSystem 확장 함께 |
| `ChasmFeeling.cs` | `_Game/Feelings/` | GridSystem.IsWalkable 확장 함께 |
| `TrapsFeeling.cs` | `_Game/Feelings/` | Phase 4 의존, 후순위 |
| `DarkFeeling.cs` | `_Game/Feelings/` | VisionSystem.visionRadius 인자 |
| `MapGenerator.cs` | `_Core/Map/` | **수정** — Feeling 풀 통합 |
| `DefaultPainter.cs` | `_Game/Painters/` | **수정** — Feeling 인자 활용 |
| `FloorFeeling.cs` | `_Core/Map/` | **수정** — Q1 결정에 따라 Apply* 시그니처 정리 |

---

## Phase 4 — 스폰 통합

### 작업 항목

1. **`IEnemySpawner` 인터페이스 확정** (`_Core/Map/`) — Phase 1 placeholder 채움
2. **`EnemySpawner`** (`_Game/Spawning/`) — `IEnemySpawner` 구현
3. **`RoomSpawnRule` 추상 + 타입별 구현체** (`_Game/Spawning/Rules/`)
4. **`EnemySpawnPool` SO** (`_Core/Data/`) — depth 별 적 풀
5. **`GridSystem` LOS 헬퍼** — Archer 배치 검증용
6. **거리 기반 난이도 차등** — `distanceFromSafe` 활용

### 핵심 설계 결정

#### Q1. `IEnemySpawner` 시그니처
```csharp
public interface IEnemySpawner {
    void Spawn(List<RoomData> rooms, int depth, DungeonRandom rng);
}
```
- 단순 1메서드 진입점. Feeling은 `ApplyToSpawn(IEnemySpawner)` 로 사전 변경 가능 (몬스터 수 -1 등).
- 풀/규칙은 EnemySpawner 내부 SerializeField.

#### Q2. `RoomSpawnRule` 책임
```csharp
public abstract class RoomSpawnRule : ScriptableObject {
    public abstract void SpawnFor(RoomData room, int depth, EnemySpawnPool pool, DungeonRandom rng);
}
```
- 타입별 구현:
  - `SafeRoomRule` — 항상 0마리 (no-op)
  - `NormalRoomRule` — 방 크기 카테고리에 비례, distanceFromSafe 가중치
  - `ExitRoomRule` — 가벼움 (다음 층 압박)
  - `SpecialRoomRule` — Phase 5 (보상 방)
- EnemySpawner 가 RoomType → Rule 매핑 (Dictionary 또는 SO 리스트)

#### Q3. Archer LOS 검증 방법
- `GridSystem.CountOpenCellsInDirection(Vector2Int from, Vector2Int dir, int maxRange)` 헬퍼 추가
- EnemySpawner 가 Archer 후보 셀마다 4방향 검증, 1방향이라도 3칸 ≥ 통과해야 배치
- 통과 못하면 다른 셀 재시도, N회 실패 시 Archer 대신 Tracker 강등

#### Q4. 거리 기반 난이도
- 단순 식: `monsterTier = floor(distanceFromSafe / 2)` 또는 `Lerp(weak, strong, distance / maxDistance)`
- `EnemySpawnPool` 에 `tier` 필드 두고 적 데이터마다 minTier 설정
- 가까운 방: minTier 0 적만 (Tracker)
- 먼 방: minTier 2 적까지 (Archer, 보스급)

#### Q5. 입구 인접 셀 제외
- `RoomData.doorCells` 는 방 경계 한 칸 바깥 (Phase 2 결정)
- 스폰 제외: 각 doorCell 의 4방향 인접 셀까지 (안전 완충)
- Safe Room 경계 cell 도 제외 (Safe Room 자체는 RoomType.Safe 로 처리되지만, 인접 Normal 방의 입구도 제외)

### 의존성

- **Phase 3 ApplyToSpawn 활성화** — Feeling 이 EnemySpawner 의 spawnDensity 등을 변경 (TrapsFeeling: 몬스터 -1)
- **기존 적 prefab 인스턴스화 메커니즘** — `GridEnemyTracker`, `GridEnemyArcher` 가 prefab 인지 SO 인지 확인 필요. 현재 Resources.Load 또는 직접 참조 둘 중 어느 방식인지 점검 후 통일

### 검증 기준

- [ ] Safe Room 에 몬스터 0마리 (100 시드 모두)
- [ ] doorCell 인접 4방향 셀에 몬스터 0건
- [ ] Archer 가 배치된 셀에서 4방향 LOS 카운트 시 최소 1방향 ≥3 셀 (실패 0건)
- [ ] distanceFromSafe ≥ 4 인 방에 강한 몬스터 출현 비율 ≥ 50%
- [ ] Feeling 통합: TrapsFeeling 발동 층에서 몬스터 수 평소 대비 -1

### Phase 4 출력물

| 파일 | 위치 | 비고 |
|------|------|------|
| `IEnemySpawner.cs` | `_Core/Map/` | **수정** — placeholder 채움 |
| `EnemySpawner.cs` | `_Game/Spawning/` | 메인 진입점 |
| `RoomSpawnRule.cs` | `_Game/Spawning/Rules/` | 추상 SO |
| `SafeRoomRule.cs` | `_Game/Spawning/Rules/` | no-op |
| `NormalRoomRule.cs` | `_Game/Spawning/Rules/` | 크기/거리 가중치 |
| `ExitRoomRule.cs` | `_Game/Spawning/Rules/` | 가볍게 |
| `EnemySpawnPool.cs` | `_Core/Data/` | depth × tier 매트릭스 SO |
| `GridSystem.cs` | `_Core/Grid/` | **수정** — LOS 헬퍼 추가 |
| `MapGenerator.cs` | `_Core/Map/` | **수정** — Spawn 단계 호출 |
| `TrapsFeeling.cs` | `_Game/Feelings/` | Phase 3 미완료 시 여기서 마무리 |
| (선택) `ItemSpawner.cs` | `_Game/Spawning/` | GDD 의 아이템 배치 — 후순위 |

---

## Phase 5 — 다양성 / 폴리싱

### 작업 항목

1. **`FigureEightBuilder`** (`_Game/Builders/`) — 8자형 루프 (두 원이 한 방을 공유)
2. **지역별 페인터** — 컨셉 1 완성도 우선, 컨셉 2/3 는 placeholder
3. **`Special Room` 종류 2~3개** — 예: StatueRoom, LibraryRoom
4. **`Secret Room` 1종** — 숨겨진 입구 + 발견 메커니즘
5. **빌더 무작위 선택** — MapGenerator 가 BuilderPool SO 에서 선택

### 핵심 설계 결정

#### Q1. FigureEightBuilder 와 LoopBuilder 의 공통 코드
- LoopBuilder 의 `AssignSize`, `PositionInLoop`, `ConnectWithTunnel`, `LShapePath` 등은 8자형에서도 동일
- 옵션 A: `LoopBuilder` 를 base class 로 보호된 헬퍼 노출 (상속) — 하지만 SO 상속은 Unity 에서 미묘함
- 옵션 B: `BuilderUtils` 정적 클래스로 헬퍼 추출 (`_Core/Map/`) — 상속 회피, 깔끔
- **권장 B**

#### Q2. 지역별 페인터 — 신규 클래스 vs SO 인스턴스 다중 생성
- DefaultPainter 가 충분히 일반적이면 → SO 인스턴스만 추가 (`SewerPainter.asset`, `ForestPainter.asset` — 다른 RuleTile 할당)
- 컨셉마다 페인트 알고리즘이 달라야 한다면 → 신규 클래스 (`SewerPainter.cs` 등)
- **CLAUDE.md 스코프 락 — 컨셉 1만 완성도 있게.** Phase 5 시점에 컨셉 1 페인터 신규 1개 만들고 컨셉 2/3는 SO 인스턴스로 가벼이.

#### Q3. Special Room 처리
- 현재 RoomType.Special 은 enum 에 있지만 별도 처리 X
- Phase 5: SpecialRoom 마다 인테리어 다름 (조각상, 책장 배치)
- 옵션 A: Painter 가 `RoomType.Special` 인지 보고 인테리어 페인트
- 옵션 B: 별도 `SpecialRoomDecorator` SO 풀 (StatueDecorator, LibraryDecorator) → DefaultPainter 가 호출
- **권장 B** — 새 Special 추가 = Decorator 1개 + 풀 등록. 확장성↑.

#### Q4. Secret Room 메커니즘
- 입구가 wallTilemap 으로 위장 (벽처럼 그려짐)
- 발견 트리거:
  - 지도 스크롤 사용 → 모든 Secret 표시
  - 벽 이동 시도 → 확률적 발견 (ItemUse / 직접 접촉)
- Phase 5 시점에는 "지도 스크롤 사용" 만 구현, "벽 이동 시도" 는 BACKLOG.md
- 데이터: `RoomData.type == Secret` 인 방의 doorCells 에 wallTile 페인트

#### Q5. 빌더/페인터 풀
- `BuilderPool` SO — `List<ScriptableObject>` (모든 ILayoutBuilder SO)
- `PainterPool` SO — `Dictionary<int depthRange, IRegionPainter>` 또는 `List<DepthPainterPair>`
- MapGenerator 가 풀에서 무작위 선택 (DungeonRandom)

### 검증 기준

- [ ] MapGenerator 가 BuilderPool 에서 빌더 무작위 선택 (시드별 다른 빌더)
- [ ] depth 1~5 / 6~10 / 11~15 등 깊이 구간별 다른 페인터 사용
- [ ] Special Room 이 평균 1~2개/층 등장
- [ ] Secret Room 이 ~30% 층에 등장 (시드 100개 기준)
- [ ] 8자 빌더로도 entrance↔exit 100 시드 연결성 통과

### Phase 5 출력물

| 파일 | 위치 | 비고 |
|------|------|------|
| `FigureEightBuilder.cs` | `_Game/Builders/` | 8자 빌더 |
| `BuilderUtils.cs` | `_Core/Map/` | LoopBuilder/FigureEight 공통 헬퍼 |
| `SewerPainter.cs` | `_Game/Painters/` | 컨셉 1 페인터 |
| `(다른 컨셉 .asset)` | (Project) | DefaultPainter SO 인스턴스 다중 |
| `SpecialRoomDecorator.cs` | `_Game/Painters/Decorators/` | 추상 SO |
| `StatueDecorator.cs`, `LibraryDecorator.cs` | `_Game/Painters/Decorators/` | 구현체 |
| `SecretRoomMechanic.cs` | `_Game/Rooms/` | 위장 입구 페인트 |
| `BuilderPool.cs`, `PainterPool.cs` | `_Core/Data/` | SO 풀 |
| `MapGenerator.cs` | `_Core/Map/` | **수정** — 풀에서 빌더/페인터 선택 |

### 스코프 주의 (CLAUDE.md 스코프 락)

- "보스 3종 다 만들자" 충동 → BACKLOG.md (보스 1종만 우선, Phase 5 외)
- "추가 빌더 (Random, Maze 등)" → BACKLOG.md
- "다국어 지원, 저장/로드" → 후순위, Phase 5 범위 외
- 컨셉 2/3 는 SO 인스턴스만 가벼이 — 페인트 알고리즘 신규 클래스 만들지 말 것

---

## 통합 검증 (Phase 5 완료 시점)

본 개발 진입 점검표 (CLAUDE.md 참조) 6개 항목 모두 "예" 가 나와야 함:

- [ ] 코어 시스템 이전 완료 (`KDU/01.Scripts/{Grid, Time, Vision, Turn, Events}`)
- [ ] 새 적 종류 추가가 SO 인스턴스 + 인스펙터 설정만으로 가능
- [ ] 새 함정 종류 추가가 같은 방식으로 가능
- [ ] 새 Feeling 추가가 클래스 1개 + 풀 등록만으로 가능
- [ ] 새 빌더/페인터 추가가 같은 방식으로 가능
- [ ] 아이템/저주가 GameEvents 구독으로 동작

추가:
- [ ] 100 시드 × depth 1~15 모든 조합에서 entrance↔exit 연결성 PASS
- [ ] 결정론: 동일 (runSeed, depth) → 동일 맵 / 동일 Feeling / 동일 스폰
- [ ] Archer 0 멍 (모든 Archer 가 LOS 검증 통과 셀에 배치)

---

## 작업 순서 (권장)

1. **Phase 3 시작 전**: Q1~Q3 의사결정 → 본 문서 업데이트
2. **Phase 3**:
   - Step 1: FloorFeeling 시그니처 확정 (Apply* 정리)
   - Step 2: WaterFeeling + FloorFeelingPool + MapGenerator 통합 (가장 단순한 1개로 파이프라인 완성)
   - Step 3: DarkFeeling (VisionSystem 인자 1개)
   - Step 4: GrassFeeling (VisionSystem 확장 필요 시)
   - Step 5: ChasmFeeling (GridSystem 확장 필요 시)
   - Step 6: TrapsFeeling — Phase 4 진행 후 마무리
3. **Phase 4**:
   - Step 1: IEnemySpawner 시그니처 확정 + EnemySpawner 골격
   - Step 2: NormalRoomRule + SafeRoomRule (가장 기본 2개)
   - Step 3: GridSystem LOS 헬퍼 + Archer 검증
   - Step 4: EnemySpawnPool + 거리 기반 차등
   - Step 5: TrapsFeeling 마무리 + ApplyToSpawn 활성화
4. **Phase 5**:
   - Step 1: BuilderUtils 추출 (LoopBuilder 리팩토링)
   - Step 2: FigureEightBuilder
   - Step 3: BuilderPool + MapGenerator 빌더 선택
   - Step 4: 컨셉 1 페인터 (SewerPainter)
   - Step 5: PainterPool + depth 기반 선택
   - Step 6: SpecialRoomDecorator + 1~2종
   - Step 7: SecretRoom 위장 입구

각 Step 완료 후 통합 테스트 (`MapGenerationTester` 스트레스 테스트) 통과 확인.
