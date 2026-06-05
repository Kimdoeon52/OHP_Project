# TUTORIAL_PLAN.md — 튜토리얼 구현 계획

> 상태: **계획만** (코드 미구현). GDD 외 콘텐츠(현재 GDD 미반영) — 출품 범위 확정 시 스코프에 정식 편입 필요. 관련 결정은 BACKLOG/SCOPE 와 동기화.
> **우선순위 원칙(사용자 지정)**: ① 내 코드(KDU) 수정 또는 신규 코드로 구현 가능한 것 = **최우선**. ② 다른 팀원 코드 의존 = **협의 후 별도 진행**.

---

## 1. 목표 / 구조

고정 레이아웃의 짧은 안내형 튜토리얼. 절차생성 X — **손으로 그린 고정 맵**(보스방과 같은 사상).

### 맵 레이아웃 (사용자 제공)
```
[시작 SafeRoom] ── [몬스터 방] ── [출구 방 (포탈 ◯)]
                       │
                  [숨겨진 방]
```

### 진행 10단계
| # | 단계 | 완료 판정(안) |
| --- | --- | --- |
| 1 | 시작 | 컨트롤러 시작 / 아무 키 입력 |
| 2 | 지도 스크롤 사용 | 지도 스크롤 아이템 사용 감지 |
| 3 | 맵 둘러보기 | SafeRoom 플래닝 카메라 줌/팬 입력 감지 |
| 4 | 인벤토리에서 아이템 소개 | 인벤토리 열림(E) 감지 |
| 5 | 몬스터 방으로 이동 | 플레이어가 몬스터 방 영역 진입 |
| 6 | 전투 및 열쇠 획득 | 적 처치 + 열쇠 보유 |
| 7 | 숨겨진 방 진입 | 플레이어가 숨겨진 방 영역 진입 |
| 8 | 열쇠로 상자 열기 | RequiresKey 상자 개방 |
| 9 | 출구방으로 이동 | 플레이어가 출구 방 영역 진입 |
| 10 | 포탈로 1층 진입 | 포탈(GridExit) 도달 → **인벤토리 초기화** → 1층 로드 |

> **제약: 튜토리얼 중 얻은 아이템은 인벤토리에 정상 적립하되, 1층 진입 시 전부 사라진다.**
> 상자 개방(단계 8)·전투/열쇠(단계 6) 보상은 튜토리얼 동안 실제로 인벤토리에 들어가서 학습 가능.
> 단, **단계 10(포탈→1층 전환) 시점에 인벤토리(+열쇠/획득물)를 초기화**해 본 게임은 빈손으로 시작.
> 구현: 튜토리얼→1층 전환 직전 `InventoryController` 비우기 + `KeyInventory.ClearAll`(층 진입 시 이미 폐기됨). 본 게임 시작 상태와 동일하게 리셋.

## 2. 아키텍처 (신규)

- **전용 튜토리얼 씬** + 손으로 배치한 고정 Tilemap(Floor/Wall) + 배치 오브젝트(상자·열쇠·적·포탈·아이템 픽업). 절차생성/보스로더 불필요 — 씬에 직접 그림.
- `TutorialController` (신규, KDU): 선형 단계 시퀀스 구동. 현재 단계 인덱스 + 각 단계의 완료 조건 검사 → 다음 단계. GameEvents 구독 + 플레이어 상태 폴링.
- `TutorialStep` 표현: 단계별 (안내 텍스트 + 완료조건 enum + 옵션 하이라이트/영역 마커). 과설계 금지 — 컨트롤러 1개 + 직렬화 배열/enum switch 로 충분 (파일 1개·한 책임).
- **영역 판정**: 단계 5/7/9 는 방별 트리거 존(BoxCollider2D) 또는 인스펙터 셀 RectInt 로. (트리거 존 권장 — 손배치 맵과 궁합.)
- `TutorialHintUI` (신규, UI): 안내 텍스트 패널(+ 옵션 화살표/하이라이트).
- **결정론 무관**: 고정 맵이라 RNG 불필요.

## 3. 단계별 구현 분류 ★우선순위 핵심

### A. 즉시 구현 가능 (KDU 코드 / 신규) — 최우선

| 단계 | 근거 (기존 자산/이벤트) | 필요한 신규 |
| --- | --- | --- |
| 0 (공통) | — | 튜토리얼 씬 + 고정 맵 배치, `TutorialController`, `TutorialHintUI` |
| 1 시작 | 컨트롤러 자체 | 인트로 안내 |
| 3 맵 둘러보기 | `CameraFollow.ArmSafeRoom`(KDU) — 줌/팬 입력 KDU 소유 | `CameraFollow` 에 "팬/줌 발생" 플래그/이벤트 노출 |
| 5/7/9 방 이동 | `GridPlayer.GridPosition`(KDU) | 방별 트리거 존 + 단계 진입 판정 |
| 6 처치/열쇠 (판정부) | `GameEvents.OnEnemyKilled`(존재) · `KeyInventory`/`KeyPickup`(KDU) | `KeyInventory.Grant` 에 `OnKeyAcquired` 이벤트 추가 |
| 8 열쇠 상자 | `GridChest.RequiresKey` + `KeyInventory` 이미 동작 | `GridChest.TryOpen` 에 `GameEvents.OnChestOpened` 추가 |
| 10 포탈→1층 | `GridExit`→`OnLevelComplete`→`FloorTransitionController`(KDU) | 튜토리얼→1층 전환 배선(씬 전환 또는 depth=1 빌드 트리거) |

**신규 GameEvents (KDU 소유, 추가 가능):**
- `OnChestOpened(Vector2Int cell)` — 단계 8. `GridChest.TryOpen` 에서 발행.
- `OnKeyAcquired(KeyType)` — 단계 6. `KeyInventory.Grant` 에서 발행.

### B. 팀원 협의 후 진행

| 단계 | 의존 | 협의 내용 |
| --- | --- | --- |
| 2 지도 스크롤 사용 | **지도 스크롤 아이템 미구현** (스크롤은 Reinforce/Purify/Analyze만 존재) · 아이템 사용 = NYH/YJW | 지도 스크롤(맵 공개) 효과를 누가 구현할지(KDU 신규 vs YJW) + 사용 감지 이벤트. 미구현이면 단계 2 는 임시로 "스크롤 줌으로 대체" 가능하나 GDD 의도와 다름 |
| 4 인벤토리 아이템 소개 | 인벤토리 = NYH (`InventoryController`/`InventoryPanelToggle`) | 인벤토리 열림 이벤트(`OnInventoryOpened`) NYH 가 발행 + 특정 아이템 하이라이트 API |
| 6 적 배치/행동 | 적 프리팹·스폰 = KKH | 튜토리얼용 적 1마리 배치 방식(씬 직접 배치 vs 스포너) + 열쇠 드롭(적 처치 시 vs 바닥 KeyPickup 고정 배치) |

> B 항목은 사용자가 팀원과 협의 후 지시하면 그때 구현. 그 전까지 A 항목으로 튜토리얼 골격(씬·컨트롤러·UI·단계 1·3·5·7·8·9·10)을 완성해 두면, B 단계는 조건 검사만 끼워 넣으면 됨.

## 4. 진입/종료 흐름

- **진입**: 튜토리얼은 1층 이전. 전용 씬 우선 로드(메인 메뉴 → 튜토리얼 → 1층) 또는 첫 실행 플래그. (게임 시작 흐름은 `FloorTransitionController.generateOnStart` + `RunState.startDepth=1` 구조 — 튜토리얼은 이 앞단.)
- **종료(단계 10)**: 포탈(GridExit) 도달 → 튜토리얼 종료 처리 → **인벤토리/획득물 초기화**(빈손) → 실제 1층 진입(씬 전환 또는 메인 게임 씬 로드 후 `FloorTransitionController` 가 depth=1 생성). 튜토리얼 보상이 본 게임으로 넘어가지 않도록 전환 직전 비움.
- **스킵/재생**: 1회성 — 클리어 시 다시 안 뜨게 PlayerPrefs 플래그(간단). (저장 시스템과 무관한 단일 플래그.)

## 5. 구현 순서 (마일스톤)

- **T1 — 골격**: 튜토리얼 씬 + 고정 맵 배치 + `TutorialController`(단계 인덱스·완료조건 enum) + `TutorialHintUI`(텍스트). 단계 1 동작.
- **T2 — 위치/기존자산 단계**: 단계 3(카메라 팬), 5/7/9(방 이동 트리거), 8(열쇠 상자 + `OnChestOpened`), 10(포탈→1층). `OnKeyAcquired` 추가로 6의 열쇠 판정.
- **T3 — 협의 단계 끼움**: 단계 2(지도 스크롤), 4(인벤토리), 6 적 배치 — 팀원 협의 결과 반영.
- **T4 — 폴리시**: 화살표/하이라이트, 스킵, 1회성 플래그.

## 6. 검증 기준

- [ ] 튜토리얼 씬에서 10단계가 순서대로만 진행(역행/건너뜀 없음).
- [ ] 각 단계 완료 조건 충족 시에만 다음 안내 노출.
- [ ] 단계 8: 열쇠 없이 상자 개방 불가, 열쇠 보유 시 개방 + 다음 단계.
- [ ] 단계 10: 포탈 도달 시 1층으로 정상 진입.
- [ ] A 항목만으로 골격이 끝까지 진행(B 단계는 임시 우회 가능)되는지.
- [ ] 700줄 넘는 클래스 없음.

## 8. 구현 현황 (2026-06-05) — A그룹 골격 코드 완료

코드 선작업(유니티 미사용) 완료분:
- **신규 이벤트**: `GameEvents.OnChestOpened(Vector2Int)`, `GameEvents.OnKeyAcquired(KeyType)` (+ `ClearAllEvents` 리셋).
- **발행 지점**: `GridChest.TryOpen` → `OnChestOpened`, `KeyInventory.Grant` → `OnKeyAcquired`.
- **카메라 팬 감지**: `CameraFollow.HasPannedSinceArm` (ArmSafeRoom 시 리셋, 줌/팬 입력 시 true) — 단계 3 판정.
- **신규 파일**:
  - `Assets/KDU/01.Scripts/Tutorial/TutorialController.cs` — 선형 단계 진행. 완료조건 enum(PressAdvanceKey/MapScrollUsed/CameraPanned/InventoryOpened/EnterRegion/DefeatAndGetKey/ChestOpened/ReachExit). 종료 시 인벤토리/열쇠 비우고 1층 씬 로드.
  - `Assets/KDU/01.Scripts/Tutorial/TutorialHintUI.cs` — TMP 안내 패널(ShowHint/HideHint).
- **B그룹 임시 처리**: MapScrollUsed / InventoryOpened 는 전용 이벤트가 붙기 전까지 `advanceKey`(기본 Space) 수동 진행. `debugSkipKey` 로 콘텐츠 없이 전체 흐름 강제 점검 가능.

### 8.1 탐색/숨겨진 문 발견 (단계 7)
- **탐색 액션**: `Assets/KDU/01.Scripts/Shared/SearchAction.cs` — `searchKey`(기본 F) → `GameEvents.OnSearchPerformed(center, radius=1)`. 시간 소모 없음(즉시).
- **F 즉시 발견(액티브)**: `GridDoor` 가 `OnSearchPerformed` 구독 → 3×3 내 Secret+Hidden 문 즉시 `Discover()`.
- **패시브 확률 발견**: `GridDoor` 의 기존 3초 인접 발견 제거 → 범위(체비셰프 `passiveDiscoveryRange`, 기본1) 안에 머무는 동안 매초 `passiveDiscoveryChancePerSecond`(기본0.15) 확률 롤(문별 셀 시드 `DungeonRandom.Chance`).
- **발견 이벤트**: `GridDoor.Discover()` → `GameEvents.OnSecretDiscovered(cell)`. 튜토리얼 조건 `SecretDiscovered` 가 이걸로 완료.
- **GridDoor 자가 초기화**: `selfInitInScene`/`sceneKind=Secret`/`sceneHorizontalPassage` — 씬 직접 배치 문이 Start 에서 현재 위치로 Hidden 초기화(MapGenerator 없이). `_initialized` 가드로 절차생성 영향 없음.
- **확장**: 숨겨진 함정(NYH `GridTrap`)은 같은 `OnSearchPerformed`/`OnSecretDiscovered` 패턴으로 편입.

### 8.2 튜토리얼 숨겨진 방 씬 구성 (단계 7)
1. 숨겨진 방 입구 칸에 Wall 타일을 칠한다(봉인).
2. 그 칸에 GridDoor 프리팹 배치 → `selfInitInScene=true`, `sceneKind=Secret`, `sceneHorizontalPassage`(통로 방향) 설정.
3. steps 에 **7a 발견 단계**: `condition=SecretDiscovered`, hint="문 앞으로 가서 **F** 를 눌러 숨겨진 문을 찾으세요". → F 누르면 문이 드러나며 다음으로.
4. **7b 진입 단계**: `condition=EnterRegion`, `region=Region_SecretRoom`, hint="숨겨진 방으로 들어가세요".
5. 씬에 `SearchAction` 컴포넌트 1개(플레이어 자동 검색) 배치.

남은 작업(유니티 필요 / 협의):
- 전용 튜토리얼 씬 + 고정 맵 배치, `TutorialController`/`TutorialHintUI` 배치, steps 인스펙터 입력(안내문·EnterRegion 콜라이더·killCount), 영역 콜라이더 배치, `floor1SceneName` 지정.
- 씬 의존: `GridSystem`/`TimeSystem`/`CameraFollow`/`GridExit`(포탈) 존재 필요. (GridExit 는 `TimeSystem.Instance.StopTimer` 호출 — 씬에 TimeSystem 필요.)
- B그룹 이벤트 훅(단계 2/4), 적 배치(단계 6) 협의 후 조건 교체.

## 7. 미결 / 협의 대상 요약

1. 지도 스크롤(맵 공개) 아이템 구현 주체 — KDU 신규 vs YJW. (단계 2 선결)
2. 인벤토리 열림/하이라이트 이벤트 — NYH 협의. (단계 4)
3. 튜토리얼 적 배치·열쇠 드롭 — KKH 협의. (단계 6)
4. 튜토리얼 진입 방식 — **확정: 전용 씬** (코어 싱글톤이 전부 `Singleton<T>` 씬 단위 + 사망 재시작이 이미 씬 리로드 → 씬 경계가 곧 리셋. 1층 진입 시 빈손 제약 공짜 충족).
5. GDD 미반영 — 출품 스코프 편입 여부 확정 필요.

---

## 9. 튜토리얼 씬 구성 체크리스트

> 메인 씬을 복제 후 절차생성/런 머신을 들어내고 아래만 남기는 식이 빠름.

### A. 코어 시스템 (씬에 1개씩)
- [ ] **GridSystem** + Floor/Wall/Hazard Tilemap 연결(`Tilemaps`). 거의 모든 오브젝트가 grid↔world·IsWalkable 사용. **필수**
- [ ] **TimeSystem** — 포탈(`GridExit`)이 `TimeSystem.Instance.StopTimer()` 를 **무조건 호출**(null 체크 없음) → 없으면 단계 10에서 NullRef. **필수**
- [ ] **KeyInventory** — 단계 6 열쇠 / 단계 8 RequiresKey 상자. **필수**
- [ ] **VisionSystem** (+ FogRenderer) — 안개/탐험 표시 원하면. 없으면 픽업·상자가 항상 보임(기능상 무해). *권장/선택*
- [ ] ~~MapGenerator~~ — 절차생성 안 하니 **불필요**. 단, 없으면 ① `CameraFollow` 팬 클램프(`MapWorldBounds`) 미작동(맵 밖까지 팬 가능) ② `GridExit` area 애니 스킵. 허용 가능. *선택*
- [ ] ~~RunState~~ — 튜토리얼은 시드/depth 불필요. *선택*

### B. 플레이어 / 카메라 / UI
- [ ] **GridPlayer** prefab — 시작 SafeRoom 중앙 배치.
- [ ] **Main Camera + CameraFollow** — `target`=player, `planningCamera`=카메라, `player` 연결.
- [ ] **인벤토리** (NYH: `InventoryController` + Canvas + `ItemGrid`) — 단계 4 시연 + 단계 6/8 보상 적립. `TutorialController.inventory` 에 연결.
- [ ] HUD(`DebugUI` 등) — HP/시간 표시. *선택*

### C. 고정 맵 (타일맵)
- [ ] Grid + Floor/Wall/Hazard Tilemap 에 4방 레이아웃 직접 그림: 시작SafeRoom─몬스터방─출구방, 몬스터방 아래 숨겨진방.

### D. 배치 오브젝트 (단계 매핑)
- [ ] 몬스터 방: **적 1마리**(KKH prefab — *협의*) + **열쇠**(`KeyPickup`, keyType = 상자와 일치) 또는 적 처치 시 드롭. → 단계 5·6
- [ ] 숨겨진 방: **열쇠 상자**(`GridChest` + `ChestData.RequiresKey=true`, keyType 일치). → 단계 7·8
- [ ] 출구 방: **포탈**(`GridExit`). → 단계 9·10
- [ ] (선택) 시작 SafeRoom: 인벤 시연용 시작 아이템(`ItemPickup`) / 지도 스크롤 아이템(단계 2, *미구현·협의*).

### E. 튜토리얼 전용
- [ ] **TutorialController** — `steps` 입력(단계별 hint·condition·region·killCount), `advanceKey`, `debugSkipKey`, `inventory`, `floor1SceneName`.
- [ ] **TutorialHintUI** — Canvas 안 패널 + TMP_Text, `TutorialController.hintUI` 연결.
- [ ] **영역 콜라이더** — 단계 5/7/9 용 `BoxCollider2D` 영역 오브젝트 3개(몬스터방·숨겨진방·출구방), 각 step.region 에 연결.

### F. 글루 / 주의 ⚠
- [ ] **세이프룸 Arm 주체 부재** — 튜토리얼엔 `FloorTransitionController` 가 없어 시작 시 `CameraFollow.ArmSafeRoom` 을 부를 게 없음 → 단계 3(카메라 팬·`HasPannedSinceArm`)과 플래닝 줌/팬·세이프룸 타이머가 **작동 안 함**. → 시작 시 시작방 셀로 Arm 하는 **작은 부트스트랩 필요**(또는 `TutorialController` 시작에 Arm 추가 — 코드 보강 가능).
- [ ] **Build Settings** — 튜토리얼 씬 + 1층(메인) 씬 둘 다 등록, 시작 씬 순서 지정. `floor1SceneName` 에 1층 씬 이름.
- [ ] 단계 2/4 는 현재 `advanceKey` 수동 진행, 적 없으면 `debugSkipKey` 로 전체 흐름 점검.
