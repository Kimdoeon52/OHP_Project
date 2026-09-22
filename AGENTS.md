# AGENTS.md — Stolen Time

**이 저장소의 에이전트 지침은 `CLAUDE.md` 다. 작업 전에 그것부터 읽을 것.**

여기에 규칙을 복사해 두지 않는다. 사본이 생기면 원본과 어긋나고, 어긋난 사본을 읽은 에이전트가 낡은 규칙대로 코드를 짠다. 아래 두 줄만 예외 — 어기면 되돌리기 어려운 항목이라 중복을 감수한다.

- **GPLv3 격리.** 녹픽던/Shattered Pixel Dungeon 은 GPLv3. 빌드에 GPL 에셋·코드 단 한 줄도 금지. 디자인만 참고. 임시 GPL 에셋은 `_DoNotShip/` 에 격리하고 빌드 스크립트에서 제외.
- **디자인 수치의 원본은 GDD 하나.** 저장소 안 작업 사본은 `Assets/KDU/01.Scripts/MarkDown/Stolen_Time_GDD_v1_3_clean.md`. 수치·규칙을 다른 문서에 복붙하지 말고 "GDD N장 참조" 로 가리킬 것. 충돌하면 항상 GDD.

## 문서 지도

| 문서 | 다루는 것 |
| --- | --- |
| `CLAUDE.md` | 에이전트 지침 본체 — 아키텍처 결정·금지 패턴·스코프 락·점검표 |
| `Assets/KDU/01.Scripts/MarkDown/Stolen_Time_GDD_v1_3_clean.md` | 디자인 단일 진실 공급원 (수치·규칙) |
| `Assets/KDU/01.Scripts/MarkDown/ARCHITECTURE.md` | 티어 구분·분리 기준·폴더 트리·출품 당시 팀 분담 기록 |
| `Assets/KDU/01.Scripts/MarkDown/SCOPE_AND_ROADMAP.md` | 스코프 락 상세·마일스톤·보스 폴백 전략 |
| `Assets/KDU/01.Scripts/MarkDown/MAP_GENERATION.md` | 맵 생성 시스템 명세 (맵 작업 시 필독) |
| `Assets/KDU/01.Scripts/MarkDown/PHASE_3_5_PLAN.md` | Phase 3~5 단계별 계획 |
| `Assets/KDU/01.Scripts/MarkDown/SHOP_PLAN.md` · `TUTORIAL_PLAN.md` | 상점·튜토리얼 구현 계획 (계획만, 미구현) |
| `Assets/KDU/01.Scripts/MarkDown/BACKLOG.md` | 미룬 아이디어 |

---

이 파일은 원래 출품 당시(5인 팀·4개월·구 프로젝트명 "60초 던전") 기준 442줄짜리 전체 지침서였다. 출품 종료와 1인 개발 전환으로 내용 대부분이 낡아 포인터로 축약했다. 옛 내용이 필요하면 `git log --follow AGENTS.md` 로 꺼낼 것.
