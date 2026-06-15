# Area 3 몬스터 제작을 위한 스크립트 작성 가이드

> **프로젝트**: 60초 던전 (OHP_Project) | **대상 영역**: Area 3 - 불타는 저승 (11~15층)
> **작성 목적**: Area 3 신규 몬스터 4종의 개발 일관성 유지 및 티어 2 구현 패턴 준수

---

## 1. 개요 및 스펙 요약

Area 3(불타는 저승, 11~15층)에서 등장하는 지옥 테마의 몬스터들은 기존 구역의 적들보다 향상된 기믹과 스탯을 가집니다. **살아남을 코어는 제대로(티어 1), 단발 콘텐츠는 가볍게(티어 2)**라는 설계 규칙을 준수하여, 추상화 강박을 버리고 아래 기믹들을 직접적이고 단순하게 구현해야 합니다.

### 몬스터별 스펙 시트

| 몬스터명 | 등급 | HP / 공격력 | 이동 / 공격속도 | 특수 기믹 및 행동 | 시간 보너스 |
| :--- | :---: | :---: | :---: | :--- | :---: |
| **작은 악마 (EnemyImp)** | Common | 40 / 8~14 | 0.2초/칸 / 1회/s | 3x3 화염 장판 예고 후 공격, 화염 1초간 잔존 | +3초 |
| **지렁이 (EnemyWorm)** | Common | 60 / 10~15 | 0.3초/칸 / 1회/s | 이동 중 원거리 공격(투척, 마법) 완전 면역 | +3초 |
| **핑크 트롤 (EnemyPinkTroll)** | Common | 120 / 12~18 | 0.3초/칸 / 1회/s | 높은 체력의 기본 근접 전투형 (특수 행동 없음) | +3초 |
| **불의 정령 (EnemyFireSpirit)** | Elite | 110 / 15~22 | 0.3초/칸 / 1회/s | 공격 명중 시 플레이어에게 3초간 화상 디버프 부여 | +5초 |

---

## 2. 공통 아키텍처 규칙 (티어 2 콘텐츠)

* **`EnemyBase` 상속**: 모든 몬스터는 `EnemyBase`를 반드시 상속받아 구현합니다.
* **템플릿 메서드 패턴 준수**: `EnemyBase`에 구현된 3대 메서드인 `PerformMovement()`, `CheckAttackTelegraph()`, `ExecutePendingAttack()`을 필요에 맞게 override 합니다.
* **ScriptableObject 데이터 분리**: 스탯 수치, 보상, 아이템 드롭 정보 등은 하드코딩하지 말고 `EnemyData` ScriptableObject를 새로 생성하여 주입합니다.
* **경로 격리**: 스크립트는 `Assets/KKH/02. Scripts/Enemies/Area 3/` 폴더 아래 생성해야 협업 시 충돌을 방지할 수 있습니다.

---

## 3. 몬스터별 스크립트 구현 가이드

### 3.1 작은 악마 (`EnemyImp.cs`)

**기믹**: 3x3 화염 장판 공격 예고(텔레그래프) 및 지형 생성

* **CheckAttackTelegraph 오버라이드**:
  플레이어가 사정거리 내에 있을 때, targetCell을 중심으로 3x3 범위 of 모든 타일에 예고를 띄웁니다.
* **ExecutePendingAttack 오버라이드**:
  예고 후 1.5초 대기(delaySeconds)하고, 해당 범위 내에 플레이어가 위치하면 롤링한 데미지를 가합니다. 공격이 종료되면 해당 타일에 1초 동안 유지되는 화염(Fire) 장판을 `TrapTerrainController` 등을 통해 스폰합니다.

#### 구현 예시 코드

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyImp : EnemyBase
{
    private readonly List<Vector2Int> _targetArea = new List<Vector2Int>();

    protected override void CheckAttackTelegraph()
    {
        if (IsDead || _isAttacking) return;

        // 플레이어가 감지 범위 내에 있으면 3x3 장판 공격 시도
        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            _pendingAttackCell = Player.GridPosition;
            UpdateSpriteDirection(_pendingAttackCell.Value);
            
            // 3x3 범위 계산
            _targetArea.Clear();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    _targetArea.Add(_pendingAttackCell.Value + new Vector2Int(x, y));
                }
            }

            // 범위 전체에 예고 타일 표시
            foreach (var cell in _targetArea)
            {
                ShowTelegraph(cell);
            }

            StartCoroutine(ExecutePendingAttack(_pendingAttackCell.Value, 1.5f));
        }
    }

    protected override IEnumerator ExecutePendingAttack(Vector2Int targetCell, float delaySeconds)
    {
        _isAttacking = true;
        yield return new WaitForSeconds(delaySeconds);

        if (!IsDead)
        {
            if (_animator != null) _animator.SetTrigger("Attack");

            // 플레이어가 3x3 영역 내에 있는지 판정
            if (_targetArea.Contains(Player.GridPosition))
            {
                int damage = RollAttackDamage();
                TryHitPlayer(damage);
            }

            // 공격 후 3x3 영역에 화염 장판 지형 생성
            foreach (var cell in _targetArea)
            {
                if (GridSystem.Instance.IsWalkable(cell))
                {
                    // TODO: TrapTerrainController 또는 Hazard 지형 생성기 연동하여 1초 유지되는 화염 생성
                    // TrapTerrainController.SpawnTemporaryHazard(cell, HazardType.Fire, 1f);
                }
            }
        }

        ClearTelegraphs();
        _isAttacking = false;
    }
}
```

---

### 3.2 지렁이 (`EnemyWorm.cs`)

**기믹**: 이동 중 원거리 공격 면역 (투척 및 마법 무효)

* **데미지 타입 확장**:
  기존 `TakeDamage(int amount)`는 공격 타입을 알 수 없으므로, 원거리 공격 무력화를 처리하기 위해 데미지 타입을 구분하도록 `EnemyBase`나 인터페이스를 일부 수정하거나, `TakeDamage` 오버로드/체크 방식을 사용합니다.
* **이동 상태 감지**:
  이동 보간 코루틴(`_moveCoroutine != null`)이 진행 중이거나, `Walk` 애니메이션 플래그가 true인 동안 원거리 타입의 데미지를 `0`으로 처리합니다.

#### 구현 예시 코드

```csharp
using UnityEngine;

public class EnemyWorm : EnemyBase
{
    private Coroutine _moveCoroutine;
    private bool _isMovingVisual = false;

    // 공격 타입을 받는 오버로드 함수 작성 (기존 TakeDamage 호환 가능)
    public void TakeDamage(int amount, DamageType damageType)
    {
        if (IsDead || amount <= 0) return;

        // 이동 중(시각적 보간 중이거나 Walk 중)에 원거리(투척/마법) 공격을 받으면 완전 무효화
        if ((_isMovingVisual || (_animator != null && _animator.GetBool("Walk"))) && damageType == DamageType.Ranged)
        {
            // 이펙트 혹은 텍스트 요청 ("Immune" 등)
            GameEvents.OnCombatTextRequested?.Invoke(
                new CombatTextRequest(transform.position, "Immune", 0, CombatTextType.Miss)
            );
            Debug.Log($"{name}이 이동 중 원거리 공격을 면역했습니다.");
            return;
        }

        base.TakeDamage(amount);
    }

    // 기존 TakeDamage(int) 호출 시 기본 근접(Melee) 타입으로 취급
    public override void TakeDamage(int amount)
    {
        TakeDamage(amount, DamageType.Melee);
    }

    protected override bool TryStep(Vector2Int dir)
    {
        bool success = base.TryStep(dir);
        if (success)
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(SmoothMoveVisual(0.1f));
        }
        return success;
    }

    private System.Collections.IEnumerator SmoothMoveVisual(float delay)
    {
        _isMovingVisual = true;
        if (_animator != null) _animator.SetBool("Walk", true);

        Vector3 targetWorldPos = GridSystem.Instance.GridToWorld(GridPosition);
        transform.position = targetWorldPos;
        
        yield return new WaitForSeconds(delay);

        if (_animator != null) _animator.SetBool("Walk", false);
        _isMovingVisual = false;
    }
}

// 헬퍼 데미지 타입 열거형 (KDU/01.Scripts/Entities/IDamageable.cs 또는 Data 폴더에 정의 권장)
public enum DamageType
{
    Melee,      // 근접 공격
    Ranged,     // 원거리 공격 (투척 무기 및 마법)
    StatusEffect // 상태이상 틱 피해
}
```

---

### 3.3 핑크 트롤 (`EnemyPinkTroll.cs`)

**기믹**: 기믹 없음, 높은 체력을 가진 기본 근접 탱커형

* **구현 최소화**:
  핑크 트롤은 특수 행동이 전혀 없습니다. `EnemyBase`를 상속받은 뒤, 특별히 오버라이드할 로직이 없으므로 상속만 받아주고 수치 튜닝은 ScriptableObject `EnemyData`에서 전적으로 처리합니다.

#### 구현 예시 코드

```csharp
using UnityEngine;

public class EnemyPinkTroll : EnemyBase
{
    // 기획 상 특수 행동이 없으므로 base.PerformMovement()와 
    // base.CheckAttackTelegraph()의 기본 1칸 추적 및 인접 공격을 그대로 상속하여 사용합니다.
}
```

---

### 3.4 불의 정령 (`EnemyFireSpirit.cs`)

**기믹**: 공격 명중 시 3초간 화상 (1초당 2 데미지) 디버프 적용

* **코드 수정 최소화**:
  이미 부모 클래스인 `EnemyBase.cs`에 명중 시 상태이상을 주입해 주는 `TryApplyOnHitEffect` 기능이 구현되어 있습니다. 따라서 C# 코드 추가 대신, **인스펙터의 ScriptableObject 설정만으로 구현할 것을 강력 권장**합니다.
* **ScriptableObject 설정법**:
  1. `EnemyData` 인스턴스를 생성합니다.
  2. `On Hit Effect` 슬롯에 연결될 `StatusEffectData`를 구성합니다.
  3. `Status Type`을 **`Burn`**으로 선택하고, `Duration Turns`를 **`3`**, `Power`를 **`2`**로 설정합니다.

만약 코드상에서 이를 제어해야 하거나 추가 처리가 필요하다면, 아래와 같이 오버라이드할 수 있습니다.

#### 구현 예시 코드 (필요 시에만 오버라이드)

```csharp
using UnityEngine;

public class EnemyFireSpirit : EnemyBase
{
    // 명중 시 수동으로 화상 디버프를 확실하게 주고 싶을 때 사용
    protected override bool TryStep(Vector2Int dir)
    {
        return base.TryStep(dir);
    }
    
    // 공격 예고 및 실행은 기본 동작 사용
}
```

---

## 4. ScriptableObject 데이터 세팅

각 몬스터를 활성화하기 위해 ScriptableObject 데이터를 신규 생성하고 프리팹에 바인딩해야 합니다.

### 4.1 ScriptableObject 생성

1. Unity Project 창에서 `Assets/KKH/04. SOData/Enemies/Area 3/` 폴더로 이동합니다.
2. 우클릭 후 `Create > 60s Dungeon > Enemy Data`를 선택하여 생성합니다.
3. 명세서에 나와 있는 상세 스탯 수치를 인스펙터에 기입합니다.
   * **예시 (불의 정령)**:
     * `Max Hp`: 110
     * `Move Speed`: 0.3
     * `Attack Speed`: 1.0 (초당 1회)
     * `Attack Min` / `Max`: 15 / 22
     * `Defense Min` / `Max`: 2 / 5
     * `Evasion`: 12
     * `Accuracy`: 18
     * `Time Bonus`: 5
     * `Exp Reward`: 30
     * `On Hit Effect`: `StatusType.Burn` (지속: 3, 파워: 2)

### 4.2 프리팹 구성

1. `Assets/KKH/03. Prefab/Enemies/Area 3/` 폴더에 각 몬스터 프리팹을 생성합니다.
2. 각 몬스터 프리팹에 새로 작성한 C# 컴포넌트(`EnemyImp`, `EnemyWorm`, `EnemyPinkTroll`, `EnemyFireSpirit`)를 붙입니다.
3. 컴포넌트 내의 `Data` 슬롯에 방금 생성한 ScriptableObject를 연결합니다.
4. **텔레그래프 타일맵 경고 연동**: `warningTilemap` 슬롯에 씬의 Warning Tilemap을 연결하고, `warningTile` 슬롯에 경고 타일 에셋을 장착합니다.

### 4.3 FloorSpawnConfig 연동

지하 11~14층(지옥 구역)에서 정상 스폰되도록 스폰 테이블을 수정합니다.

1. `Assets/KKH/04. SOData/EnemySystem/Area3_HellConfig.asset`을 엽니다.
2. `minDepth` = 11, `maxDepth` = 14로 범위가 잘 되어있는지 확인합니다.
3. `prefabMappings` 리스트에 다음을 등록합니다:
   * 작은 악마 `EnemyData` -> 작은 악마 `Prefab`
   * 지렁이 `EnemyData` -> 지렁이 `Prefab`
   * 핑크 트롤 `EnemyData` -> 핑크 트롤 `Prefab`
   * 불의 정령 `EnemyData` -> 불의 정령 `Prefab`
4. Hierarchy에 있는 `EnemySpawner` 오브젝트의 `floorConfigs` 리스트에 `Area3_HellConfig`가 세 번째 요소로 잘 등록되어 있는지 더블 체크합니다.
