# 적 스포너 리팩토링 계획 (Enemy Spawner Refactoring Plan)

이 계획은 `MAP_GENERATION.md` 및 `60sec_GDD_v0.1.docx`에 명시된 기획 사양에 맞게 `EnemySpawner` 및 `RoomSpawnRule`을 조정하기 위해 필요한 변경 사항을 자세히 설명합니다.

## 1. 핵심 변경 사항

### A. 타일 데이터 로드 및 통행 가능 여부 검증
*   **대상 파일**: `RoomSpawnRule.cs` (`GetValidSpawnPositions`)
*   **로직**: 현재 `GetValidSpawnPositions`는 `room.floorCells`와 문 근접성만 확인합니다. 낭떨어지, 벽 또는 통행할 수 없는 타일에 스폰되는 것을 방지하기 위해 `GridSystem.Instance.IsWalkable(pos)`를 조회해야 합니다.
*   **코드 변경**:
    ```csharp
    protected static List<Vector2Int> GetValidSpawnPositions(RoomData room)
    {
        return room.floorCells
            .Where(pos => !IsTooCloseToDoor(pos, room))
            .Where(pos => GridSystem.Instance == null || GridSystem.Instance.IsWalkable(pos))
            .Where(pos => TurnManager.Instance == null || TurnManager.Instance.GetEntityAt(pos) == null)
            .ToList();
    }
    ```

### B. 방 크기 카테고리 기반 스폰 수량 스케일링
*   **대상 파일**: `RoomSpawnRule.cs` (`NormalRoomSpawnRule`, `ExitRoomSpawnRule`)
*   **로직**: `room.sizeCategory`를 기반으로 현재 `SpawnTier`에 정의된 스폰 수(`minCount` / `maxCount`)에 배수를 곱합니다.
    *   `SizeCategory.Normal` (5x5 ~ 7x7): `1.0x` 배수
    *   `SizeCategory.Large` (8x8 ~ 12x12): `1.5x` 배수
    *   `SizeCategory.Giant` (12x12 ~ 16x16): `2.0x` 배수
*   **공식**:
    ```csharp
    float sizeMultiplier = room.sizeCategory switch
    {
        SizeCategory.Normal => 1.0f,
        SizeCategory.Large => 1.5f,
        SizeCategory.Giant => 2.0f,
        _ => 1.0f
    };
    ```

### C. Floor Feeling 보정치 지원 (예: Traps Feeling: 몬스터 수 -1)
*   **대상 파일**: `EnemySpawner.cs` 및 `RoomSpawnRule.cs`
*   **로직**:
    1.  `EnemySpawner.cs`에 `SpawnCountModifier` 속성(기본값 `0`)을 추가합니다.
    2.  `RoomSpawnRule.Generate`의 시그니처를 업데이트하여 선택적 매개변수 `int countModifier = 0`을 허용합니다.
    3.  `EnemySpawner.Spawn()`에서 `SpawnCountModifier`를 `rule.Generate` 호출에 전달하고, 메서드 끝에서 `SpawnCountModifier = 0`으로 초기화합니다.
    4.  `NormalRoomSpawnRule` 및 `ExitRoomSpawnRule`에서 `countModifier`를 사용하여 `minCount` 및 `maxCount`를 조정합니다 (`0` 미만으로 떨어지지 않도록 보장).

### D. 스마트 Archer(원거리) 배치 및 폴백 로직
*   **대상 파일**: `RoomSpawnRule.cs` (`NormalRoomSpawnRule`, `ExitRoomSpawnRule`)
*   **로직**:
    *   선택된 몬스터가 `requiresLineOfSight = true`인 경우, `HasStraightLOS(pos)` 조건을 만족하는 남은 `validPosition`이 있는지 확인합니다.
    *   찾은 경우, 해당 위치에 스폰합니다.
    *   **찾지 못한 경우**, 슬롯을 그냥 건너뛰지 않습니다. 방의 스폰 수가 줄어드는 것을 방지하기 위해 해당 티어 풀에서 시야(LOS)가 필요 없는 몬스터를 대신 뽑아 현재 `posIndex`에 스폰하도록 폴백(Fallback) 처리합니다.

### E. 통행 가능한 보스 배치
*   **대상 파일**: `RoomSpawnRule.cs` (`BossRoomSpawnRule`)
*   **로직**:
    *   방의 중앙에서 가장 가까운 **통행 가능한 타일**(`GridSystem.Instance.IsWalkable(cell)`)을 찾습니다. 통행 가능한 타일이 없는 경우, `room.floorCells`에서 가장 가까운 셀로 폴백합니다.

---

## 2. 검증 단계
1.  **코드 컴파일**: Unity에서 모든 변경 사항이 성공적으로 빌드되는지 확인합니다.
2.  **모의 테스트(Mock Tests)**: `TestKKH` 씬에서 `EnemySpawnTester` 컨텍스트 메뉴 테스트를 실행하여 다양한 방 유형을 확인합니다.
3.  **지형 교차 검사**: 몬스터가 낭떨어지 타일 좌표나 벽 위에 스폰되지 않는지 확인합니다.
