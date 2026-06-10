# 스페셜 몬스터 룸에서의 골램 스폰 구현 계획 (상세 가이드)

스페셜 룸(`RoomType.Special`) 중 몬스터 방(`SpecialRoomKind.Monster`)을 식별하여, 엘리트 몬스터 배치 방식과 유사하게 특정 조건의 골램(Golem) 몬스터를 확정적으로 배치하도록 스폰 시스템을 개선합니다.

---

## Proposed Changes

### [EnemySystem]

#### [MODIFY] [EnemySpawner.cs](file:///d:/kkh/OHP_Project/Assets/KKH/02. Scripts/EnemySystem/EnemySpawner.cs)

직접 수정하셔야 할 위치와 상세 코드 예시입니다.

---

### 1. `Spawn` 메서드 수정 (약 133 ~ 145행 영역)

* **목적**:
  1. 골램을 스페셜 몬스터 룸에 스폰하는 신규 단계 추가
  2. 일반 스폰 루프에서 스페셜 몬스터 룸은 스킵하도록 필터 조건 추가 (골램 단독 배치 목적)

```csharp
// [기존 코드 133라인 근처]
// 1단계: 엘리트 몬스터 Exit Room 확정 배치 (단독)
SpawnEliteInExitRoom(rooms, currentConfig, rng);

// ─── [추가할 코드] ───
// 1.5단계: 스페셜 몬스터 룸에 골램 확정 배치 (단독)
SpawnGolemInSpecialMonsterRoom(rooms, currentConfig, rng);
// ─────────────────────

// 2단계: 방별 일반 스폰 (Exit Room은 엘리트 단독이므로 스킵)
foreach (var room in rooms)
{
    if (room.type == RoomType.Tunnel) continue; // 복도는 스킵
    if (room.type == RoomType.Exit) continue;   // Exit Room은 엘리트 단독
    
    // ─── [추가할 코드] ───
    // 스페셜 몬스터 룸은 골램 전용 방이므로 일반 스폰 루프에서 제외
    if (room.type == RoomType.Special && room.specialKind == SpecialRoomKind.Monster) continue;
    // ─────────────────────

    // 방 타입에 맞는 RoomSpawnRule 결정
    RoomSpawnRule rule = ResolveRule(room.type);
    ...
```

---

### 2. 신규 스폰 메서드 `SpawnGolemInSpecialMonsterRoom` 구현 (약 311라인 근처)

* **목적**: `SpawnEliteInExitRoom` 메서드 바로 아래에 골램 스폰을 담당하는 신규 private 메서드를 정의합니다.
* **로직**:
  1. `currentConfig.prefabMappings`에서 `displayName == "Golem"`인 `EnemyData`를 조회합니다.
  2. 방 목록에서 `RoomType.Special` 및 `specialKind == SpecialRoomKind.Monster`인 스페셜 방을 찾습니다.
  3. 방 중앙 좌표에 가까운 Walkable 타일을 탐색해 골램을 1마리 스폰합니다.

```csharp
        /// <summary>
        /// 스페셜 룸 중 몬스터 방(SpecialRoomKind.Monster)을 찾아 골램을 1마리 스폰합니다.
        /// </summary>
        private void SpawnGolemInSpecialMonsterRoom(List<RoomData> rooms, FloorSpawnConfig config, DungeonRandom rng)
        {
            if (config == null || config.prefabMappings == null)
            {
                Debug.LogWarning("[SpawnGolemInSpecialMonsterRoom] 설정 또는 프리팹 매핑이 null입니다.");
                return;
            }

            // 1. 프리팹 매핑에서 displayName이 "Golem"인 EnemyData 검색
            EnemyData golemData = null;
            foreach (var mapping in config.prefabMappings)
            {
                if (mapping.enemyData != null && mapping.enemyData.displayName == "Golem")
                {
                    golemData = mapping.enemyData;
                    break;
                }
            }

            if (golemData == null)
            {
                // 현재 층에 골램이 배치되지 않는 경우(예: 골램이 미등록된 층) 조용히 리턴하거나 로그를 남깁니다.
                Debug.Log("[SpawnGolemInSpecialMonsterRoom] 이 층에는 골램 프리팹 매핑이 없습니다.");
                return;
            }

            // 2. 스페셜 몬스터 방 탐색 및 골램 스폰
            foreach (var room in rooms)
            {
                if (room.type == RoomType.Special && room.specialKind == SpecialRoomKind.Monster)
                {
                    // 3. 방 중앙과 가장 가까운 Walkable 셀 탐색
                    Vector2Int center = new Vector2Int(
                        room.bounds.x + room.bounds.width / 2,
                        room.bounds.y + room.bounds.height / 2
                    );

                    Vector2Int spawnPos = center;
                    bool foundWalkable = false;
                    float minDist = float.MaxValue;

                    foreach (var cell in room.floorCells)
                    {
                        bool isWalkable = GridSystem.Instance == null || GridSystem.Instance.IsWalkable(cell);
                        if (isWalkable)
                        {
                            float dist = (cell - center).sqrMagnitude;
                            if (dist < minDist)
                            {
                                minDist = dist;
                                spawnPos = cell;
                                foundWalkable = true;
                            }
                        }
                    }

                    // 만약 Walkable 셀을 찾지 못했다면 첫 번째 바닥 타일을 폴백으로 사용
                    if (!foundWalkable && room.floorCells.Count > 0)
                    {
                        spawnPos = room.floorCells[0];
                    }

                    // 4. 골램 소환
                    var spawnEntry = new EnemySpawnEntry(golemData, spawnPos);
                    InstantiateOne(spawnEntry, config);

                    Debug.Log($"EnemySpawner: 스페셜 몬스터 룸({room.specialKind})에 골램 '{golemData.displayName}' 배치 완료 ({spawnPos}).");
                }
            }
        }
```

---

## Verification Plan

### Manual Verification

1. `MapGenerator`가 생성한 방 중에서 `RoomType.Special` 및 `specialKind == SpecialRoomKind.Monster`를 지닌 방이 존재하는지 기획 데이터나 디버그 로그로 확인합니다.
2. 해당 방의 격자 좌표 내에 골램 프리팹이 월드 좌표로 정확히 변환되어 단 1마리 스폰되는지 씬(Scene) 뷰와 하이어라키(Hierarchy) 창에서 확인합니다.
3. 골램이 스폰된 방에는 일반 몬스터가 중복으로 겹쳐 스폰되지 않는지 교차 검증합니다.
