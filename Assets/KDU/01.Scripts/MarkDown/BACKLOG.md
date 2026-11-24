# BACKLOG.md — 미룬 항목

> 출품(완료) 범위에서 빠졌거나 개선/확장으로 미룬 작업 모음. **현재 Steam 출시 버전 단계** — 각 항목은 출시 포함 여부를 분류한다 (1인 개발).
> 출시 스코프는 `CLAUDE.md` / `SCOPE_AND_ROADMAP.md` 0절 참조. GDD 기준 명세는 `Stolen_Time_GDD_v1_3.docx`.

---

## 문 시스템 (Door System)

**왜 미루나**: 출품 빌드에선 보스층 Safe Anchor를 **단일 셀 방식**으로 처리하고, 일반 층 `RoomData.doorCells` 는 페인터에서 별도 문 객체로 인스턴스화하지 않는다. 게임 흐름상 큰 영향이 없고 우선순위 낮음.

**언제 만드나**: 일반 층 Safe Room 의 GDD 11.1 동작(**한 번 나가면 재입장 불가**) 을 정식 구현할 때 같이.

**작업 범위 (한 번에 묶어 처리)**:
- `GridDoor` MonoBehaviour — 잠김/열림 상태, 통행 가능 여부, OnEnter 트리거
- 페인터가 `RoomData.doorCells` 위치에 GridDoor prefab 인스턴스화
- Safe Room 입구는 일방통행 (나가면 잠김)
- Special 방 입구는 열쇠 필요 (GDD 6.2.1) — 입구 잠금 + 열쇠 인벤토리 + 1초 소비
- Secret 방 입구는 숨김 (GDD 6.2.2 / 8.6) — 탐색 확률로 발견 시 활성화

**같이 마이그레이션할 것**:
- 보스층 Safe Anchor 를 **flood fill 영역 인식 방식** 으로 전환 가능 (`MAP_GENERATION.md` 1.2 참조). 보스 prefab 안에서 Safe 와 Boss 영역을 문/벽으로 자연스럽게 격리할 수 있음.

---

## 그 외 미룬 항목 (자리표시자 — 채워 나갈 것)

- 영역별 페인터 풀 (Sewer 1~5 / Dungeon 6~10 / Hell 11~15) — `MAP_GENERATION.md` Phase 5
- 더 다양한 빌더 (FigureEight 외) — `MAP_GENERATION.md` Phase 5
- 폴리싱 (이펙트, 사운드 디테일 등) — 출시 전 폴리싱 스프린트
- **아티팩트·룬 장비 슬롯** — 확장 계획, 기획 미완 (2026-11). GDD 갱신 후 구현. 코드 쪽 자리는 이미 있음(`EquipmentSlot.Artifact/Rune`, `LeaderboardDetails` 6칸 포맷). 추가 시 `ScoreDetailPopup` 프리팹 `equipCells`에 5·6번 칸(아티팩트, 룬)만 붙이면 기록 상세에 표시됨 — 지금은 4칸이라 해당 장비는 표시 생략

## 출시 스코프로 이동한 항목

- 일일 챌린지 / Steam 랭킹 (GDD 13장 P2 → 출시 범위) — `SCOPE_AND_ROADMAP.md` 0절
