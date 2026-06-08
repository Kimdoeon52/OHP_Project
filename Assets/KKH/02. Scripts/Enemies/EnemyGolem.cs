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
