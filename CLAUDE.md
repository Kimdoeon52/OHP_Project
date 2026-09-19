# CLAUDE.md — Stolen Time (본 개발)

> **단일 진실 공급원 규칙 (반드시 준수)**
> 디자인 수치·규칙의 원본은 **GDD (`Stolen_Time_GDD_v1_3.docx`)** 하나다.
> 이 문서는 "어떻게 짜는가"(아키텍처·원칙·스코프)만 다룬다. 수치를 여기 복붙하지 말고 **"GDD N장 참조"** 로 가리킬 것.
> GDD와 충돌하면 **항상 GDD를 따른다.** 발췌/하위 문서도 마찬가지.

## 프로젝트

- **Stolen Time** — 타일 기반 시간제한 로그라이크. Safe Room 계획 + 60초 실행.
- 비버롹스 출품 완료(2026-09) → **현재 단계: Steam 출시 버전 제작.** **1인 개발**(출품까지는 5인 팀) / Unity·C# / PC(Steam).
- **게임 범위 = 3영역(15층)·3보스.** (감시자/불사왕/하데스, 5/10/15층) 출품 때 확정, 출시 버전도 동일.
- 전체 명세: `Stolen_Time_GDD_v1_3.docx`

## ⚠️ 라이선스 (반드시 준수)

녹픽던/SPD는 **GPLv3**. 빌드에 **GPL 에셋/코드 단 한 줄도 금지**, 디자인만 참고.
임시 GPL 에셋은 자체 제작 100% 교체까지 `_DoNotShip/`에 격리, 빌드 스크립트에서 제외.

## 핵심 아키텍처 결정 (이것만은 반드시 인지)

1. **두 티어 분리.** 티어 1(코어: Grid/Time/Vision/Turn/Map/Events/Entities/Data)은 처음부터 제대로, 티어 2(콘텐츠: 개별 적/함정/아이템/빌더/페인터/Feeling/보스/UI)는 가볍게 80%로. → 상세: `ARCHITECTURE.md`
2. **데이터는 ScriptableObject.** 적·함정·무기·아이템·포션·스크롤·저주·마법 전부 SO. 콘텐츠 추가 = 데이터만 추가, 코드 수정 X.
3. **통신은 정적 이벤트(`GameEvents`).** UI/사운드/시간보너스/저주발동은 구독으로. 직접 참조 X.
4. **시드 결정론은 `DungeonRandom` 통일.** `UnityEngine.Random`/`System.Random` 직접 호출 금지. (챌린지/시드챌린지 기반 — GDD 13장)
5. **Floor Feeling은 3종 고정** — Hazard/Traps/Dark. Hazard는 영역별 리스킨(오염수/함정바닥/용암), area 파라미터로 분기. **Water/Grass/Chasm은 옛 모델 — 만들지 말 것.** (GDD 6.3)
6. **보스층(5/10/15)은 절차생성 제외.** `MapGenerator`가 보스 depth면 스킵 → `BossRoomLoader`로 고정 맵 로드. (GDD 5.1/6.2)
7. **파일 700줄+ 분리 필수.** 한 클래스 한 책임. (예외: 데이터/SO/enum 파일) → 분리 기준 상세: `ARCHITECTURE.md`
8. **Steam 연동은 Steamworks.NET(UPM) 단일 경로.** `SteamBootstrap`이 자동 초기화 → Steam API 호출 전 `SteamBootstrap.IsReady` 확인. Steam 미연결이어도 게임은 돌아가야 함(Steam 기능만 비활성).
9. **일일 챌린지 날짜는 로컬 시계 금지.** Steam 서버 시각(`SteamUtils.GetServerRealTime`) + 9시간 = KST. 날짜 키·시드·보드 이름은 `DailyChallenge`로 통일.

## 폴더 컨벤션

기능별 폴더 + `[티어 1]`/`[티어 2]` 태그. `Assets/{이니셜}/` 최상위 폴더는 출품 당시 팀 구조 — 이동/정리 없이 그대로 유지(참조 깨짐 방지). **새 코드는 KDU 기능 폴더**에, 기존 파일 수정은 원래 위치에서.
코어(`Grid/Map/Time/Turn/Vision/Events/Entities/Data`) 변경 시 PR 리뷰 대신 **결정론·연결성 점검**(동일 시드 → 동일 맵, 입구↔출구 연결) 후 커밋.
→ 전체 트리: `ARCHITECTURE.md`. 맵 관련 파일 배치: `MAP_GENERATION.md`.

## 주석·포맷·인스펙터 라벨 컨벤션 (기준 예시: `MapGenerator.cs`)

- **주석은 짧은 한 줄 `//`.** 메서드/필드 위에 "무엇"만 간결히. `/// <summary>` XML 독 블록·여러 줄 서술·"왜/어떻게" 장황한 설명 ❌.
- **GDD 조항 번호·근거를 주석에 박지 말 것.** 수치·규칙 원본은 GDD(단일 진실) — 주석은 코드가 뭘 하는지만.
- **`[Tooltip]`/`[Header]` 텍스트는 짧은 라벨.** (예: `[Tooltip("문 프리팹")]`, `[Tooltip("Exit")]`)
- **`if` 본문은 한 줄짜리라도 다음 줄로.** `if (x) return;` ❌ → `if (x)` 줄바꿈 후 `    return;`. (블록 `{ ... }`도 본문을 줄바꿈)
- **메서드 파라미터는 한 줄로.** 여러 줄로 쪼개지 말 것.
- 예: `/// <summary>현재 층의 Hazard 셀 좌표 — 이동 처리부가 페널티 판정용으로 조회. Generate 호출마다 갱신.</summary>` → `// 현재 층의 Hazard 셀 좌표`

## 금지 패턴 (하지 말 것)

- ❌ DI 컨테이너(Zenject), 추상 팩토리/빌더 라이브러리, 이벤트 버스 라이브러리, 상태머신 프레임워크 — 1인 개발에 과설계
- ❌ ECS, Addressables — 과설계 (저장/로드·다국어는 이미 있음 → `SaveManager`/`LocalizationManager` 확장)
- ❌ 로컬 시계(`DateTime.Now`)로 일일 챌린지 날짜 판정 — 조작 가능 (반드시 `DailyChallenge`)
- ❌ 인터페이스 강박(구현 1개뿐인데 인터페이스), 5줄 헬퍼 클래스, getter/setter 래퍼, 추상클래스 남발
- ❌ Water/Grass/Chasm Feeling 생성 (3종 고정)
- ❌ 보스층을 절차생성 파이프라인에 태우기
- ❌ `UnityEngine.Random`/`System.Random` 직접 호출 (반드시 `DungeonRandom`)
- ❌ GDD에 없는 콘텐츠(몬스터/아이템/방종류/Feeling) 임의 추가 — 스코프 락

## 스코프 락

출품은 끝났고 **출시 버전 스코프**로 전환. 3영역·3보스는 그대로, 방어선은 여전히 **영역당 콘텐츠 깊이 = GDD 풀로 고정**(몬스터 GDD 10장, 아이템 9장, Feeling 3종, 방종류 6.2.1/6.2.2). GDD에 없는 콘텐츠는 코드부터 추가 금지 → 넣기로 했으면 **GDD 먼저 갱신** 후 구현, 아니면 `BACKLOG.md`.
챌린지 모드(13장): 토글 6종+점수+선택/결산 UI+시드입력 완료. **일일 시드 챌린지(KST 24시간 보드) + 전체 Steam 랭킹은 출시 범위**(코드 구현됨, UI 연결 남음). 시드 직접 입력 런은 로컬·Steam 기록 모두 제외. 나머지 `BACKLOG.md` 항목은 출시 전 포함 여부 분류.
→ 스코프 상세·출시 체크리스트·보스 폴백 전략: `SCOPE_AND_ROADMAP.md`

## 완벽주의 금지

80% 완성도로 끝내고 다음으로. 폴리싱은 출시 전 폴리싱 스프린트에 한꺼번에. 미완성도 인터페이스/이벤트만 먼저 잡아두기. "더 멋있게"는 `BACKLOG.md`.

## 본 개발 진입 점검표 (의도 자가검증 — 작업 후 확인)

- [ ] 코어 시스템이 프로토타입에서 그대로 이전됐는가? (`Grid/Time/Vision/Turn/Events/Entities/Data`)
- [ ] 새 적/함정 추가가 SO 인스턴스 + 인스펙터 설정만으로 가능한가?
- [ ] 새 Feeling 추가가 클래스 1개 + 풀 등록만으로 가능한가? (단 풀은 3종 고정)
- [ ] 새 빌더/페인터 추가가 같은 방식으로 가능한가?
- [ ] **보스층이 절차생성을 스킵하고 고정 맵을 로드하는가?**
- [ ] 아이템/저주/마법이 GameEvents 구독으로 동작하는가?
- [ ] Steam 미연결(클라이언트 꺼짐)에서도 게임이 정상 진행되는가? (Steam 기능만 비활성)
- [ ] 700줄 넘는 클래스가 없는가? (데이터/SO 예외)

## 참고 문서

- `Stolen_Time_GDD_v1_3.docx` — **디자인 단일 진실 공급원** (수치·규칙 원본)
- `ARCHITECTURE.md` — 티어 구분 이유·코드 예시·분리 기준·폴더 트리·(출품 당시) 팀 분담 기록
- `SCOPE_AND_ROADMAP.md` — 스코프 락 상세·월별 마일스톤·보스 폴백 전략
- `MAP_GENERATION.md` — 맵 생성 시스템 명세 (맵 작업 시 읽을 것)
- `PHASE_3_5_PLAN.md` — Phase 3~5 단계별 계획
- `Stolen_Time_Map_Design_for_ClaudeCode.md` — GDD 맵/레벨 발췌 (직접수정 금지, GDD 변경 시 재생성)
- `BACKLOG.md` — 미룬 아이디어 (출시 스코프 분류 대상)

---

**작업 흐름**: 맵 코드 → `MAP_GENERATION.md` 읽기. Phase 작업 → `PHASE_3_5_PLAN.md`. 아키텍처 의문 → `ARCHITECTURE.md`. 수치 → GDD. 그 외엔 이 문서의 결정·금지·점검표만으로 충분.

## Unity 에셋 탐색 규칙

Unity 에셋(.prefab, .unity, .asset)을 읽을 때 cat/find/grep 대신
unity-scanner를 사용한다 (토큰 절감). 프로젝트 루트에서 실행하므로 -p . 사용.

- 폴더 구조: unity-scanner list -p . <path>
- 검색:      unity-scanner search -p . Assets --name <name> --type <type>
- 구조 파악:  unity-scanner read -p . <asset>
- 필드 확인:  unity-scanner read -p . <asset> --component <name>
- 참조 추적:  unity-scanner refs -p . <asset>

먼저 list/search로 지도를 그리고, 필요한 대상만 read로 드릴다운.