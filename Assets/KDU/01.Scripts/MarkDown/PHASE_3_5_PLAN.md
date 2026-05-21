# PHASE 3~5 구현 계획

> Phase 1 (코어 인프라) / Phase 2 (LoopBuilder + DefaultPainter + 통합 테스트) 완료 후 진행할 단계별 계획.
> 실제 코딩은 각 Phase 시작 시점에 본 문서를 참조하여 점진적으로 수행.
> 본 문서는 작업 시작 전 점검표 + 설계 결정 기록용 — 실제 코드 작성 시 추가 변경/축소 가능.
> **디자인 수치·규칙은 GDD가 원본. 충돌 시 GDD를 따른다.**

---

## 공통 사항

- **디렉토리 위치**: `Assets/KDU/01.Scripts/{Map,Builders,Painters,Feelings,Spawning}` — 기능별 폴더 + `[티어 1]`/`[티어 2]` 태그 (CLAUDE.md 디렉토리 구조 참조)
- **티어 구분**: Builder/Painter/Feeling/Spawner = 티어 2 (구현체), 인터페이스 / 풀 SO / GameConfig 변경 = 티어 1
- **결정론**: 모든 Phase에서 무작위 호출은 `DungeonRandom`만 사용
- **Feeling 풀 / Builder 풀 / Painter 풀 = 모두 ScriptableObject** — 인스펙터 드래그-드롭으로 추가 가능해야 함
- **Feeling은 3종 고정** (Hazard/Traps/Dark) — GDD 6.3. 추가 X.
- **보스층(5/10/15)은 절차생성 제외** — 고정 맵. 본 Phase 작업 범위는 일반 층 생성 + 보스 고정맵 로더 연동까지.

---

## Phase 3 — Feeling 시스템 (3종)

### 작업 항목

1. **`FloorFeelingPool` SO** (`Data/FloorFeelingPool.cs`)
   - `List<FloorFeeling>` 보관 (SO 자식 인스턴스)
   - `FloorFeeling Pick(DungeonRandom rng, float triggerChance)` — 50% 확률로 발동, 발동 시 1개 선택
   - 영역별 가중치 적용 (GDD 5.2: Area 1 40/40/20, Area 2 40/30/30, Area 3 30/30/40)
2. **`HazardFeeling`** (`Feelings/HazardFeeling.cs`) — 테스트용 1종 먼저. area 파라미터로 오염수/함정바닥/용암 분기
3. **`MapGenerator` 통합** — Generate() 안에서 `pool?.Pick(rng, 0.5f)` 호출, 결과를 painter/spawner에 전달
4. **나머지 2종** — `DarkFeeling`, `TrapsFeeling`

### 핵심 설계 결정 (Phase 3 시작 시 확정)

#### Q1. `FloorFeeling` 의 Apply* 메서드는 무엇을 변경하나?
- `ApplyToInit(rooms)` — 방 리스트에 영향. 후보:
  - 방 추가 (Traps Feeling: 함정 방 1개 강제 추가)
  - 방 메타데이터 부여 (Feeling별 플래그)
- **`ApplyToPaint` 제거 (결정 A)**: Painter는 이미 `Paint(rooms, ..., feeling, rng)` 로 `feeling` 인자를 직접 받으므로, Painter가 `feeling`을 직접 읽는 게 깔끔. 별도 ApplyToPaint 불필요.
- `ApplyToSpawn(IEnemySpawner)` — Phase 4 EnemySpawner 통합 시점에 정의 (TrapsFeeling: 몬스터 -1)

#### Q2. Hazard 효과를 페인터/그리드가 어떻게 표현?
- **Hazard는 통과 가능 + 비용 부과** (통과 불가 아님 — 옛 Chasm 모델과 다름).
- 페인터: `DefaultPainter`가 feeling.area 보고 비율 계산 → 방 면적의 X%를 hazardTile(오염수/함정바닥/용암)로 덮음.
- 그리드/이동: Hazard 셀 좌표 집합(`HashSet<Vector2Int> hazardCells`)을 맵 데이터에 보관. 이동 처리부가 진입 셀이 Hazard면 **이동 시간 2배 + 데미지** 적용. 부유/공허 발걸음이면 무시. (GDD 6.3 / 8.5.1 / 9.16)
- → GridSystem.IsWalkable 확장은 **불필요** (Hazard는 walkable). 대신 이동 비용/데미지 훅이 필요.

#### Q3. 풀 등록 메커니즘
- 각 Feeling 을 `[CreateAssetMenu] ScriptableObject`로 만들고 풀이 `List<FloorFeeling>` 보관
- Phase 2 Builder/Painter 와 동일 패턴, 인스펙터 드래그-드롭, 자식별 파라미터 인스펙터에서 튜닝 가능

### 의존성 / 차순위

- **Hazard/Dark는 Phase 3 우선**: Dark = `VisionSystem.visionRadius` 인자 1개로 가장 단순. Hazard = hazardCells 집합 + 페인터 분기 + 이동 비용 훅.
- **Traps는 Phase 4 의존** (몬스터 -1, 함정 30%↑ — 둘 다 spawn 단계 영향)
  - → Traps Feeling 은 Phase 4 통합 후 마무리해도 됨
- (옛 계획의 Grass/Chasm 작업 항목은 삭제됨 — 3종 모델에 없음. VisionSystem 시야차단 셀 확장 / GridSystem 통과불가 확장 모두 불필요.)

### 검증 기준

- [ ] HazardFeeling만 풀에 등록한 상태에서 50% 확률 동작 (`runSeed` 100개 중 ~50개 발동)
- [ ] 같은 (runSeed, depth) → 항상 같은 Feeling 결과 (결정론)
- [ ] 새 Feeling 클래스 추가 + 풀에 SO 등록만으로 동작 (기존 코드 0줄 수정)
- [ ] Hazard 발동 층에서 floorTilemap 의 일정 비율이 hazard 타일, 진입 시 이동 2배 + 데미지 적용
- [ ] 부유 상태에서 Hazard 셀 진입 시 페널티 무시

### Phase 3 출력물

| 파일 | 위치 | 비고 |
|------|------|------|
| `FloorFeelingPool.cs` | `Data/` | SO + .asset 인스턴스, 영역 가중치 |
| `HazardFeeling.cs` | `Feelings/` | SO, area 분기 (오염수/함정바닥/용암) |
| `DarkFeeling.cs` | `Feelings/` | VisionSystem.visionRadius 인자 |
| `TrapsFeeling.cs` | `Feelings/` | Phase 4 의존, 후순위 |
| `MapGenerator.cs` | `Map/` | **수정** — Feeling 풀 통합 |
| `DefaultPainter.cs` | `Painters/` | **수정** — Feeling 인자 활용 (hazard 비율) |
| `FloorFeeling.cs` | `Map/` | **수정** — Q1 결정(ApplyToPaint 제거)에 따라 시그니처 정리 |
| 이동 처리부 | `Grid/` 또는 이동 담당 | **수정** — Hazard 셀 이동 비용/데미지 훅 |

---

## Phase 4 — 스폰 통합

### 작업 항목

1. **`IEnemySpawner` 인터페이스 확정** (`Map/`) — Phase 1 placeholder 채움
2. **`EnemySpawner`** (`Spawning/`) — `IEnemySpawner` 구현
3. **`RoomSpawnRule` 추상 + 타입별 구현체** (`Spawning/Rules/`)
4. **`EnemySpawnPool` SO** (`Data/`) — depth 별 적 풀 (GDD 10장 로스터)
5. **`GridSystem` LOS 헬퍼** — 마법사 스켈레톤 배치 검증용
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
  - `SpecialRoomRule` — Phase 5 (보상 방, 고대 골렘 등)
- EnemySpawner 가 RoomType → Rule 매핑 (Dictionary 또는 SO 리스트)

#### Q3. 마법사 스켈레톤 LOS 검증 방법 (GDD 10장 — 4방향 직선 LOS 텔레그래프)
- `GridSystem.CountOpenCellsInDirection(Vector2Int from, Vector2Int dir, int maxRange)` 헬퍼 추가
- EnemySpawner 가 마법사 스켈레톤 후보 셀마다 4방향 검증, 1방향이라도 3칸 ≥ 통과해야 배치
- 통과 못하면 다른 셀 재시도, N회 실패 시 근접 스켈레톤으로 강등

#### Q4. 거리 기반 난이도
- 단순 식: `monsterTier = floor(distanceFromSafe / 2)` 또는 `Lerp(weak, strong, distance / maxDistance)`
- `EnemySpawnPool` 에 `tier` 필드 두고 적 데이터마다 minTier 설정
- 가까운 방: 약한 적만 (역병 쥐/거미 등)
- 먼 방: 강한 적까지 (엘리트 — 뱀/트롤/불의 정령 등)

#### Q5. 입구 인접 셀 제외
- `RoomData.doorCells` 는 방 경계 한 칸 바깥 (Phase 2 결정)
- 스폰 제외: 각 doorCell 의 4방향 인접 셀까지 (안전 완충)
- Safe Room 경계 cell 도 제외 (Safe Room 자체는 RoomType.Safe 로 처리되지만, 인접 Normal 방의 입구도 제외)

### 의존성

- **Phase 3 ApplyToSpawn 활성화** — Feeling 이 EnemySpawner 의 spawnDensity 등을 변경 (TrapsFeeling: 몬스터 -1)
- **기존 적 prefab 인스턴스화 메커니즘** — GDD 10장 로스터의 적들이 prefab 인지 SO 인지 확인 필요. 현재 Resources.Load 또는 직접 참조 둘 중 어느 방식인지 점검 후 통일

### 검증 기준

- [ ] Safe Room 에 몬스터 0마리 (100 시드 모두)
- [ ] doorCell 인접 4방향 셀에 몬스터 0건
- [ ] 마법사 스켈레톤이 배치된 셀에서 4방향 LOS 카운트 시 최소 1방향 ≥3 셀 (실패 0건)
- [ ] distanceFromSafe ≥ 4 인 방에 강한 몬스터 출현 비율 ≥ 50%
- [ ] Feeling 통합: TrapsFeeling 발동 층에서 몬스터 수 평소 대비 -1

### Phase 4 출력물

| 파일 | 위치 | 비고 |
|------|------|------|
| `IEnemySpawner.cs` | `Map/` | **수정** — placeholder 채움 |
| `EnemySpawner.cs` | `Spawning/` | 메인 진입점 |
| `RoomSpawnRule.cs` | `Spawning/Rules/` | 추상 SO |
| `SafeRoomRule.cs` | `Spawning/Rules/` | no-op |
| `NormalRoomRule.cs` | `Spawning/Rules/` | 크기/거리 가중치 |
| `ExitRoomRule.cs` | `Spawning/Rules/` | 가볍게 |
| `EnemySpawnPool.cs` | `Data/` | depth × tier 매트릭스 SO (GDD 10장) |
| `GridSystem.cs` | `Grid/` | **수정** — LOS 헬퍼 추가 |
| `MapGenerator.cs` | `Map/` | **수정** — Spawn 단계 호출 |
| `TrapsFeeling.cs` | `Feelings/` | Phase 3 미완료 시 여기서 마무리 |
| (선택) `ItemSpawner.cs` | `Spawning/` | GDD 의 아이템 배치 — 후순위 |

---

## Phase 5 — 다양성 / 폴리싱

### 작업 항목

1. **`FigureEightBuilder`** (`Builders/`) — 8자형 루프 (두 원이 한 방을 공유)
2. **영역별 페인터** — Sewer/Dungeon/Hell 3영역 모두 출품 범위 (스코프 락 참조)
3. **`Special Room` 종류** — GDD 6.2.1 의 실제 종류
4. **`Secret Room`** — 숨겨진 입구 + 탐색 확률 발견
5. **빌더 무작위 선택** — MapGenerator 가 BuilderPool SO 에서 선택

### 핵심 설계 결정

#### Q1. FigureEightBuilder 와 LoopBuilder 의 공통 코드
- LoopBuilder 의 `AssignSize`, `PositionInLoop`, `ConnectWithTunnel`, `LShapePath` 등은 8자형에서도 동일
- **권장 B**: `BuilderUtils` 정적 클래스로 헬퍼 추출 (`Map/`) — SO 상속 회피, 깔끔

#### Q2. 영역별 페인터 — 신규 클래스 vs SO 인스턴스
- 3영역(하수구/던전/지옥)이 출품 범위. 영역마다 분위기/타일셋이 명확히 다르므로 영역당 페인터를 둔다.
- DefaultPainter 가 충분히 일반적이면 → 영역별로 SO 인스턴스(다른 RuleTile 할당)로 처리
- 페인트 알고리즘이 영역마다 달라야 하면 → 신규 클래스 (`SewerPainter` / `DungeonPainter` / `HellPainter`)
- **권장**: 공통 골격은 DefaultPainter 유지, 영역 특색(Hazard 연출 등)만 영역 페인터로 분기. 컨셉마다 알고리즘 전면 신규작성은 피함.

#### Q3. Special Room 처리 (GDD 6.2.1)
- RoomType.Special 은 enum 에 있음. Special 방은 **열쇠로 열고 입장**(1초), 종류별 고유 보상.
- 종류: 랜덤 상자 방 / 몬스터 방(고대 골렘) / 반지 상자 방 / 골드 상자 방 / 스크롤 방 — 랜덤 1개 등장.
- 옵션 B: `SpecialRoomContent` SO 풀 (각 종류별 SO) → 페인터/스포너가 종류 보고 배치. 새 종류 추가 = SO 1개 + 풀 등록.

#### Q4. Secret Room 메커니즘 (GDD 6.2.2)
- 숨겨진 문(**열쇠 불필요**), 입구가 wallTilemap 으로 위장.
- 발견 트리거: **탐색 확률 기반** (이동/접촉 시 확률 발견. GDD 8.6 "조심스러운 발걸음" 특성이 탐색 확률↑).
- 종류: 포션 방 / 장비 상자 방 / 골드 방 / 인챈트 방 — 랜덤 1개 등장.
- 데이터: `RoomData.type == Secret` 인 방의 doorCells 에 wallTile 페인트.
- (지도 스크롤이 시크릿 방을 표시하는지는 GDD 9.1 확인 필요 — 지도 스크롤은 "지형+함정"만 공개로 명시됨. 현재는 탐색 확률만 구현, 지도 스크롤 연동은 GDD 확정 후.)

#### Q5. 빌더/페인터 풀
- `BuilderPool` SO — `List<ScriptableObject>` (모든 ILayoutBuilder SO)
- `PainterPool` SO — depth 구간 → 영역 페인터 매핑 (1~5 Sewer, 6~10 Dungeon, 11~15 Hell)
- MapGenerator 가 풀에서 선택 (DungeonRandom). 단 보스층은 BossRoomLoader 분기로 빠지므로 풀 선택 대상 아님.

### 검증 기준

- [ ] MapGenerator 가 BuilderPool 에서 빌더 무작위 선택 (시드별 다른 빌더)
- [ ] depth 구간별 다른 영역 페인터 사용 (1~5 / 6~10 / 11~15)
- [ ] 보스층(5/10/15)은 풀 선택 안 함 → 고정 맵
- [ ] Special Room 이 층당 0~1개 등장 (GDD 6.2)
- [ ] Secret Room 이 ~30% 층에 등장 (시드 100개 기준)
- [ ] 8자 빌더로도 entrance↔exit 100 시드 연결성 통과

### Phase 5 출력물

| 파일 | 위치 | 비고 |
|------|------|------|
| `FigureEightBuilder.cs` | `Builders/` | 8자 빌더 |
| `BuilderUtils.cs` | `Map/` | LoopBuilder/FigureEight 공통 헬퍼 |
| `SewerPainter.cs` | `Painters/` | 하수구 (Area 1) |
| `DungeonPainter.cs` | `Painters/` | 던전 (Area 2) |
| `HellPainter.cs` | `Painters/` | 지옥 (Area 3) |
| `SpecialRoomContent.cs` (+ 종류별 SO) | `Painters/` 또는 `Spawning/` | GDD 6.2.1 5종 |
| `SecretRoomMechanic.cs` | `Rooms/` | 위장 입구 + 탐색 확률 발견 |
| `BuilderPool.cs`, `PainterPool.cs` | `Data/` | SO 풀 |
| `MapGenerator.cs` | `Map/` | **수정** — 풀에서 빌더/페인터 선택 (보스층 제외) |

### 스코프 주의 (CLAUDE.md 스코프 락)

- **3영역·3보스는 출품 범위 — 줄이지 않음.** 스코프 방어선은 영역당 콘텐츠 깊이(몬스터/아이템/방종류 = GDD 풀 고정).
- "추가 빌더 (Random, Maze 등)" → BACKLOG.md
- "GDD에 없는 Special/Secret 방 종류 발명" → 금지. GDD 6.2.1/6.2.2 종류만.
- "다국어 지원, 저장/로드" → 후순위 (단 챌린지 기록/최고점수 저장은 13장 범위)
- Feeling 추가 금지 — 3종 고정

---

## 통합 검증 (Phase 5 완료 시점)

본 개발 진입 점검표 (CLAUDE.md 참조) 7개 항목 모두 "예" 가 나와야 함:

- [ ] 코어 시스템 이전 완료 (`KDU/01.Scripts/{Grid, Time, Vision, Turn, Events}`)
- [ ] 새 적 종류 추가가 SO 인스턴스 + 인스펙터 설정만으로 가능
- [ ] 새 함정 종류 추가가 같은 방식으로 가능
- [ ] 새 Feeling 추가가 클래스 1개 + 풀 등록만으로 가능 (단 풀은 3종 고정)
- [ ] 새 빌더/페인터 추가가 같은 방식으로 가능
- [ ] 보스층이 절차생성을 스킵하고 고정 맵을 로드
- [ ] 아이템/저주/마법이 GameEvents 구독으로 동작

추가:
- [ ] 100 시드 × depth 1~15 모든 조합에서 entrance↔exit 연결성 PASS (보스층은 고정맵 로드 PASS)
- [ ] 결정론: 동일 (runSeed, depth) → 동일 맵 / 동일 Feeling / 동일 스폰
- [ ] 마법사 스켈레톤 0 멍 (모든 원거리 캐스터가 LOS 검증 통과 셀에 배치)

---

## 작업 순서 (권장)

1. **Phase 3 시작 전**: Q1~Q3 의사결정 → 본 문서 업데이트
2. **Phase 3**:
   - Step 1: FloorFeeling 시그니처 확정 (ApplyToPaint 제거, ApplyToInit/ApplyToSpawn 정리)
   - Step 2: HazardFeeling + FloorFeelingPool + MapGenerator 통합 (가장 핵심 1개로 파이프라인 완성)
   - Step 3: DarkFeeling (VisionSystem 인자 1개)
   - Step 4: TrapsFeeling — Phase 4 진행 후 마무리
   (옛 Grass/Chasm Step 삭제)
3. **Phase 4**:
   - Step 1: IEnemySpawner 시그니처 확정 + EnemySpawner 골격
   - Step 2: NormalRoomRule + SafeRoomRule (가장 기본 2개)
   - Step 3: GridSystem LOS 헬퍼 + 마법사 스켈레톤 검증
   - Step 4: EnemySpawnPool + 거리 기반 차등
   - Step 5: TrapsFeeling 마무리 + ApplyToSpawn 활성화
4. **Phase 5**:
   - Step 1: BuilderUtils 추출 (LoopBuilder 리팩토링)
   - Step 2: FigureEightBuilder
   - Step 3: BuilderPool + MapGenerator 빌더 선택
   - Step 4: 영역 페인터 3종 (Sewer/Dungeon/Hell)
   - Step 5: PainterPool + depth 기반 선택
   - Step 6: Special 방 종류 (GDD 6.2.1)
   - Step 7: Secret 방 위장 입구 + 탐색 확률 (GDD 6.2.2)

각 Step 완료 후 통합 테스트 (`MapGenerationTester` 스트레스 테스트) 통과 확인.
