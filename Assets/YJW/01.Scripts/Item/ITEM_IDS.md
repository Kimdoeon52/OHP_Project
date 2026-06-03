# 아이템 ID 규칙 (Item ID Map)

상자 전리품 추첨과 맵 드랍 분기를 위해 아이템 `id`(ItemBase.id)를 구간으로 나눈다.
코드 매핑은 [ItemCategory.cs](ItemCategory.cs) (`ItemCategories.FromId`)가 단일 진실 공급원이며,
이 표와 항상 일치시킨다.

## 구간 표

| ID 구간 | 분류 | `ItemCategory` | 비고 |
|---|---|---|---|
| 1001 ~ 2000 | 스크롤 | `Scroll` | 강화 스크롤 포함 (랜덤 상자는 추첨 시 제외) |
| 2001 ~ 3000 | 포션 | `Potion` | |
| 3001 ~ 4000 | 투척무기 | `ThrowWeapon` | |
| 4001 ~ 5000 | 방어구 | `Armor` | |
| 5001 ~ 6000 | 근접무기 | `MeleeWeapon` | |
| 6001 | 횃불 | `Torch` | 단일 ID |
| 6002 | 생명파편 | `LifeShard` | 단일 ID |
| 6003 | 소형비상식량 | `Food` | |
| 6004 | 비상식량 | `Food` | |
| 6005 | 빵 | `Food` | |
| 7001 ~ 8000 | 반지 | `Ring` | |

> 6000번대는 구간이 아니라 개별 ID로 부여한 기타/소비 아이템 영역이다.
> 그 외(매핑 안 된 ID)는 `ItemCategory.Unknown` 으로 처리된다.

## 추가 규칙

- 새 아이템 ID는 **해당 구간 안에서** 부여한다. 구간을 새로 만들 때는
  이 표와 `ItemCategory.FromId` 를 함께 갱신한다.
- 상자별로 어떤 카테고리가 나오는지는 GDD 9.14(상자 시스템) 및 각 `ChestData` SO 참조.
