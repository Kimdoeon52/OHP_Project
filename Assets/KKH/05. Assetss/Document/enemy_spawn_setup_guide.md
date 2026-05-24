# 시연용 스테이지 적 스폰 및 에셋 설정 가이드

본 문서는 리팩토링된 적(Enemy) 시스템을 시연용 스테이지에서 스폰하기 위해 Unity 에디터 상에서 설정해야 하는 상세 가이드를 제공합니다.

---

## [Step 1] 몬스터 프리팹(Prefab) 생성 및 설정
몬스터의 실체인 **GameObject 프리팹**에 C# 컴포넌트와 관련 데이터를 할당해야 합니다.

1. 빈 게임 오브젝트를 생성한 뒤 이름을 설정합니다 (예: `Troll`).
2. 각 몬스터 타입에 맞는 **C# 스크립트 컴포넌트**를 부착합니다 (예: `EnemyTroll`).
3. 인스펙터 창에서 아래 항목을 꼭 연결해야 합니다:
   * **Data**: `Assets/KKH/04. SOData/Enemy/`에 있는 해당하는 **`EnemyData` SO**를 할당합니다 (예: `Troll.asset`).
   * **Telegraph Prefab**: 바닥에 공격 경고를 띄워주는 `TelegraphIndicator` 프리팹을 할당합니다.
   * **Slow Debuff Data** *(거미인 `EnemySpider` 프리팹만 해당)*: `Cripple_2t_1p.asset`을 할당합니다.
4. 설정을 마친 오브젝트를 `Assets/KKH/03. Prefab/` 폴더에 드래그하여 **프리팹(Prefab)으로 저장**합니다.

---

## [Step 2] 스폰 테이블(Enemy Spawn Table SO) 설정
스폰 테이블은 Safe Room(시작 방)과의 거리별로 어떤 적이 어떤 확률(가중치)로 나올지 결정합니다.

1. `Assets/KKH/04. SOData/` 폴더 내 우클릭 $\rightarrow$ **Create $\rightarrow$ 60s Dungeon $\rightarrow$ Enemy Spawn Table**을 선택해 테이블 에셋을 만듭니다. (예: `DemoSpawnTable.asset`)
2. 인스펙터에서 **Tiers** 배열을 생성해 층/방의 거리에 맞는 난이도 곡선을 구성합니다.
   * **Tiers[0] (거리 1)**: 시작 방과 바로 붙어있는 방들의 설정. `Rat`, `Spider`, `Bat` 위주로 등록합니다.
   * **Tiers[1] (거리 2)**: `Skeleton_NoShield`, `GoblinThief` 위주로 등록합니다.
   * **Tiers[2] (거리 3 이상)**: `Skeleton_Shield`, `Troll`, `Snake` 등 강력한 적 위주로 가중치를 부여합니다.
   * 각 티어마다 **방당 스폰될 마리 수(Min/Max Count)**와 **Elite 몬스터 전환 확률(Elite Chance)**을 지정할 수 있습니다.
   * *Tip: 마법사 스켈레톤처럼 사수가 배치될 항목은 `Requires Line Of Sight` 체크를 활성화하여 벽에 막히지 않는 직선 타일에 배치되도록 유도합니다.*

---

## [Step 3] 층 스폰 설정(Floor Spawn Config SO) 설정
스폰 테이블과 몬스터 프리팹을 하나로 연결해 주는 다리 역할을 하는 설정 파일입니다.

1. `Assets/KKH/04. SOData/` 폴더 내 우클릭 $\rightarrow$ **Create $\rightarrow$ 60s Dungeon $\rightarrow$ Floor Spawn Config**를 선택해 에셋을 만듭니다. (예: `DemoFloorSpawnConfig.asset`)
2. 인스펙터에서 아래 값을 채워줍니다:
   * **Spawn Table**: 위에서 만든 `DemoSpawnTable.asset`을 할당합니다.
   * **Prefab Mappings**: 몬스터의 **`EnemyData` SO**와 방금 [Step 1]에서 저장한 **프리팹(Prefab)**을 1:1로 짝지어 목록에 모두 등록해 줍니다.
     ```
     [Rat.asset]             <---> [PlagueRat Prefab]
     [Spider.asset]          <---> [Spider Prefab]
     [Skeleton_Shield.asset] <---> [Skeleton Prefab] (이하 동일 프리팹 사용 가능)
     [Troll.asset]           <---> [Troll Prefab]
     ... (시연에 사용할 몬스터 매핑)
     ```

---

## [Step 4] EnemySpawner 컴포넌트 셋팅 및 실행
마지막으로 씬 내에 있는 스파우너에 해당 설정을 등록합니다.

1. 씬에 배치되어 있는 `EnemySpawner` 게임 오브젝트를 선택합니다.
2. 인스펙터에서 **Floor Configs** 리스트에 새 요소를 추가하고 범위를 입력합니다.
   * **Min Depth**: 1
   * **Max Depth**: 1 (시연용 단일 스테이지가 1층 취급일 때)
   * **Config**: 위에서 만든 `DemoFloorSpawnConfig.asset`을 드래그해서 할당합니다.

#### 💡 실행 및 테스트
* **에디터 뷰 테스트**: 게임 실행 없이 수동으로 배치 상태를 테스트하고 싶다면, `EnemySpawner` 컴포넌트의 우상단 톱니바퀴(컨텍스트 메뉴) $\rightarrow$ **타일맵 기반 적 자동 스폰(SpawnFromTilemap)**을 클릭하면 씬 상에 즉시 몬스터들이 배치됩니다.
* **실제 인게임 실행**: 게임이 시작되면 `EnemySpawner`가 격자 타일맵을 자동 분석하여 스폰 설정에 기재된 몬스터들을 무작위 범위 내에 자동 스폰시킵니다.
