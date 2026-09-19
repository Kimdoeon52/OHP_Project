# AGENTS.md — 60초 던전 (본 개발)

> **⚠️ 현재 단계: 출품 완료(2026-09) → 1인 개발로 Steam 출시 버전 제작.** 이 문서의 "출품·4개월·5인 팀·PR 리뷰·P2" 표기는 출품 기준이라 낡음.
> 최신 스코프·금지 패턴·Steam 연동 규칙은 `CLAUDE.md`와 `Assets/KDU/01.Scripts/MarkDown/SCOPE_AND_ROADMAP.md` 0절을 따를 것.

## 프로젝트 개요

**프로젝트명**: 60초 던전 (가제)
**단계**: 본 개발 (프로토타입 검증 완료 후 신규 프로젝트)
**목적**: 비버롹스 인디 게임 페스티벌 출품작 완성
**기간**: 약 4개월
**팀**: 프로그래머 5명
**엔진**: Unity / C#
**플랫폼**: PC (키보드/마우스)

## 한 줄 컨셉

**Safe Room에서 계획을 세우고, 60초 안에 던전 한 층을 탈출하라.**
Shattered Pixel Dungeon + Half-Minute Hero 결.

## ⚠️ 라이선스 경고 (반드시 준수)

녹픽던 / Shattered Pixel Dungeon은 **GPLv3**.

- 본 개발 빌드는 **단 한 픽셀, 단 한 줄도 GPL 에셋/코드 금지**
- 디자인만 참고, 코드는 절대 가져오지 말 것
- 임시 사용 중인 GPL 에셋은 자체 제작으로 100% 교체될 때까지 `_DoNotShip/` 폴더에 격리
- 빌드 스크립트에서 `_DoNotShip/` 폴더 제외 처리 필수

## 핵심 게임 시스템 (이전 검증 완료)

### 검증된 핵심 재미
1. **60초 압박감** — 통과
2. **카이팅 + 단검 회수 사이클** — 통과
3. **적 패턴 회피 (텔레그래프)** — 통과
4. **공개 vs 숨겨진 함정** — 통과
5. **부스 시연 적합성** — 통과

### 본 개발에서 추가되는 시스템
- **Safe Room** — 매 층 시작 방, 타이머 정지, 몬스터 진입 불가
- **절차적 맵 생성** — 매 층 다른 맵 (Initialize → Build → Paint)
- **Floor Feeling** — 층별 테마 (Water/Grass/Chasm/Traps/Dark)
- **15층 + 보스 3회** (5/10/15층, 우선순위 P1)
- **아이템 확장** — 스크롤/포션/이슬/투척무기/장착장비/저주
- **힘 스탯 기반 장비 해금**

자세한 명세는 `60sec_GDD_v0.1.docx` 참조.

---

## 코드 아키텍처 가이드 (본 개발 적용)

### 핵심 원칙

**살아남을 코어는 제대로, 단발 기능은 가볍게.**

본 개발이라고 모든 코드를 추상화 강박으로 짜지 말 것. 4개월 안에 출품해야 한다. **두 개의 티어로 명확히 구분**:

### 티어 1 — 재사용 시스템 (제대로 짤 것)

본 게임 평생 살아남을 시스템. 처음부터 확장성 고려.

- **`GridSystem`** — 격자 좌표 / 이동 / 좌표 변환
- **`TimeSystem`** — 60초 카운트다운 + 행동 비용
- **`VisionSystem`** — 시야 계산 + 안개 관리
- **`TurnManager`** — 플레이어 행동 → 적 행동 동기화
- **`Entity` 추상화** — IGridEntity, IDamageable, EntityBase
- **데이터 정의** — EnemyData, TrapData, WeaponData, ItemData를 ScriptableObject로
- **이벤트 시스템** — GameEvents (정적 이벤트 허브)
- **맵 생성기 코어** — ILayoutBuilder, IRegionPainter, RoomData
- **DungeonRandom** — System.Random 래핑, 시드 기반 결정론적 난수

### 티어 2 — 콘텐츠 구현 (가볍게 짤 것)

추상화 X, 직접 구현. 콘텐츠 추가는 SO 인스턴스로.

- 개별 몬스터 AI (Tracker, Archer 등)
- 개별 함정 (Spike, Alarm 등)
- 개별 아이템 효과 (각 포션, 각 스크롤)
- 개별 빌더/페인터 구현체 (LoopBuilder, SewerPainter 등)
- UI 화면 (메인 메뉴, HUD, 인벤토리 등)

---

### 확장성 가이드 — 티어 1 코드만 적용

**1. 데이터와 로직 분리**
```csharp
// ❌ 하드코딩
if (enemy.type == "Tracker") { speed = 1; hp = 1; }

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
public interface IDamageable {
    void TakeDamage(int amount);
}

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
UI/사운드/시간 보너스/저주 발동 등이 다 구독. 직접 참조보다 무조건 이거.

**4. ScriptableObject 우선**
- 적 / 함정 / 무기 / 아이템 / 포션 / 스크롤 / 저주 — 다 SO
- 콘텐츠 추가 = 데이터만 추가, 코드 수정 X
- 디자이너가 인스펙터에서 수치 튜닝 (밸런싱 핵심)

**5. 매직넘버 금지 — 하지만 너무 멀리 가지 말 것**
```csharp
// ❌ 매직넘버
if (timeRemaining < 5f) ...

// ✅ const 또는 SerializeField
[SerializeField] private float lowTimeWarning = 5f;
```
ScriptableObject `GameConfig` 하나에 핵심 상수 모아두기.

---

### 파일 크기 및 분리 가이드

**SOLID 완벽주의 X, 실용적 분리 O.**

한 파일이 700~1000줄 넘으면 본인도 못 찾고 Codex 작업도 비효율. 처음부터 적당한 사이즈로 유지.

**파일 크기 기준**

- **~200줄**: 이상적, 한눈에 파악 가능
- **~400줄**: 정상 범위, 분리 고민 시작
- **~600줄**: 분리 검토 — 책임 두 개 이상이면 무조건 쪼갬
- **700줄+**: 분리 필수, 예외 거의 없음
- **1000줄+**: 적신호, 즉시 리팩토링

**예외**: 데이터 클래스 / SO / enum 정의 파일은 길어도 OK (한 가지 일만 함).

**분리 신호 — 이런 게 보이면 쪼개기**

1. `#region`을 쓰고 싶어지는 순간 — 그 region이 별도 클래스 신호
2. 메서드 이름이 두 영역으로 그룹화 — `Move*()`와 `Attack*()`이 한 클래스에 있으면 둘로 쪼갤 시점
3. 한 메서드가 100줄 넘어감 — 내부 단계를 private 메서드로 추출
4. `if-else` 분기가 5개 넘어감 — 전략 패턴 또는 데이터 분리 신호
5. 테스트 작성이 어려움 — 의존성 너무 많다는 뜻

**현실적 책임 분리 — Player 예시**

```csharp
// ❌ 다 한 곳에 (700줄 가는 흔한 패턴)
public class Player : MonoBehaviour {
    // 입력 / 이동 / 시야 / 단검 / HP / 애니메이션 / 사운드 / UI
}

// ✅ 적당히 분리 (각 100~250줄)
public class GridPlayer : MonoBehaviour {
    private GridPlayerInput    input;
    private GridPlayerMovement movement;
    private GridPlayerCombat   combat;
    private GridPlayerHealth   health;
}
```

3~4개로 쪼개는 게 적당. 10개로 쪼개면 그게 더 함정.

**과분리 함정 — 하지 말 것**

- ❌ 인터페이스 강박 — `IPlayerMovement`, `IPlayerMovementImpl` 같은 거 X
- ❌ 5줄짜리 헬퍼 클래스 — 그냥 static 메서드로
- ❌ getter/setter 래퍼 — public 프로퍼티로 충분
- ❌ 추상 클래스 남발 — 진짜 다형성 필요한 곳에만 (적 AI, 빌더, 페인터, Feeling 등)
- ❌ 파일 분리만 하고 의존성 그대로 — 모든 컴포넌트가 모든 컴포넌트를 직접 참조하면 의미 X

**분리의 우선순위**

1. **데이터 ↔ 로직 분리** (가장 중요) — SO로 데이터 빼기
2. **입력 ↔ 도메인 로직 분리** — `PlayerInput`은 `PlayerMovement.Move(direction)` 호출만
3. **렌더링/애니메이션 ↔ 게임 로직 분리** — 로직은 plain C#, 애니메이션은 별도 컴포넌트
4. **이벤트 발행자 ↔ 구독자 분리** — UI는 GameEvents 구독, 게임 로직이 UI 직접 호출 X

이 4개만 지켜도 700줄 클래스는 거의 안 생김.

---

### 5명 협업 측면

5명이 같은 파일 만지면 머지 컨플릭트 지옥. 처음부터 분리하는 게 협업의 핵심.

**팀원별 작업 폴더 (충돌 최소화)**

각 팀원은 `Assets/{이니셜}/` 본인 폴더에서 작업. 코어 시스템은 PM(KDU) 폴더에 모이며, 다른 팀원이 코어 변경 필요 시 PR 리뷰 필수.

| 담당 | 본인 폴더 | 주 작업 |
|------|-----------|---------|
| 김도언 (PM/맵) | `Assets/KDU/` | 코어 시스템 전반 + 맵 생성 (Map / Builders / Painters / Feelings) |
| LSH (보스) | `Assets/LSH/` | 보스 구현 (BossA, BossB 등) |
| 멤버 C (플레이어) | `Assets/{이니셜}/` | 스탯, 장비, 저주, 힘 해금 |
| 멤버 D (적/함정) | `Assets/{이니셜}/` | 몬스터 추가, 스폰 규칙, 함정 |
| 멤버 E (아이템/UI) | `Assets/{이니셜}/` | 포션/스크롤, UI, Safe Room 흐름 |

**코어 영역 (KDU 폴더 내)** — 변경 시 PR 리뷰 필수:
`Assets/KDU/01.Scripts/{Grid, Map, Time, Turn, Vision, Events, Entities, Data}`

---

### 디렉토리 구조

**팀원별 최상위 폴더 (`Assets/{이니셜}/`) 패턴.** 코어는 KDU 폴더 안에 모이고, 다른 팀원은 본인 폴더에서 작업.
폴더 안의 `[티어 1]` / `[티어 2]` 표기는 책임 구분용 — 같은 폴더에 섞여 있어도 OK.

```
Assets/
├── KDU/                                # 김도언 (PM/맵) 작업 폴더
│   ├── 00.Scene/                       # 씬 파일
│   ├── 01.Scripts/
│   │   ├── Data/                       # [티어 1] SO 데이터 정의
│   │   │   ├── EnemyData.cs
│   │   │   ├── TrapData.cs
│   │   │   ├── WeaponData.cs
│   │   │   └── GameConfig.cs
│   │   ├── Entities/                   # [티어 1] 엔티티 추상화
│   │   │   ├── IGridEntity.cs
│   │   │   ├── IDamageable.cs
│   │   │   └── EntityBase.cs
│   │   ├── Events/                     # [티어 1] 정적 이벤트 허브
│   │   │   └── GameEvents.cs
│   │   ├── Grid/                       # [티어 1] 격자 시스템
│   │   │   ├── GridSystem.cs
│   │   │   ├── Player/                 # [티어 2] 플레이어
│   │   │   │   ├── GridPlayer.cs
│   │   │   │   ├── GridPlayerInput.cs
│   │   │   │   ├── GridPlayerMovement.cs
│   │   │   │   ├── GridPlayerCombat.cs
│   │   │   │   └── GridPlayerHealth.cs
│   │   │   ├── Enemies/                # [티어 2] 적 AI
│   │   │   │   ├── GridEnemyBase.cs
│   │   │   │   ├── GridEnemyTracker.cs
│   │   │   │   └── GridEnemyArcher.cs
│   │   │   └── Items/                  # [티어 2] 단검, Exit
│   │   │       ├── GridDagger.cs
│   │   │       └── GridExit.cs
│   │   ├── Map/                        # [티어 1] 맵 생성 코어
│   │   │   ├── RoomData.cs
│   │   │   ├── DungeonRandom.cs
│   │   │   ├── ILayoutBuilder.cs
│   │   │   ├── IRegionPainter.cs
│   │   │   ├── FloorFeeling.cs         # 추상 + IEnemySpawner placeholder
│   │   │   └── MapGenerator.cs
│   │   ├── Builders/                   # [티어 2] 맵 빌더 구현체 (Phase 2~)
│   │   │   ├── LoopBuilder.cs
│   │   │   └── FigureEightBuilder.cs
│   │   ├── Painters/                   # [티어 2] 페인터 구현체 (Phase 2~)
│   │   │   ├── DefaultPainter.cs
│   │   │   └── ConceptOnePainter.cs
│   │   ├── Feelings/                   # [티어 2] Floor Feeling (Phase 3~)
│   │   │   ├── WaterFeeling.cs
│   │   │   ├── GrassFeeling.cs
│   │   │   ├── ChasmFeeling.cs
│   │   │   ├── TrapsFeeling.cs
│   │   │   └── DarkFeeling.cs
│   │   ├── Spawning/                   # [티어 2] 스폰 (Phase 4~)
│   │   │   ├── EnemySpawner.cs        # IEnemySpawner 구현
│   │   │   └── ItemSpawner.cs
│   │   ├── Time/                       # [티어 1] 60초 카운트다운
│   │   │   └── TimeSystem.cs
│   │   ├── Turn/                       # [티어 1] 턴 매니저
│   │   │   └── TurnManager.cs
│   │   ├── Vision/                     # [티어 1] 시야 + 안개
│   │   │   └── VisionSystem.cs
│   │   ├── Shared/                     # 공용 컴포넌트/플로우
│   │   │   ├── PrototypeGameFlow.cs
│   │   │   ├── CameraFollow.cs
│   │   │   ├── DebugUI.cs
│   │   │   ├── FogRenderer.cs
│   │   │   └── TelegraphIndicator.cs
│   │   ├── Util/                       # 헬퍼
│   │   │   └── Singleton.cs
│   │   └── MarkDown/                   # 작업 명세 (MAP_GENERATION.md 등)
│   ├── 02.SOData/                      # SO 인스턴스 (.asset)
│   └── 03.Prefab/                      # 프리팹
├── LSH/                                # LSH 작업 폴더 (보스)
│   └── 02. Scripts/
│       └── Boss/                       # [티어 2] 보스 구현
│           ├── BossBase.cs
│           ├── BossA.cs
│           └── BossB.cs
└── (다른 팀원 폴더 — 합류 시 같은 패턴으로 추가)

# 향후 결정 (현재 미생성)
#  - GPL 격리:  _DoNotShip/  (라이선스 경고 섹션 참조 — 빌드 제외 필수)
#  - 자체 제작 에셋 (스프라이트/타일맵/오디오): 위치 팀 협의 후
#  - 공용 UI 씬/프리팹: 위치 팀 협의 후
```

**현재 구조의 특이점**
- `Grid/Player`, `Grid/Enemies`, `Grid/Items` 처럼 티어 2 콘텐츠가 티어 1 폴더 (`Grid/`) 안에 중첩됨 → 프로토타입 잔재. 본 개발 진행 중 분리 여부는 팀 회의로.
- `Builders/`, `Painters/`, `Feelings/`, `Spawning/` 은 Phase 2 이후 KDU 폴더에 신설 예정.
- 다른 팀원은 본인 작업물을 `Assets/{이니셜}/` 아래 같은 패턴(`00.Scene`, `01.Scripts`, `02.SOData`, `03.Prefab`)으로 정리 권장.

---

### 권장 구현 패턴

- **격자 좌표는 `Vector2Int`** — 화면 좌표 변환은 GridSystem이 전담
- **턴 진행은 코루틴 또는 큐 기반** — `Update()` 안에서 매 프레임 체크 X
- **`MonoBehaviour` 의존도 낮추기** — 핵심 로직은 plain C# 클래스로 (테스트 가능)
- **`Singleton` 보다 `ServiceLocator` 또는 정적 이벤트** — 시스템 늘면 싱글톤 지옥됨
- **타일맵은 RuleTile 활용** — 벽 자동 연결, 코드로 비트마스크 직접 안 짬
- **시드 기반 생성 시 `DungeonRandom` 통일** — `UnityEngine.Random` 직접 호출 금지

---

### 과설계 함정 — 본 개발에서도 하지 말 것

- ❌ DI 컨테이너 (Zenject 등) — 5명 팀에 사치
- ❌ 추상 팩토리, 빌더 패턴 라이브러리 — 콘텐츠 양에 비해 오버엔지니어링
- ❌ 이벤트 버스 라이브러리 — 정적 이벤트로 충분
- ❌ 상태 머신 프레임워크 — 적 AI는 enum + switch로 시작, 복잡해지면 그때 도입
- ❌ ECS — 4개월 일정에 부담
- ❌ Addressables — Resources.Load 또는 직접 참조로 시작
- ❌ 다국어 시스템 — 후반에 도입
- ❌ 저장 / 로드 시스템 — 한 판이 짧음, 출품 직전에 결정

원칙: **"본 개발에서 생길 일"을 미리 만들지 말고, "본 개발에서 생길 일이 자연스럽게 들어올 자리"만 비워둘 것.** 인터페이스, 이벤트, SO 데이터 분리가 그 "자리"임.

---

### 완벽주의 금지

- 한 번에 80% 완성도로 끝내고 다음 작업으로 넘어갈 것
- 폴리싱은 4개월차에 한꺼번에
- 미완성 시스템도 다른 시스템이 의존할 수 있게 인터페이스/이벤트만 먼저 잡아두기
- "이 부분 더 멋있게 짤 수 있는데"는 BACKLOG.md에 기록 후 패스

---

## 스코프 락 (Scope Lock)

GDD에 없는 건 본 개발 중간에 **추가 안 함**. "이거 추가하면 더 좋을 텐데" 충동 발생 시:

1. 4개월 일정에 들어가는지 자문
2. 안 들어가면 → `BACKLOG.md`에 적고 출품 후로 미룸
3. 들어간다면 → 팀 회의 후 결정 (혼자 추가 X)

자주 추가하고 싶어지는 함정:
- "유물 시스템 미리 넣자" → P2 후순위, 출품 후
- "다국어 지원" → 후순위
- "추가 컨셉 영역" → 컨셉 1만 완성도 있게
- "보스 3종 다 만들자" → 컨셉 1 보스만 우선

---

## 진행 상황 추적

매주 갱신.

### Month 1 (기반)
- [ ] 코어 시스템 이전 완료 (`KDU/01.Scripts/{Grid, Time, Vision, Turn, Events}`)
- [ ] 맵 생성기 코어 (RoomData, ILayoutBuilder, IRegionPainter)
- [ ] LoopBuilder 1종 동작
- [ ] DefaultPainter 1종 동작
- [ ] Safe Room / Exit Room 타입 분리
- [ ] 함정 클래스 (Spike, Alarm)
- [ ] 힘 스탯 + 장비 해금 베이스

### Month 2 (콘텐츠 1차)
- [ ] Floor Feeling 3종 (Water, Grass, Traps)
- [ ] 아이템 P0 구현 (지도 스크롤, 회복 포션, 투척용 칼, 소검)
- [ ] 저주 시스템 (정화 스크롤 포함)
- [ ] 몬스터 추가 1~2종

### Month 3 (콘텐츠 2차)
- [ ] 보스층 (depth 5) 1종
- [ ] 몬스터 추가 2종
- [ ] 나머지 Feeling (Chasm, Dark)
- [ ] 나머지 아이템 (포션 6종 전체, 투척 무기 전체)

### Month 4 (폴리싱 + 제출)
- [ ] UI 폴리싱 (HUD, 인벤토리, 메인 메뉴, 죽음 화면)
- [ ] 사운드 디자인
- [ ] 자체 제작 에셋 100% 교체 (GPL 제거)
- [ ] 밸런싱 (60초, 시간 보너스, 몬스터 데미지)
- [ ] 버그 수정
- [ ] 비버롹스 제출 빌드

---

## 본 개발 진입 점검표

신규 프로젝트 셋업 후 확인:

- [ ] 코어 시스템(`KDU/01.Scripts` 의 Grid/Time/Vision/Turn/Events/Entities/Data)이 프로토타입에서 그대로 이전됐는가?
- [ ] 새 적 종류 추가가 SO 인스턴스 + 인스펙터 설정만으로 가능한가?
- [ ] 새 함정 종류 추가가 같은 방식으로 가능한가?
- [ ] 새 Feeling 추가가 클래스 1개 + 풀 등록만으로 가능한가?
- [ ] 새 빌더/페인터 추가가 같은 방식으로 가능한가?
- [ ] 아이템/저주가 GameEvents 구독으로 동작하는가?

이 6개에 다 "예"가 나오면 아키텍처 잘 짠 것.

---

## 참고 게임 (디자인 톤)

- **Shattered Pixel Dungeon** — 절차적 맵, 시야, 아이템 (디자인만, 코드/에셋 X)
- **Crypt of the NecroDancer** — 격자 + 빠른 결정 + 적 패턴 학습
- **Half-Minute Hero (용사 30)** — 시간 압박 + 짧은 사이클
- **Don't Starve** — 어두운 낙서풍 비주얼
- **Into the Breach** — 적 의도 텔레그래프 시스템
- **Hyper Light Drifter** — 카이팅 액션 로그라이크 톤

---

## 참고 문서

- `60sec_GDD_v0.1.docx` — 게임 디자인 명세 전체
- `MAP_GENERATION.md` — 맵 생성 시스템 상세 명세 (Codex 작업용)
- `BACKLOG.md` — 출품 후로 미룬 아이디어 모음

---

**핵심 원칙**: 4개월 안에 출품해야 한다. 코어 시스템(티어 1 — Grid/Time/Vision/Turn/Map 등)은 처음부터 제대로, 콘텐츠(티어 2 — 개별 적/함정/아이템/빌더 등)는 가볍게. 두 티어를 명확히 구분하는 것이 4개월 → 6개월이 되지 않는 비결.
