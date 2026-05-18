# 🛡️ 60초 던전: 적 스폰 테스트 가이드 (TestKKH 씬)

이 문서는 새로 구현한 `RoomSpawnRule` 로직이 올바르게 동작하는지 `TestKKH` 씬에서 테스트하기 위한 환경 구성 가이드입니다.

---

## 📋 1단계: Enemy Spawn Table 세팅 (데이터 준비)

먼저 적들이 어떤 확률과 수량으로 스폰될지 결정하는 데이터(SO)를 만들어야 합니다.

1. **Unity Project 창**에서 `Assets/KKH/SOData` 폴더로 이동합니다. (폴더가 없다면 생성해주세요)
2. 빈 공간에 **우클릭 -> `Create > 60s Dungeon > Enemy Spawn Table`** 을 클릭하여 `TestSpawnTable` 이라는 이름으로 생성합니다.
3. 생성된 `TestSpawnTable` 파일을 클릭하고 **Inspector 창**을 확인합니다.
4. `Tiers` 배열 크기를 **4** 정도로 늘려줍니다 (이 값은 Safe Room으로부터의 거리 1~4에 대응됩니다).
5. 각 Tier 항목을 열어서 `Pool` 배열을 늘려 적 데이터를 추가합니다:
   - `Enemy Data` 칸에는 `Assets/KDU/02.SOData/`에 있는 `Tracker.asset`이나 `Archer.asset`을 끌어다 넣습니다.
   - `Min Count`, `Max Count`를 조절하여 해당 거리에서 몇 마리의 적이 나올지 설정합니다.
   - ⚠️ **중요:** Archer처럼 직선 시야(LOS)가 확보되어야 스폰할 수 있는 몬스터는 `Requires Line Of Sight` 체크박스를 **반드시 체크**해주세요.

---

## 🗺️ 2단계: TestKKH 씬 맵 환경 구성

스폰 로직은 `GridSystem`을 사용하여 맵 위를 판별하므로 타일맵 환경이 필요합니다.

1. `Assets/KKH/Scene/TestKKH.unity` 씬을 엽니다. (없다면 새로 만들어주세요)
2. **Hierarchy 창** 빈 공간에 **우클릭 -> `2D Object > Tilemap > Rectangular`** 를 클릭해 타일맵을 생성합니다. 
   - 생성된 `Tilemap` 오브젝트의 이름을 `FloorTilemap`으로 변경합니다.
3. 빈 GameObject를 하나 생성하고 이름을 `GridSystem`으로 변경합니다.
4. `GridSystem` 오브젝트에 KDU 코어의 `GridSystem.cs` 스크립트를 컴포넌트로 추가합니다.
5. 추가한 `GridSystem` 컴포넌트의 `Floor Tilemap` 슬롯에 아까 만든 `FloorTilemap` 오브젝트를 드래그해서 연결해 줍니다.
6. 상단 메뉴의 `Window > 2D > Tile Palette`를 열고 아무 타일이나 선택하여 `FloorTilemap`에 **5x5 크기 정도의 바닥**을 칠해줍니다. (바닥이 없으면 스폰할 곳이 없다고 판단합니다)

---

## ⚙️ 3단계: EnemySpawnTester 스크립트 부착 및 매핑

테스트 코드를 씬에 올리고, 프리팹을 연결합니다.

1. Hierarchy 창에 빈 GameObject를 하나 생성하고 이름을 `Tester`로 변경합니다.
2. `Assets/KKH/Scripts/EnemySystem/`에 있는 **`EnemySpawnTester.cs`** 스크립트를 `Tester` 오브젝트에 드래그하여 붙입니다.
3. `Tester` 오브젝트를 클릭하고 **Inspector 창**을 다음과 같이 세팅합니다:
   - **Spawn Table:** 1단계에서 만든 `TestSpawnTable`을 연결합니다.
   - **Prefab Mappings:** 배열 크기를 늘린 후, **Enemy Data**(`Tracker.asset` 등)와 **실제 스폰될 Prefab**(에셋이 없다면 임시로 만든 큐브나 캡슐 2D 오브젝트의 프리팹)을 짝지어 줍니다.
   - **Test Boss Data:** 보스 룸 테스트를 하려면 여기에 보스 Data를 연결합니다 (보스 데이터가 없다면 비워둬도 무방합니다).

---

## 🚀 4단계: 테스트 실행 방법

모든 준비가 끝났습니다! 에디터 상태에서 바로 테스트해 볼 수 있습니다.

1. Unity 상단의 **Play(▶)** 버튼을 눌러 게임을 실행합니다. (Play를 안 누른 에디터 모드에서도 작동은 하지만 Play 모드 권장)
2. Hierarchy에서 `Tester` 오브젝트를 클릭합니다.
3. Inspector 창에 있는 **`Enemy Spawn Tester (Script)` 이름 텍스트 위에서 마우스 우클릭**을 합니다.
4. 컨텍스트 메뉴 맨 아래에 있는 **`스폰 테스트 실행 (가상 방 생성 및 적 스폰)`** 을 클릭합니다.
5. **Scene 뷰**를 확인해 보세요!
   - 타일이 깔린 위치 내에서, 입구(기본적으로 상단 중앙으로 가상 설정됨)를 피해서 몬스터 프리팹들이 스폰됩니다.
   - 콘솔 창(Console)에도 스폰 완료 로그가 찍힙니다.

> 💡 **다양한 상황 테스트 팁:**
> `Tester` 인스펙터 창에서 `Test Room Type`을 `Safe`, `Exit`, `Boss` 등으로 변경하거나, `Distance From Safe` 수치를 올린 다음 다시 우클릭 -> `스폰 테스트 실행`을 클릭해보세요. 
> 설정한 룸 타입과 난이도(거리)에 따라 스폰되는 몬스터 종류와 수가 실시간으로 달라지는 것을 확인할 수 있습니다!
