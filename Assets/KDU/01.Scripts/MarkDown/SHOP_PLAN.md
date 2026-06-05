# SHOP_PLAN.md — 상점 (Safe Room 상점 모드) 구현 계획

> 상태: **계획만** (코드 미구현). 승인 후 단계별 구현.
> 디자인 수치 원본은 **GDD 11.4** (Safe Room 두 가지 모드 / 상점 표). 이 문서는 수치를 복붙하지 않고 **구조·흐름**만 정의한다. 가격·확률·개수는 전부 `ShopTableData` SO 에서 GDD 11.4 표를 보고 인스펙터로 입력 → 코드/문서에 하드코딩 X.

---

## 1. 목표 (GDD 11.4)

- **1 / 6 / 11층** Safe Room 은 "상점 모드" — 상인(상인 NPC)이 등장하고 방 **테두리**에 상품이 진열된다.
- 플레이어는 골드로 상품을 구매한다. 가격·등장확률·개수는 **층(1/6/11)마다 다름** (GDD 11.4 표).
- 상품 종류: 비상 식량 · 지도/감정/정화 스크롤 · 층별 투척무기 세트 · 층별 갑옷 · 0강 반지 · 회복 포션 · 랜덤 포션. (정확한 풀·확률·가격은 GDD 11.4 표)

## 2. 현재 인프라 / 갭

| 필요 | 현황 |
| --- | --- |
| 골드 획득 이벤트 | ✅ `GameEvents.OnGoldDropped(pos, gold)` (적/상자/`GoldPickup` 발행) |
| **골드 잔액(지갑)** | ❌ **구독자 없음 — 잔액을 들고 있는 주체가 없다. 신규 필요.** |
| 골드 HUD | ❌ 없음 |
| 인벤토리 지급 | ✅ `PrototypeRT.InventoryController.TryAddItem(ItemInstance)` (가득 차면 false) |
| ItemInstance 생성 | ✅ 패턴 존재 (`ItemPickup.BuildInstance` — Weapon/Armor/Potion/기타 분기) |
| 인접+상호작용 패턴 | ✅ `GridChest`(인접 머무름), `GoldPickup`/`ItemPickup`(같은 칸 + 키) |
| Safe Room 생성 | ✅ `MapGenerator` → `RoomType.Safe` 1개, `FloorTransitionController` 가 중앙 텔레포트 |
| Safe Room 바닥 아이템 | 🚫 `SafeRoomItemRule` = no-op (상점이 대체 — PHASE_3_5_PLAN 169행) |
| 결정론 RNG | ✅ `MapGenerator.Rng` (층별 `DeriveFloorSeed`) — 상점 생성에 그대로 전달 |

## 3. 아키텍처 원칙 적용 (CLAUDE.md)

- **데이터는 SO**: 상점 상품표 = `ShopTableData` SO 1개. 상품 추가/조정 = 데이터만 수정.
- **통신은 정적 이벤트**: 잔액 변동 → `GameEvents.OnGoldChanged`. HUD·구매 실패 피드백은 구독.
- **결정론**: 어떤 상품이 몇 개·어느 가격으로 뜨는지는 `MapGenerator.Rng` 로 생성 시점에 굴림. `UnityEngine.Random` 직접 호출 X.
- **보스층 무관**: 상점은 1/6/11층 일반 Safe Room 에만. 보스층(5/10/15) 파이프라인과 무관.
- **파일 700줄 이하·한 책임**: 아래 파일 분리.

## 4. 파일 / 컴포넌트 (신규)

위치: `Assets/KDU/01.Scripts/Shared/Shop/` (지갑·HUD 는 UI 성격이라 향후 멤버 E/UI 폴더로 이관 가능 — ARCHITECTURE 분담표상 상점은 "멤버 E 아이템/UI"). 초기엔 KDU 폴더에서 시작.

| 파일 | 티어 | 책임 |
| --- | --- | --- |
| `Wallet.cs` | 1(코어 인접) | 골드 잔액 단일 보유. `OnGoldDropped` 구독 → 가산. `TrySpend(int)` API. 변동 시 `OnGoldChanged` 발행. **런 내 층 이동에도 유지**(파괴 X), 사망/새 런에 리셋. |
| `GoldHud.cs` | 2(UI) | `OnGoldChanged` 구독 → 골드 수치 표시 (좌상단 플레이어 정보 영역, GDD 12.2). |
| `ShopItemEntry.cs` | 데이터 | 직렬화 구조체/클래스 1개 — 상품 1행: 아이템(ItemBase) 또는 후보 세트, 층별 등장확률[3], 층별 개수 규칙, 층별 가격[3], 강화레벨(0강), 비고 플래그. |
| `ShopTableData.cs` | 데이터(SO) | `ShopItemEntry[]` + 층 인덱스 매핑(1/6/11 → 0/1/2). GDD 11.4 표를 그대로 담는 자산. |
| `ShopStall.cs` | 2(콘텐츠) | 진열대 1칸 = 상품 1개. 스프라이트 + 가격 라벨. 플레이어 인접 + 구매키 → `Wallet.TrySpend` 성공 시 `InventoryController.TryAddItem`, 진열대 비움/제거. 실패(골드부족/인벤풀) 피드백 이벤트. 시야 표시(`GridChest` 패턴 재사용). |
| `ShopSpawner.cs` | 2(콘텐츠) | depth ∈ {1,6,11} 일 때 Safe Room 테두리 셀 산출 → `ShopTableData` + `Rng` 로 상품 결정론 생성 → `ShopStall` 배치 + 상인 프롭 배치. `MapGenerator` 가 호출(`chestSpawner`/`mapItemSpawner` 와 동일 훅). |
| (수정) `GameEvents.cs` | 코어 | `OnGoldChanged(int balance)` 추가. 선택: `OnPurchaseFailed(reason)` / `OnItemPurchased(ItemBase)`. |
| (수정) `MapGenerator.cs` | 코어 | 일반 층 분기 끝에 `if (IsShopDepth(depth)) shopSpawner.Spawn(safeRoom, depth, Rng);` 추가. `IsShopDepth(d) => d==1||d==6||d==11`. |

신규 프리팹/자산 (인스펙터 수동): `ShopStall` 프리팹, `Merchant`(상인) 프롭, `GoldHud` UI, `ShopTableData.asset`(11.4 표 입력).

## 5. 데이터 모델 — `ShopTableData`

- 층 인덱스: 1층→0, 6층→1, 11층→2. (`FloorIndex(depth)`).
- `ShopItemEntry` 필드(안):
  - `ItemBase item` — 단일 상품. **또는** `ItemBase[] oneOfSet` — "둘 중 하나 반드시"(수리검/작살, 쿠나이/올가미, 도끼/투창) · "랜덤 1개"(0강 반지 = 전체 반지 풀) 표현.
  - `float[3] appearChance` — 층별 등장확률 (1/6/11). 예: 투척무기 세트 = {1, 0, 0}.
  - `Vector2Int[3] countRange` 또는 `int[3] fixedCount` — 층별 개수(고정/범위). 회복포션 1~3, 랜덤포션 1~2 등.
  - `int[3] price` — 층별 가격.
  - `int enhanceLevel` — 0강(=0). 장비/반지용.
  - `bool excludeFromCurse` 등 필요 플래그.
- "랜덤 포션(힘/경험/체력 제외)" 같은 **필터 규칙**은 entry 에 카테고리/제외 플래그로 표현하거나 전용 entry 종류로. (GridChest 의 `ItemCategory` 필터 방식 재사용 검토 → 일관성↑)

## 6. 생성 흐름 (`ShopSpawner.Spawn`)

1. Safe Room `RoomData` 확보 (MapGenerator 가 전달, 또는 rooms 에서 `RoomType.Safe` 검색).
2. **테두리 셀 산출**: `floorCells` 중 벽에 인접한 외곽 셀 = 진열 후보. (player 텔레포트 중앙은 제외.)
3. `ShopTableData` 순회 — 각 entry 에 대해 층 인덱스로 `appearChance` 판정(`Rng`), 통과 시 `count` 만큼 상품 인스턴스 결정:
   - `oneOfSet` 있으면 `Rng` 로 1개 선택(반지=풀에서 랜덤).
   - 가격 = entry.price[floorIndex].
4. 산출된 상품 목록을 테두리 셀에 순서대로 `ShopStall` 로 배치(셀 부족 시 경고).
5. 상인 프롭 1개를 Safe Room 안 고정/지정 셀에 배치(장식. 상호작용 없음 — 구매는 진열대에서).
6. 층 전환 시 이전 상점 정리: `ShopSpawner.Clear()` (chestSpawner 패턴).

## 7. 구매 상호작용 (`ShopStall`)

- 표시: 상품 아이콘 스프라이트 + 가격 라벨(월드 텍스트/스프라이트). 시야 밖 숨김(GridChest `UpdateVisibility` 재사용).
- 입력: 플레이어가 인접(또는 같은 칸) + **구매키**(예: Space/E — 줍기 키와 충돌 주의) 누름.
- 처리:
  1. `Wallet.Balance < price` → 실패 피드백(`OnPurchaseFailed`/사운드/플로팅 "골드 부족"), 종료.
  2. `ItemInstance` 생성(ItemPickup.BuildInstance 패턴: Weapon/Armor/Potion/기타).
  3. `InventoryController.TryAddItem(instance)` 실패(인벤 가득) → 골드 차감 X, 실패 피드백, 종료.
  4. 성공 → `Wallet.TrySpend(price)` 차감 → 진열대 소진(스프라이트 비움 또는 Destroy) → `OnItemPurchased`.
  - **순서 주의**: 인벤 추가 성공을 먼저 확인하고 골드 차감(가득 찼는데 골드만 빠지는 버그 방지). `TryAddItem` → 성공 시 `TrySpend`.

## 8. 지갑 & HUD (`Wallet` / `GoldHud`)

- `Wallet`: 씬 1개. `OnEnable` 에서 `OnGoldDropped += Add`. 잔액 변동마다 `OnGoldChanged?.Invoke(balance)`.
  - **지속성**: 런 동안 층 이동에도 유지(상점이 1→6→11 누적 경제). `FloorTransitionController` 의 시스템 리셋 대상에서 제외하거나 런 스코프 오브젝트에 둔다.
  - 리셋: 플레이어 사망/새 런 시작 시 0. (`OnPlayerDied`/런 시작 훅)
- `GoldHud`: `OnGoldChanged` 구독, 좌상단 표시(GDD 12.2 플레이어 정보 영역).

## 9. 결정론

- 상점 생성은 `MapGenerator.Rng`(층별 시드) 사용 → 같은 시드 = 같은 상점 구성. 챌린지/시드챌린지(GDD 13장)와 호환.
- 단, **구매 행위**는 플레이어 입력이므로 비결정 — 생성 결과만 결정론.

## 10. 미결 설계 결정 (추천안)

1. **지갑 위치/지속성**: ⭐런 스코프 싱글톤(층 이동 유지, 사망 리셋). — 별도 RunState 오브젝트 or `DontDestroyOnLoad`.
2. **구매 입력 방식**: ⭐인접 + 구매키 1회(우발 구매 방지). 줍기 키(Space)와 분리된 키 권장(예: E 또는 전용).
3. **상품 지급 경로**: ⭐인벤토리 직접 추가(`TryAddItem`). (바닥 픽업 생성 X — 상점은 즉시 소유 이전이 자연스러움.)
4. **상인 역할**: ⭐장식 NPC(상호작용 없음). 구매는 진열대 단위. (대화/단일 상점 UI 는 BACKLOG.)
5. **가격 라벨 렌더링**: 월드 스페이스 텍스트 vs 스프라이트 숫자. ⭐월드 캔버스 TMP 텍스트(간단).
6. **인벤 가득 시**: 구매 차단 + 피드백(골드 보존). (바닥 드롭 폴백은 BACKLOG.)
7. **테두리 셀 부족**: 경고 로그 + 가능한 만큼만 배치.

## 11. 구현 순서 (마일스톤)

- **M1 — 경제 기반**: `Wallet` + `GameEvents.OnGoldChanged` + `GoldHud`. (상점 없이도 적/상자 골드가 잔액에 쌓이고 HUD 표시.) ← 가장 먼저, 독립 검증 가능.
- **M2 — 단일 진열대 구매**: `ShopStall` 1개 수동 배치 → 인접+키 구매 → 골드 차감·인벤 추가 동작.
- **M3 — 데이터·생성**: `ShopItemEntry`/`ShopTableData` + `ShopSpawner` + `MapGenerator` 훅. depth 1/6/11 자동 상점.
- **M4 — 표 완성·연출**: GDD 11.4 표 전체 입력, 상인 프롭, 실패 피드백, 가격 라벨 폴리시.

## 12. 검증 기준

- [ ] 적/상자 골드 획득 시 `Wallet.Balance` 증가 + HUD 갱신.
- [ ] 1/6/11층 Safe Room 에만 상점 등장, 그 외 층 미등장.
- [ ] 같은 시드 = 같은 상품 구성(결정론).
- [ ] 골드 부족 시 구매 차단·잔액 불변.
- [ ] 인벤 가득 시 구매 차단·골드 불변.
- [ ] 구매 성공 시 정확히 1개 인벤 추가 + 정확한 가격 차감 + 진열대 소진.
- [ ] 보스층(5/10/15)에 상점 미등장.
- [ ] 700줄 넘는 클래스 없음.

## 13. 스코프 밖 (→ BACKLOG)

- 단일 상점 UI 창(목록/구매 패널), 상인 대화, 판매(되팔기), 재입고/리롤, 할인, 일일 특가 등.

## 14. 인스펙터 / 에셋 수동 작업 (구현 후)

- `ShopTableData.asset` 생성 → GDD 11.4 표대로 입력(층별 확률/개수/가격).
- `ShopStall` 프리팹(스프라이트 + 가격 라벨 + `ShopStall`), `Merchant` 프롭, `GoldHud` UI.
- `MapGenerator` 인스펙터에 `shopSpawner`·`ShopTableData` 슬롯 연결.
- `Wallet`/`GoldHud` 를 씬(또는 런 스코프 오브젝트)에 배치.
