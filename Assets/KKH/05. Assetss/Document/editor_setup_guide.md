# 적 스폰 시스템 에디터 연동 및 설정 가이드

본 문서는 리팩토링된 [FloorSpawnConfig](file:///d:/kkh/OHP_Project/Assets/KKH/02.%20Scripts/EnemySystem/FloorSpawnConfig.cs) 및 [EnemySpawner](file:///d:/kkh/OHP_Project/Assets/KKH/02.%20Scripts/EnemySystem/EnemySpawner.cs) 시스템을 유니티 에디터에서 설정하고 연동하는 절차를 설명함.

---

## 1단계: FloorSpawnConfig SO 생성 및 설정

각 층 구간(예: 하수구 1~5층, 던전 6~10층 등)에 해당하는 스폰 규칙과 리소스를 담을 ScriptableObject를 생성해야 함.

1. **SO 생성**:
   - 프로젝트 뷰(Project window)에서 우클릭 $\rightarrow$ **Create** $\rightarrow$ **60s Dungeon** $\rightarrow$ **Floor Spawn Config**를 선택하여 에셋을 생성함.
2. **층 범위 설정 (적용 키값)**:
   - **Min Depth / Max Depth**: 이 설정이 작동할 최소/최대 층 깊이(depth)를 정수값으로 입력함. (예: `Min: 1`, `Max: 5`)
   - **Specific Depths** (선택 사항): 특정 층에서만 특별히 이 테이블을 쓰고 싶다면 층 번호 리스트를 직접 지정할 수 있음. 여기에 값이 있으면 Min/Max 범위보다 우선하여 일치 여부를 검사함.
3. **난이도 설정 테이블 연결**:
   - 방 종류별 가중치 및 스폰 몬스터 마릿수를 규칙화한 **EnemySpawnTable** SO를 드래그 앤 드롭으로 할당함.
4. **몬스터 프리팹 매핑 (Prefab Mappings)**:
   - 이 구역에서 등장할 몬스터의 [EnemyData](file:///d:/kkh/OHP_Project/Assets/KDU/01.Scripts/Data/EnemyData.cs) SO와 실제 하이러키에 소환될 **Unity 프리팹(GameObject)**의 쌍을 1:1로 매핑하여 리스트에 등록함.

---

## 2단계: EnemySpawner 컴포넌트 연결

실제 게임 내에서 몬스터 스폰을 실행하는 [EnemySpawner](file:///d:/kkh/OHP_Project/Assets/KKH/02.%20Scripts/EnemySystem/EnemySpawner.cs)에 위에서 만든 설정 파일들을 연동해야 함.

1. **스포너 오브젝트 선택**:
   - 하이러키(Hierarchy) 창에서 `EnemySpawner` 스크립트가 붙어 있는 게임 오브젝트를 선택함.
2. **FloorConfigs 리스트 등록**:
   - 인스펙터의 **Floor Configs** 리스트에 1단계에서 생성한 **FloorSpawnConfig SO 에셋**들을 드래그 앤 드롭으로 직접 등록함. (이제 복잡한 구조체 매핑 없이 SO 에셋 자체만 배열에 넣어주면 됨)

> [!TIP]
> **스폰 탐색 매커니즘**
> - 현재 스테이지 층수가 `currentDepth`로 주어지면, `floorConfigs` 목록의 첫 번째 에셋부터 순서대로 `IsDepthMatching(currentDepth)`를 실행함.
> - 먼저 조건에 만족하는(매칭되는) 첫 번째 SO 설정을 기준으로 해당 층의 모든 적을 소환함.

---

## 3단계: 에디터 테스트 및 검증 기능

### 1. 타일맵 기반 적 자동 스폰 (ContextMenu)
* 인게임 테스트 중 또는 에디터 수동 테스트 시, `EnemySpawner` 컴포넌트의 우측 상단 톱니바퀴 아이콘을 클릭한 뒤 **"타일맵 기반 적 자동 스폰" (SpawnFromTilemap)**을 실행하면 현재 그려진 GridSystem의 이동 가능한 타일 위에 임시 1층 기준 규칙으로 적들이 자동 배치됨.

### 2. 가상 방 스폰 테스트 (EnemySpawnTester)
* 맵 전체를 빌드하지 않고 방 한 개당 몬스터가 기획대로 스폰되는지 확인하려면 [EnemySpawnTester](file:///d:/kkh/OHP_Project/Assets/KKH/02.%20Scripts/EnemySystem/EnemySpawnTester.cs) 컴포넌트를 사용함.
* **사용법**: 
  1. 가상 테스트 오브젝트에 `EnemySpawnTester`를 추가함.
  2. 테스트할 `SpawnTable`, `PrefabMappings`, 가상의 방 크기 및 좌표를 설정함.
  3. 컴포넌트 우클릭 $\rightarrow$ **"스폰 테스트 실행 (가상 방 생성 및 적 스폰)"**을 클릭하면 해당 방 크기에 맞춰 몬스터들이 에디터 상에 즉시 소환되어 정상 스폰되는지 시각적으로 검증 가능함.
