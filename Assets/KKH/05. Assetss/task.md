# 60초 던전 적(Enemy) 파트 TODO 리스트

GDD v1.0을 바탕으로 적(Enemy) 파트 담당자가 수행해야 할 작업 목록입니다.

## 1. 기반 시스템 및 스폰 규칙 (RoomSpawnRule)

- `[X]` **추상 클래스 `RoomSpawnRule` 기반 방 타입별 스폰 규칙 구현** (P0)
  - `[X]` `SafeRoomSpawnRule`: 적 스폰 금지
  - `[X]` `NormalRoomSpawnRule`: Safe Room으로부터의 그래프 거리 기반 난이도 곡선 스폰
  - `[X]` `ExitRoomSpawnRule`: 강한 적 + Elite 확정 스폰
  - `[X]` `BossRoomSpawnRule`: 보스 1체 스폰
- `[X]` **공통 스폰 제약 조건 구현**
  - `[X]` 입구 인접 셀(1칸 거리) 스폰 금지 (즉사 방지)
  - `[X]` Archer는 4방향 직선 LOS 확보 셀에만 배치
- `[X]` **거리 기반 난이도 곡선(Safe 거리 1~4) 데이터 연동**

## 2. 몬스터 추가 및 확장 (우선순위별)

- `[ ]` **신규 몬스터 클래스 구현 (Area 2, 3용)** (P2 확장)
  - `[ ]` `Crawler` (고속 이동)
  - `[X]` `Wraith` (벽 통과)
  - `[ ]` `Phantom` (사망 시 분열)
  - `[ ]` `Hollow King's Guard` (고체력)
  - `[ ]` Elite 변종 (Exit Room 인접 배치용)
- `[ ]` **보스 3종 패턴 및 동작 구현** (P1, 최소 Area 1 보스는 P0)
  - `[ ]` Area 1 보스: 광맥의 군주 (Lord of the Veins)
  - `[ ]` Area 2 보스: 망령 군주 (Wraith Sovereign)
  - `[ ]` Area 3 최종 보스: 심연의 화신 (Avatar of the Abyss)

## 3. 적 처치 보상 (시간 보너스) 시스템 연동

- `[ ]` **몬스터 등급별 처치 시간 보너스 이벤트 연동**
  - `[ ]` 일반 (Common, Tracker/Archer 등): +3초 (Safe 인접/일반방)
  - `[ ]` 엘리트 (Elite, Tracker Captain 등): +5초
  - `[ ]` 미니 보스: +10초
  - `[ ]` 보스: 페이즈 전환 시 +5초, 처치 시 +30초 (다음 층 캐리 안 됨)

## 4. UI/UX 및 텔레그래프 시스템 보강 (P1)

- `[ ]` **Archer 사격 텔레그래프 보강**: 사격 직전 1턴 빨간 점선으로 강조
- `[ ]` **Tracker 이동 텔레그래프 보강**: 도착 셀 그림자 외 이동 방향 화살표 추가 (제안)
- `[ ]` **보스 광역기 텔레그래프 구현**: 광역 위협 셀에 빨간 영역 페이드인 (P1)

## 5. 기존 구현체 호환성 및 테스트

- `[ ]` 기존 `_Prototype`의 `GridEnemyBase`, `Tracker`, `Archer` 코드를 정식 구조(`_Game/Enemies`)로 마이그레이션 또는 확장
- `[ ]` 경험치(EXP) 획득 밸런싱 연동 (Common +5, Elite +12, Boss +50 등)
- `[ ]` 냉기 상태이상(`서리 포션`)에 따른 이동속도/공격속도 둔화 로직 연동
