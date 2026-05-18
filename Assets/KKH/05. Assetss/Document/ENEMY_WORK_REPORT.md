# 몬스터 시스템 작업 현황 및 에디터 설정 가이드

본 문서는 **60초 던전** 프로젝트의 Enemy 파트 작업 결과와 유니티 에디터에서의 설정 방법을 정리한 리포트입니다.

---

## 1. 주요 작업 완료 현황

### ① 시스템 안정화 및 오류 해결

* **네임스페이스 충돌 해결**: 존재하지 않는 `KDU.Grid` 참조를 제거하고 전역 네임스페이스를 활용하도록 수정하여 빌드 오류를 해결했습니다.
* **데이터 타입 교정**: `Vector2Int?` (nullable) 타입을 적용하여 텔레그래프 및 공격 판정 로직의 런타임 에러 가능성을 제거했습니다.

### ② 신규 몬스터 클래스 구현

* **`GridEnemyWraith` (망령)**:
  * **특징**: 벽을 무시하고 플레이어를 추적합니다.
  * **로직**: `IsWalkable` 체크를 생략하여 지형지물을 통과하며, 시야 밖에서도 플레이어를 감지할 수 있도록 설계되었습니다.
* **`GridEnemyCrawler` (크롤러)**:
  * **특징**: 한 턴에 2칸을 이동하는 고속 몬스터입니다.
  * **로직**: '논리-연출 분리' 방식을 적용했습니다. 격자 상의 위치는 즉시 이동하지만, 유저 눈에는 0.5초 간격으로 이동하는 부드러운 애니메이션을 제공합니다.

---

## 2. 유니티 에디터 설정 가이드

새로운 몬스터를 게임에 배치하기 위해 다음 순서로 설정이 필요합니다.

### STEP 1: EnemyData(SO) 생성 및 설정

1. `Project` 창에서 우클릭 -> `Create` -> `ScriptableObject` -> `EnemyData`를 선택해 새 파일을 만듭니다. (파일명을 `EnemyData_Wraith`, `EnemyData_Crawler` 등으로 지정)
2. **인스펙터 설정**:
    * **ID**: 유일한 문자열 (예: `wraith_01`)
    * **HP/Damage**: `Wraith`는 벽 통과 능력이 강력하므로 일반 적보다 낮게, `Crawler`는 이동이 빠르므로 적절한 수치로 설정합니다.
    * **Vision Range**: 적이 플레이어를 인지하기 시작하는 거리입니다.
    * **Sprite**: 적의 외형 이미지를 할당합니다.

### STEP 2: 몬스터 프리팹(Prefab) 생성

1. 빈 오브젝트를 생성하고 이름을 `Enemy_Wraith` 등으로 지정합니다.
2. **컴포넌트 추가**: `SpriteRenderer`와 해당 스크립트(`GridEnemyWraith` 또는 `GridEnemyCrawler`)를 추가합니다.
3. **참조 할당**:
    * **Data**: 위에서 만든 `EnemyData` SO를 드래그하여 넣습니다.
    * **Telegraph Prefab**: `Assets/KDU/03.Prefab/` 폴더에 있는 `TelegraphIndicator` 프리팹을 할당합니다.
4. 작업이 끝나면 이 오브젝트를 `Assets/KKH/03.Prefab/` 폴더로 드래그하여 프리팹화합니다.

### STEP 3: 스폰 테이블(Spawn Table) 등록

1. 현재 사용 중인 `EnemySpawnTable` ScriptableObject를 찾습니다.
2. `Normal Room` 또는 `Exit Room` 등의 티어 리스트에 새로 만든 프리팹을 등록합니다.
    * **Weight**: 등장 확률 (Wraith는 희귀하므로 낮은 가중치 추천)
    * **Requires Line Of Sight**: Wraith는 벽을 통과하므로 `false`로 두는 것이 좋습니다.

---

## 3. 향후 작업 권장 사항 (`task.md` 기반)

* [ ] **Crawler 세부 튜닝**: 현재 0.5f 딜레이가 게임 템포에 적절한지 테스트 후 조정.
* [ ] **보상 시스템 연동**: 몬스터 등급별로 `timeBonus`가 정상적으로 `TimeSystem`에 더해지는지 확인.
* [ ] **이펙트 추가**: Wraith가 벽을 통과할 때의 투명도 처리나 Crawler의 잔상 효과 추가.
