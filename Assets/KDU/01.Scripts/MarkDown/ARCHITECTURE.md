# ARCHITECTURE.md — Stolen Time 코드 아키텍처 상세

> `CLAUDE.md`의 "핵심 아키텍처 결정"의 근거·예시·상세. 아키텍처 의문이 있을 때 읽는다.
> 결정 자체는 CLAUDE.md가 정본. 충돌 시 CLAUDE.md / GDD를 따른다.

## 핵심 원칙

**살아남을 코어는 제대로, 단발 기능은 가볍게.** 모든 코드를 추상화 강박으로 짜지 말 것. 1인 개발로 출시까지 가야 한다.

## 티어 구분

### 티어 1 — 재사용 시스템 (제대로 짤 것)
본 게임 평생 살아남을 시스템. 처음부터 확장성 고려.
- `GridSystem` — 격자 좌표/이동/좌표 변환
- `TimeSystem` — 60초 카운트다운 + 행동 비용
- `VisionSystem` — 시야 계산 + 안개 관리
- `TurnManager` — 플레이어 행동 → 적 행동 동기화
- `Entity` 추상화 — IGridEntity, IDamageable, EntityBase
- 데이터 정의 — EnemyData, TrapData, WeaponData, ItemData (ScriptableObject)
- 이벤트 시스템 — GameEvents (정적 이벤트 허브)
- 맵 생성기 코어 — ILayoutBuilder, IRegionPainter, RoomData, BossRoomLoader
- DungeonRandom — System.Random 래핑, 시드 결정론

### 티어 2 — 콘텐츠 구현 (가볍게 짤 것)
추상화 X, 직접 구현. 콘텐츠 추가는 SO 인스턴스로.
- 개별 몬스터 AI (GDD 10장 로스터)
- 개별 함정, 개별 아이템 효과(각 포션/스크롤/마법)
- 개별 빌더/페인터 구현체
- 보스 구현(감시자/불사왕/하데스) + 보스층 고정 맵
- UI 화면

## 확장성 가이드 — 티어 1 코드만 적용

**1. 데이터와 로직 분리**
```csharp
// ❌ 하드코딩
if (enemy.id == "PlagueRat") { speed = 1; hp = 8; }

// ✅ ScriptableObject
[CreateAssetMenu]
public class EnemyData : ScriptableObject {
    public string id;
    public int hp;
    public float moveSpeed;
    public int timeBonus;
    public AttackPattern attackPattern;
}
```

**2. 인터페이스 우선 (얕게만)**
```csharp
public interface IDamageable { void TakeDamage(int amount); }
public interface IGridEntity {
    Vector2Int GridPosition { get; }
    void OnTurnUpdate();
}
```
구현이 1개뿐이면 인터페이스 만들지 마. 2개 이상 다형성 필요한 곳에만.

**3. 이벤트 기반 통신 (강력 추천)**
```csharp
public static class GameEvents {
    public static event Action<Enemy> OnEnemyKilled;
    public static event Action<Trap> OnTrapTriggered;
    public static event Action<float> OnTimeChanged;
}
```
UI/사운드/시간보너스/저주발동 등이 다 구독. 직접 참조보다 무조건 이거.

**4. ScriptableObject 우선** — 적/함정/무기/아이템/포션/스크롤/저주/마법 다 SO. 콘텐츠 추가 = 데이터만, 코드 수정 X. 디자이너가 인스펙터에서 수치 튜닝.

**5. 매직넘버 금지(단 적당히)**
```csharp
// ❌ if (timeRemaining < 5f) ...
// ✅ [SerializeField] private float lowTimeWarning = 5f;
```
핵심 상수는 SO `GameConfig` 하나에 모으기.

## 파일 크기 및 분리 기준

**SOLID 완벽주의 X, 실용적 분리 O.**

| 줄 수 | 판단 |
|------|------|
| ~200 | 이상적 |
| ~400 | 정상, 분리 고민 시작 |
| ~600 | 책임 2개 이상이면 쪼갬 |
| 700+ | 분리 필수 |
| 1000+ | 적신호, 즉시 리팩토링 |

예외: 데이터/SO/enum 정의 파일은 길어도 OK.

**분리 신호** — `#region` 쓰고 싶을 때 / 메서드가 `Move*()`·`Attack*()` 두 그룹으로 갈릴 때 / 한 메서드 100줄+ / `if-else` 5분기+ / 테스트 어려움.

**현실적 분리 — Player 예시**
```csharp
// ✅ 각 100~250줄
public class GridPlayer : MonoBehaviour {
    private GridPlayerInput    input;
    private GridPlayerMovement movement;
    private GridPlayerCombat   combat;
    private GridPlayerHealth   health;
}
```
3~4개로 쪼개는 게 적당. 10개는 그게 더 함정.

**분리 우선순위**: ① 데이터↔로직(SO로) ② 입력↔도메인로직 ③ 렌더링/애니↔게임로직(로직은 plain C#) ④ 이벤트 발행↔구독(UI는 GameEvents 구독). 이 4개만 지켜도 700줄 클래스 거의 안 생김.

**과분리 함정** — 인터페이스 강박(`IPlayerMovementImpl`), 5줄 헬퍼 클래스(→static), getter/setter 래퍼(→public 프로퍼티), 추상클래스 남발(진짜 다형성만: 적 AI/빌더/페인터/Feeling/보스), 파일만 쪼개고 의존성 그대로.

## 권장 구현 패턴

- 격자 좌표는 `Vector2Int`, 화면 변환은 GridSystem 전담
- 턴 진행은 코루틴/큐 기반 (`Update()` 매프레임 체크 X)
- 핵심 로직은 plain C# (MonoBehaviour 의존 낮추기, 테스트 가능)
- Singleton보다 ServiceLocator/정적 이벤트. **단 싱글톤이 정말 필요하면 `Util/Singleton<T>` 또는 `Util/PersistentSingleton<T>` 상속을 우선** — `public static T Instance` + Awake 중복 검사 코드를 매번 손으로 쓰지 말 것. 새 매니저 클래스는 기본적으로 이 상속 사용, 기존 수동 싱글톤도 발견 시 점진적으로 교체.
- 타일맵은 RuleTile (비트마스크 직접 X)
- 시드 생성은 `DungeonRandom` 통일
- 보스층은 `MapGenerator`에서 먼저 분기해 빌더/페인터가 안 건드리게

## 디렉토리 구조

기능별 폴더 + `[티어 1]`/`[티어 2]` 태그. `Assets/{이니셜}/`는 출품 당시 팀원별 폴더 — 1인 개발로 전환 후에도 참조 깨짐 방지 위해 그대로 유지. 새 코드는 KDU 기능 폴더에.

```
Assets/
├── KDU/                                # 김도언 (PM/맵)
│   ├── 00.Scene/
│   ├── 01.Scripts/
│   │   ├── Data/                       # [티어 1] SO 정의 (EnemyData, TrapData, WeaponData, GameConfig)
│   │   ├── Entities/                   # [티어 1] IGridEntity, IDamageable, EntityBase
│   │   ├── Events/                     # [티어 1] GameEvents
│   │   ├── Grid/                       # [티어 1] GridSystem
│   │   │   ├── Player/                 # [티어 2] GridPlayer + Input/Movement/Combat/Health
│   │   │   ├── Enemies/                # [티어 2] 적 AI (GDD 10장 로스터)
│   │   │   └── Items/                  # [티어 2] GridThrowable(투척+회수), GridExit
│   │   ├── Map/                        # [티어 1] RoomData, DungeonRandom, ILayoutBuilder,
│   │   │   │                           #          IRegionPainter, FloorFeeling, BossRoomLoader, MapGenerator
│   │   ├── Builders/                   # [티어 2] LoopBuilder, FigureEightBuilder (Phase 2~)
│   │   ├── Painters/                   # [티어 2] DefaultPainter, Sewer/Dungeon/HellPainter (Phase 2~)
│   │   ├── Feelings/                   # [티어 2] HazardFeeling, DarkFeeling (2종 고정)
│   │   ├── Spawning/                   # [티어 2] EnemySpawner, ItemSpawner, Rules/ (Phase 4~)
│   │   ├── Time/                       # [티어 1] TimeSystem
│   │   ├── Turn/                       # [티어 1] TurnManager
│   │   ├── Vision/                     # [티어 1] VisionSystem
│   │   ├── Shared/                     # GameFlow, CameraFollow, DebugUI, FogRenderer, TelegraphIndicator
│   │   ├── Challenge/                  # [티어 2] 챌린지 토글/선택, DailyChallenge(KST 날짜 키·시드)
│   │   ├── Score/                      # [티어 2] 점수·로컬 기록판, 랭킹 창(RankingWindow 탭 전환, LeaderboardPanel, RankRow)
│   │   ├── Steam/                      # [티어 2] SteamBootstrap(자동 초기화), SteamLeaderboard(일일·전체 보드)
│   │   ├── Util/                       # Singleton 등
│   │   └── MarkDown/                   # 작업 명세
│   ├── 02.SOData/                      # SO 인스턴스 (.asset)
│   └── 03.Prefab/                      # 프리팹 (보스층 고정 맵 프리팹 포함)
├── LSH/                                # 보스
│   └── 02.Scripts/Boss/                # [티어 2] BossBase, BossWatcher, BossImmortalKing, BossHades
└── KKH/NYH/YJW 등                    # 출품 당시 팀원 폴더 — 그대로 유지

# 미생성: _DoNotShip/ (GPL 격리, 빌드 제외), 자체 에셋·공용 UI 씬
```

**특이점**: `Grid/Player`, `Grid/Enemies`, `Grid/Items`는 티어 2가 티어 1 폴더에 중첩된 프로토 잔재 — 분리는 필요할 때만(1인 개발에선 우선순위 낮음). `Builders/Painters/Feelings/Spawning`은 Phase 2 이후 신설. Feelings는 3종 고정. 보스층은 `MapGenerator` → `BossRoomLoader` 분기.

## 팀 분담 (출품 당시 기록)

출품 이후 **1인 개발**. 아래 표는 어느 폴더에 무엇이 있는지 찾는 지도로만 사용.

| 담당 | 폴더 | 주 작업 |
|------|------|---------|
| 김도언 (PM/맵) | `Assets/KDU/` | 코어 시스템 + 맵 생성 + 보스층 고정맵 연동 |
| LSH (보스) | `Assets/LSH/` | 보스 3종 + 보스 고정맵 레이아웃 |
| 멤버 C (플레이어) | `Assets/{이니셜}/` | 스탯·장비·저주·마법·힘 해금 |
| 멤버 D (적/함정) | `Assets/{이니셜}/` | 몬스터·스폰 규칙·함정 |
| 멤버 E (아이템/UI) | `Assets/{이니셜}/` | 포션/스크롤·UI·Safe Room·상점 |

**코어 영역(KDU 폴더 내)** — 변경 시 결정론·연결성 점검 후 커밋: `Grid, Map, Time, Turn, Vision, Events, Entities, Data`

## 도언(PM/맵) 작업 시 응답 스타일

GDD 질문 시 전체 재작성 금지. 대신: 문제/이유/제안 표, 구현 가능한 규칙, Unity 친화 데이터 구조(SO), 맵 생성 파이프라인 단계, 층별 체크리스트. 우선순위는 레벨 디자인 명확성 → 맵 생성 규칙 → 영역 정체성/Feeling → Safe Room 흐름 → Tilemap/RuleTile 구현 스펙.
