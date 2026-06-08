# 고대 골렘 (Ancient Golem) 구현 작업 계획서

본 문서는 GDD v1.3에 명시된 스페셜 방(Special Room) 전용 미니보스 **고대 골렘(Ancient Golem)**을 구현하기 위한 세부 작업 절차와 설계 계획을 담고 있습니다.

---

## 1. 고대 골렘 핵심 스펙 요약 (GDD v1.3)

* **등급**: 엘리트 (Elite) / 미니 보스 역할
* **스폰 조건**: **스페셜 방(Special Room) 중 '몬스터 방'**에서 1마리 확정 스폰 (일반 방 스폰 테이블에서 제외)
* **기본 설계 방향**: 에리어(Area)별로 스탯이 다르게 성장하며, 무작위 근접 무기(강화 확률 적용)를 장착한 상태로 스폰되어 사망 시 해당 장비를 드롭합니다.

### 에리어(Area)별 능력치 상세

| 에리어 | 체력 (HP) | 공격력 (Base) | 이동 속도 | 내구성 (Defense) | 회피 / 정확도 | 경험치 / 시간 보너스 |
|---|---|---|---|---|---|---|
| Area 1 (1~5층) | 20 | 1 | 1.0초/칸 | 0 ~ 2 | 8 / 12 | 10 exp / +10초 |
| Area 2 (6~10층) | 50 | 3 | 0.75초/칸 | 1 ~ 3 | 10 / 15 | 15 exp / +10초 |
| Area 3 (11~15층)| 100 | 5 | 0.5초/칸 | 2 ~ 5 | 12 / 18 | 25 exp / +10초 |

---

## 2. 세부 구현 단계 및 절차

### 1단계: ScriptableObject 및 에셋 세팅 (`EnemyData`)

기존의 SO 주도 아키텍처에 맞게 고대 골렘 데이터를 설정합니다. 에리어별 스탯 차이가 크므로 **3개의 개별 SO 에셋**으로 분할하여 디자이너가 조절하기 쉽도록 구성합니다.

1. **골렘 SO 파일 생성** (`Assets/KKH/04. SOData/Enemy/`):
   * `Golem_Area1.asset`
   * `Golem_Area2.asset`
   * `Golem_Area3.asset`
2. **필드 세팅**:
   * `Grade` ➔ `Elite`로 설정
   * 에리어별 스탯 시트에 맞춰 HP, 기본 공격력, 이동 속도, 방어력, 회피/정확도 설정
   * 보상으로 시간 보너스 `+10초`, 경험치(10/15/25) 설정
3. **골렘 프리팹 생성** (`Assets/KKH/03. Prefab/`):
   * 골렘용 스프라이트와 애니메이터가 적용된 `EnemyGolem` 프리팹 제작
   * 에리어별로 외형(스프라이트/색상)이 다를 경우 프리팹을 분리하거나 단일 프리팹에서 SO 스탯에 따라 스프라이트를 동적 변경

---

### 2단계: 골렘 고유 클래스 작성 (`EnemyGolem.cs`)

`EnemyBase`를 상속받는 `EnemyGolem` 클래스를 구현하여 무기 무작위 장착 및 드롭 메커니즘을 처리합니다.

```
[동작 흐름]
골렘 스폰 (Awake/Start)
  ➔ 게임에 정의된 근접 무기 목록 중 하나를 무작위로 선택
  ➔ GDD 9.7 강화 확률 롤을 굴림 (0강: 55%, 1강: 30%, 2강: 10%, 3강: 5%)
  ➔ 무기 공격력 + 강화 가산치를 골렘의 최종 공격력에 반영
사망 시 (Die)
  ➔ 장착하고 있던 해당 무기 아이템(강화 수치 유지)을 골렘 위치에 드롭
```

* **EnemyGolem.cs 상세 구현 명세**:

```csharp
using UnityEngine;

/// <summary>
/// 고대 골렘 (EnemyGolem) [Elite] — 스페셜 룸 (몬스터 방) 전용 미니 보스
/// </summary>
public class EnemyGolem : EnemyBase
{
    [Header("골렘 전용 설정")]
    [Tooltip("골렘이 장착할 수 있는 근접 무기(CloseWeapon) 목록")]
    [SerializeField] private CloseWeapon[] dropCandidateWeapons;

    [Tooltip("바닥에 아이템을 드롭할 때 소환할 ItemPickup 프리팹")]
    [SerializeField] private GameObject itemPickupPrefab;

    // 런타임 장착 무기 상태
    private WeaponInstance _holdingWeaponInstance;
    private int _weaponEnhanceLevel;

    protected override void Start()
    {
        base.Start();
        InitializeRandomWeapon();
    }

    /// <summary>
    /// 골렘 스폰 시 소검, 검, 쌍검 등 후보 무기 중 1종을 랜덤으로 장착하고,
    /// GDD 9.7 강화 확률에 따라 강화도를 결정합니다.
    /// </summary>
    private void InitializeRandomWeapon()
    {
        if (dropCandidateWeapons == null || dropCandidateWeapons.Length == 0)
        {
            Debug.LogWarning($"{name}: 드롭 가능한 무기 목록이 비어 있습니다.");
            return;
        }

        // 1. 무작위 무기 선택
        CloseWeapon selectedWeapon = dropCandidateWeapons[Random.Range(0, dropCandidateWeapons.Length)];
        
        // 2. 장비 생성 및 강화 레벨 결정
        _weaponEnhanceLevel = RollEnhanceLevel();
        
        // 3. WeaponInstance 인스턴스화
        var instance = selectedWeapon.CreateInstance() as WeaponInstance;
        if (instance != null)
        {
            instance.enhanceLevel = _weaponEnhanceLevel;
            _holdingWeaponInstance = instance;
            Debug.Log($"{name}: '{selectedWeapon.Name} (+{_weaponEnhanceLevel})' 장착 완료.");
        }
    }

    /// <summary>
    /// GDD 9.7 장비 강화 시스템 등장 강화도 확률 롤 적용
    /// (0강: 55%, 1강: 30%, 2강: 10%, 3강: 5%)
    /// </summary>
    private int RollEnhanceLevel()
    {
        float roll = Random.value;
        if (roll < 0.55f) return 0;
        if (roll < 0.85f) return 1;
        if (roll < 0.95f) return 2;
        return 3;
    }

    /// <summary>
    /// 공격 데미지 롤을 오버라이드하여 기본 데미지에 장착 무기의 데미지를 추가 적용합니다.
    /// </summary>
    protected override int RollAttackDamage()
    {
        // Golem 기본 공격력 (EnemyData에 정의된 값)
        int baseDamage = base.RollAttackDamage();

        // 무기가 장착된 경우 무기 공격력 가산
        if (_holdingWeaponInstance != null)
        {
            int weaponDamage = _holdingWeaponInstance.GetDamage();
            Debug.Log($"{name} 공격 롤: 기본 {baseDamage} + 무기 {weaponDamage} = 총 {baseDamage + weaponDamage}");
            return baseDamage + weaponDamage;
        }

        return baseDamage;
    }

    /// <summary>
    /// 골렘 사망 시 장착하고 있던 무기를 바닥에 드롭합니다.
    /// </summary>
    protected override void Die()
    {
        if (IsDead) return;

        // 사망 연출 및 Base 사망 처리 전 무기 드롭 실행
        DropWeaponOnFloor();

        base.Die();
    }

    /// <summary>
    /// 골렘 위치에 장착 중인 WeaponInstance를 바닥 픽업 오브젝트로 생성합니다.
    /// </summary>
    private void DropWeaponOnFloor()
    {
        if (_holdingWeaponInstance == null || itemPickupPrefab == null) return;

        // 아이템 픽업 프리팹 생성
        GameObject dropGo = Instantiate(itemPickupPrefab, transform.position, Quaternion.identity);
        ItemPickup pickup = dropGo.GetComponent<ItemPickup>();
        
        if (pickup != null)
        {
            // 인스턴스를 직접 주입하여 강화도 정보가 유지되도록 설정
            pickup.item = _holdingWeaponInstance;
            pickup.itemSO = _holdingWeaponInstance.Weapon; // 아이콘 표기를 위한 SO 등록
            pickup.Initialize(GridPosition);
            
            Debug.Log($"{name} 처치 완료: '{_holdingWeaponInstance.Weapon.Name} (+{_weaponEnhanceLevel})' 바닥에 드롭.");
        }
    }
}
```

---

### 3단계: 스페셜 방 스폰 연동 (`SpecialRoomRule`)

골렘은 일반 방 스폰 테이블에서 무작위로 나오는 것이 아니라, 스페셜 룸 생성 시 확정 배치되어야 합니다.

1. `SpecialRoom` 빌더/페인터 또는 맵 제너레이터 레벨에서 **'몬스터 방'** 타입이 결정되었을 때, 해당 방 중앙 위치에 골렘을 스폰하도록 훅을 연결합니다.
2. 현재 층(`depth`)을 체크하여 `Golem_Area1`, `Golem_Area2`, `Golem_Area3` 중 알맞은 데이터를 골라 스폰시킵니다.
3. 스페셜 룸에 진입하기 위한 열쇠 시스템 등과 엮여 동작하는지 확인합니다.

---

### 4단계: 애니메이터 및 트랜지션 설정

- `Walk`, `Attack`, `Hit`, `Die` 애니메이션 클립 연결
* 공격 애니메이션과 데미지 판정 박자가 어긋나지 않도록 트랜지션 지속 시간(`Transition Duration` ➔ 0) 및 `Has Exit Time` 해제 처리
* 피격 타이밍에 맞춰 피격 연출과 데미지 딜레이(`yield return new WaitForSeconds(0.2f)`) 연동

---

## 3. 검증 및 테스트 계획

### 수동 검증 항목

1. **스폰 테스트**: 디버그 콘솔 또는 테스트 씬을 이용해 각 에리어 골렘이 올바르게 스폰되는지 확인.
2. **무기 장착 및 스탯 보정 테스트**:
   * 골렘이 무작위 무기를 정상 장착했는지 로그 확인
   * 기본 공격력에 무기 공격력(및 강화도 수치)이 정상적으로 더해져 플레이어에게 아프게 들어오는지 확인
3. **전리품 드롭 테스트**: 골렘을 처치했을 때 들고 있던 무기(강화 수치 포함)가 바닥에 정상적으로 드롭되고, 플레이어가 이를 획득하여 장착할 수 있는지 확인.
4. **시각적 타이밍**: 골렘의 공격 모션(내려치기) 비주얼과 실제 플레이어가 데미지를 입는 타이밍이 싱크가 잘 맞는지 검증.
