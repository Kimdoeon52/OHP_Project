# 적(Enemy) 시스템 리팩토링 가이드

> [!IMPORTANT]
> GDD v1.0 기획 변경에 따른 적 시스템 전면 재설계 방향서입니다.
> **기존 코드의 재활용성을 최대화**하면서, GDD에 맞춘 13종 + 1종(고대 골렘) 몬스터를 구현하기 위한 아키텍처 가이드입니다.

---

## 1. 현재 코드 구조 분석

### 1.1 코어 시스템 (티어 1 — 재사용 가능)

| 파일 | 위치 | 역할 | 재활용 판정 |
|------|------|------|-------------|
| [EnemyBase.cs](file:///d:/kkh/OHP_Project/Assets/LSH/02.%20Scripts/Boss/EnemyBase.cs) | `LSH/02. Scripts/Boss/` | 모든 적의 추상 베이스. HP, 시야, 텔레그래프, 턴 연동 | ✅ **핵심 유지** — 대폭 확장 필요 |
| [EnemyData.cs](file:///d:/kkh/OHP_Project/Assets/KDU/01.Scripts/Data/EnemyData.cs) | `KDU/01.Scripts/Data/` | ScriptableObject 데이터 정의 | ✅ **핵심 유지** — 필드 대폭 추가 필요 |
| [GridEnemyBase.cs](file:///d:/kkh/OHP_Project/Assets/KDU/01.Scripts/Grid/Enemies/GridEnemyBase.cs) | `KDU/01.Scripts/Grid/Enemies/` | 폐기된 래퍼 (빈 클래스) | ❌ **폐기 확정** — 추후 삭제 |

### 1.2 기존 적 구현체 (티어 2 — KDU)

| 파일 | 핵심 행동 | GDD 대응 | 재활용 판정 |
|------|-----------|----------|-------------|
| [GridEnemyTracker.cs](file:///d:/kkh/OHP_Project/Assets/KDU/01.Scripts/Grid/Enemies/GridEnemyTracker.cs) | 시야 내 추적 → 인접 시 공격 예고 | GDD에 "Tracker" 없음 (삭제) | ⚠️ **이동/공격 패턴 코드 재활용** |
| [GridEnemyArcher.cs](file:///d:/kkh/OHP_Project/Assets/KDU/01.Scripts/Grid/Enemies/GridEnemyArcher.cs) | 고정 위치, 4방향 직선 사격 | 마법사 스켈레톤과 유사 | ⚠️ **원거리 공격 로직 재활용** |

### 1.3 기존 적 구현체 (티어 2 — KKH)

| 파일 | 핵심 행동 | GDD 대응 | 재활용 판정 |
|------|-----------|----------|-------------|
| [GridEnemyKingsGuard.cs](file:///d:/kkh/OHP_Project/Assets/KKH/02.%20Scripts/Enemies/GridEnemyKingsGuard.cs) | 2턴에 1번 행동, 피해 감소 | 폐기 (GDD에 없음) | ⚠️ **턴 카운터 패턴, 피해 감소 로직 재활용** |
| [GridEnemyWraith.cs](file:///d:/kkh/OHP_Project/Assets/KKH/02.%20Scripts/Enemies/GridEnemyWraith.cs) | 벽 통과 추적 | 폐기 (GDD에 없음) | ⚠️ **벽 무시 이동 로직(TryPhaseStep) 재활용 가능** — 지렁이(원거리 면역)에 변형 적용 |
| [GridEnemyPhantom.cs](file:///d:/kkh/OHP_Project/Assets/KKH/02.%20Scripts/Enemies/GridEnemyPhantom.cs) | 사망 시 분열 | 폐기 (GDD에 없음) | ⚠️ **분열(SpawnSplit) 로직은 향후 재활용 가능** |
| [GridEnemyCrawler.cs](file:///d:/kkh/OHP_Project/Assets/KKH/02.%20Scripts/Enemies/GridEnemyCrawler.cs) | 한 턴에 2칸 이동 (고속) | 박쥐/고블린/작은악마 등 고속 몬스터와 유사 | ✅ **고속 이동 패턴 핵심 재활용** |

---

## 2. GDD v1.0 vs 현재 코드 — 핵심 차이점

### 2.1 GDD가 요구하는 새로운 스탯 시스템

GDD는 모든 몬스터에 **8종 스탯**을 정의합니다. 현재 `EnemyData`에는 이 중 일부만 존재합니다.

| GDD 스탯 | 현재 EnemyData 필드 | 상태 |
|----------|---------------------|------|
| 체력(HP) | `maxHp` | ✅ 있음 |
| 공격력(min~max) | `attackDamage` (단일 값) | ⚠️ **min/max 범위로 변경 필요** |
| 이동 속도(초/칸) | 없음 | ❌ **추가 필요** |
| 공격 속도(회/s) | 없음 | ❌ **추가 필요** |
| 내구성(Defense, min~max) | 없음 | ❌ **추가 필요** |
| 회피(Evasion) | 없음 | ❌ **추가 필요** |
| 정확도(Accuracy) | 없음 | ❌ **추가 필요** |
| 경험치(EXP) | 없음 | ❌ **추가 필요** |
| 시간 보너스 | `timeBonus` | ✅ 있음 |
| 시야 범위 | `visionRange` | ✅ 있음 |
| 골드 드롭 | 없음 | ❌ **추가 필요** |

### 2.2 GDD가 요구하는 특수 행동 (기존 코드에 없는 것)

| 특수 행동 | 대상 몬스터 | 필요 시스템 |
|-----------|-------------|-------------|
| **독 상태이상** | 역병 쥐, 거미, 뱀 | StatusEffect 시스템 필요 |
| **이동속도 감소 디버프** | 거미 | StatusEffect 시스템 필요 |
| **방패 들기 (피격 감소)** | 스켈레톤(방패O), 마법사 스켈레톤(방패O) | 턴 기반 방어 상태 |
| **아이템 훔치기 + 도주** | 칼 든 고블린 | 인벤토리 시스템 연동 |
| **장판 공격** | 작은 악마 | 범위 공격 텔레그래프 |
| **원거리 면역(이동 중)** | 지렁이 | 상태 플래그 |
| **화상 상태이상** | 불의 정령 | StatusEffect 시스템 필요 |
| **강타(기절)** | 트롤 | StatusEffect 시스템 필요 |
| **Area 기반 스케일링** | 고대 골렘 | EnemyData 또는 초기화 시 Area 파라미터 |

### 2.3 GDD가 요구하는 이동 속도 다양성

> [!NOTE]
> 현재 코드에서 모든 적은 **1턴 = 1칸** 고정입니다.
> GDD는 0.3~1.0초/칸까지 다양한 이동 속도를 요구합니다.

| 이동 속도 | 몬스터 |
|-----------|--------|
| 1.0초/칸 | 역병 쥐, 거미, 뱀 |
| 0.75초/칸 | 스켈레톤 계열, 트롤 |
| 0.5초/칸 | 박쥐, 지렁이, 핑크 트롤, 불의 정령 |
| 0.4초/칸 | 칼 든 고블린 |
| 0.3초/칸 | 작은 악마 |

→ **TimeSystem과 TurnManager 연동**하여 이동 속도 비용을 처리해야 합니다.

---

## 3. 리팩토링 아키텍처 설계

### 3.1 EnemyData 확장 (최우선)

```csharp
[CreateAssetMenu(fileName = "EnemyData", menuName = "60s Dungeon/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string id;
    public string displayName;
    public Sprite sprite;
    public EnemyGrade grade;         // Common, Elite, MiniBoss, Boss

    [Header("스탯")]
    public int maxHp = 8;
    public int attackMin = 2;        // 기존 attackDamage → min/max 분리
    public int attackMax = 4;
    public float moveSpeed = 1.0f;   // 초/칸 (낮을수록 빠름)
    public float attackSpeed = 1.0f; // 회/s
    public int defenseMin = 0;
    public int defenseMax = 0;
    public int evasion = 5;
    public int accuracy = 10;

    [Header("보상")]
    public int expReward = 5;
    public float timeBonus = 3f;
    public int goldMin = 0;
    public int goldMax = 0;

    [Header("AI")]
    public float visionRange = 6f;
    public AttackPattern attackPattern;
    public EnemyMoveType moveType;   // Normal, WallPass, Stationary

    [Header("특수 효과")]
    public StatusEffectData onHitEffect; // 명중 시 적용할 상태이상 (독, 화상 등)
}

public enum EnemyGrade { Common, Elite, MiniBoss, Boss }
public enum EnemyMoveType { Normal, WallPass, Stationary }
```

### 3.2 EnemyBase 확장

재활용할 **핵심 공통 로직**:

```
EnemyBase (추상)
├── HP / TakeDamage / Die             ← 기존 유지
├── 시야 / UpdateVisibility           ← 기존 유지
├── 텔레그래프 / Show/Clear           ← 기존 유지
├── TurnManager 등록                  ← 기존 유지
├── ★ 이동 속도 기반 행동 비용 처리   ← 신규
├── ★ Defense 적용 (받는 데미지 감소) ← 신규
├── ★ 회피/정확도 적중 판정           ← 신규
├── ★ 경험치/골드 드롭                ← 신규
└── ★ 상태이상 적용 훅               ← 신규
```

**추가해야 할 EnemyBase 메서드들:**

```csharp
// 데미지 계산 (min~max 랜덤)
protected int RollAttackDamage()
    => Random.Range(data.attackMin, data.attackMax + 1);

// Defense 적용
protected int ApplyDefense(int rawDamage)
    => Mathf.Max(0, rawDamage - Random.Range(data.defenseMin, data.defenseMax + 1));

// 회피/정확도 적중 판정
protected bool RollHit(int targetEvasion)
    => Random.Range(0, 100) < Mathf.Min(50, data.accuracy - targetEvasion + 50);

// 이동 비용 (TimeSystem 연동)
protected float GetMoveCost() => data.moveSpeed;
protected float GetAttackCost() => data.attackSpeed;
```

### 3.3 재활용 가능한 공통 이동 로직 → 유틸리티로 추출

현재 **5개 적 클래스에 동일한 `TryStepTowardPlayer()` + `TryStep()`이 복붙**되어 있습니다.

```
GridEnemyTracker     → TryStepTowardPlayer() + TryStep()
GridEnemyKingsGuard  → TryStepTowardPlayer() + TryStep() (완전 동일)
GridEnemyWraith      → TryStepTowardPlayer() + TryPhaseStep() (벽 무시 버전)
GridEnemyPhantom     → TryStepTowardPlayer() + TryStep() (완전 동일)
GridEnemyCrawler     → GetNextStepDirection() + CanMoveTo() (리팩토링된 버전)
```

> [!TIP]
> **이 로직들을 EnemyBase로 올려서 공통 메서드로 제공**하면, 새 몬스터 구현 시 코드 중복이 제거됩니다.

```csharp
// EnemyBase에 추가할 공통 이동 메서드
protected bool TryStepTowardPlayer()
{
    var diff = Player.GridPosition - GridPosition;
    var dirX = diff.x != 0 ? new Vector2Int(Math.Sign(diff.x), 0) : Vector2Int.zero;
    var dirY = diff.y != 0 ? new Vector2Int(0, Math.Sign(diff.y)) : Vector2Int.zero;

    bool xPrimary = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);
    var primary = xPrimary ? dirX : dirY;
    var secondary = xPrimary ? dirY : dirX;

    if (primary != Vector2Int.zero && TryStep(primary)) return true;
    if (secondary != Vector2Int.zero && TryStep(secondary)) return true;
    return false;
}

protected virtual bool TryStep(Vector2Int dir)
{
    var next = GridPosition + dir;
    if (!GridSystem.Instance.IsWalkable(next)) return false;
    if (next == Player.GridPosition) return false;
    if (TurnManager.Instance.GetEntityAt(next) != null) return false;
    SetGridPosition(next);
    UpdateVisibility();
    return true;
}
```

→ Wraith/지렁이 같은 벽 통과 적은 `TryStep()`을 `override`하면 됩니다.

### 3.4 공통 OnTurnUpdate 템플릿 (Template Method 패턴)

대부분의 근접 적이 **동일한 3단계 턴 흐름**을 공유합니다:

```
1. 이전 턴 공격 의도 발동
2. 시야 내에서 이동
3. 인접 시 다음 턴 공격 예고
```

이것을 EnemyBase에 **템플릿 메서드**로 올리면 됩니다:

```csharp
// EnemyBase
public override void OnTurnUpdate()
{
    if (IsDead || Player == null) return;
    ExecutePendingAttack();    // 1단계
    PerformMovement();         // 2단계 (하위 클래스 override 가능)
    CheckAttackTelegraph();    // 3단계 (하위 클래스 override 가능)
}

protected virtual void ExecutePendingAttack() { ... }
protected virtual void PerformMovement() { ... }
protected virtual void CheckAttackTelegraph() { ... }
```

→ 특수 행동이 필요한 적만 해당 단계를 `override`.

---

## 4. GDD 몬스터별 구현 전략 — 코드 재활용 매핑

### Area 1 — 하수구 (1~5층)

| 몬스터 | 기반 코드 | 추가 구현 |
|--------|-----------|-----------|
| **역병 쥐** | Tracker의 이동/공격 패턴 거의 그대로 | + 독 상태이상 (onHitEffect) |
| **거미** | Tracker 기반 | + 독 + 이동속도 감소 디버프 |
| **박쥐** | **Crawler의 고속 이동** 재활용 | moveSpeed=0.5, 높은 회피 |
| **뱀 [Elite]** | Tracker 기반 | + 독니 물기 (1.5초 공격, 독 2~4) |

### Area 2 — 던전 (6~10층)

| 몬스터 | 기반 코드 | 추가 구현 |
|--------|-----------|-----------|
| **스켈레톤(방패X)** | Tracker 기반, moveSpeed=0.75 | 기본 근접 |
| **스켈레톤(방패O)** | Tracker 기반 | + 50% 확률 방패 들기 (다음 피격 감소) — **KingsGuard의 피해 감소 로직 재활용** |
| **마법사 스켈레톤(방패X)** | **Archer의 원거리 로직** 재활용 | 4방향 LOS 텔레그래프 발사 |
| **마법사 스켈레톤(방패O)** | Archer + 방패 | 원거리 + 50% 방패 |
| **칼 든 고블린** | **Crawler의 고속 이동** 재활용 (moveSpeed=0.4) | + 아이템 훔치기 + 도주 AI (신규) |
| **트롤 [Elite]** | Tracker 기반 | + 강타 (1.5초, 1초 기절) |

### Area 3 — 용암지대 (11~15층)

| 몬스터 | 기반 코드 | 추가 구현 |
|--------|-----------|-----------|
| **작은 악마** | **Crawler의 고속 이동** (moveSpeed=0.3) | + 장판 공격 (3×3 화염, 텔레그래프 확장) |
| **지렁이** | Tracker 기반 + **Wraith의 TryPhaseStep** 변형 | + 이동 중 원거리 면역 플래그 |
| **핑크 트롤** | Tracker 기반, 높은 HP | 단순 근접 탱커 |
| **불의 정령 [Elite]** | Tracker 기반 | + 화상 상태이상 (onHitEffect) |

### 스페셜 방

| 몬스터 | 기반 코드 | 추가 구현 |
|--------|-----------|-----------|
| **고대 골렘 [Elite]** | Tracker 기반 | Area 기반 스탯 스케일링 + 랜덤 무기 장착 |

---

## 5. 파일 구조 계획

```
Assets/KKH/02. Scripts/Enemies/
├── ─── Area1 ───
│   ├── EnemyPlagueRat.cs       (역병 쥐)
│   ├── EnemySpider.cs          (거미)
│   ├── EnemyBat.cs             (박쥐)
│   └── EnemySnake.cs           (뱀)
├── ─── Area2 ───
│   ├── EnemySkeleton.cs        (스켈레톤 — 방패 유무는 EnemyData로)
│   ├── EnemyMageSkeleton.cs    (마법사 스켈레톤)
│   ├── EnemyGoblinThief.cs     (칼 든 고블린)
│   └── EnemyTroll.cs           (트롤)
├── ─── Area3 ───
│   ├── EnemyImp.cs             (작은 악마)
│   ├── EnemyWorm.cs            (지렁이)
│   ├── EnemyPinkTroll.cs       (핑크 트롤)
│   └── EnemyFireElemental.cs   (불의 정령)
├── ─── Special ───
│   └── EnemyAncientGolem.cs    (고대 골렘)
└── ─── Deprecated ───          (폐기된 기존 파일)
    ├── GridEnemyKingsGuard.cs
    ├── GridEnemyWraith.cs
    ├── GridEnemyPhantom.cs
    └── GridEnemyCrawler.cs
```

---

## 6. 핵심 원칙 요약

> [!CAUTION]
> 1. **EnemyData에 스탯을 몰아넣고, 코드에 매직넘버 금지**
> 2. **공통 이동/공격 로직은 EnemyBase에 한 번만 구현**
> 3. **특수 행동만 자식 클래스에서 override**
> 4. **기존 Tracker/Archer/Crawler/KingsGuard/Wraith의 로직을 복붙하지 말고, 추출하여 재활용**
> 5. **StatusEffect 시스템은 별도 구현 → 적과 플레이어 모두 사용**

---

## 7. 의존 시스템 (선행 작업)

| 시스템 | 현재 상태 | 필요 작업 |
|--------|-----------|-----------|
| TimeSystem | ✅ 존재 | 이동/공격 속도별 시간 소모 연동 |
| TurnManager | ✅ 존재 | 이동 속도 기반 행동 순서 고려 |
| StatusEffect 시스템 | ❌ 미구현 | **신규 구현 필요** (독, 화상, 기절, 둔화) |
| 회피/정확도 판정 | ❌ 미구현 | EnemyBase + 플레이어 양쪽에 구현 |
| 인벤토리 시스템 | ❌ 미구현 | 고블린 훔치기용 (후순위) |
