using UnityEngine;
using PrototypeRT;

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

    private bool _golemDead = false;
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

        // 시드 결정론 유지 — 맵 생성과 같은 난수원을 쓴다
        DungeonRandom rng = MapGenerator.Instance != null ? MapGenerator.Instance.Rng : null;
        if (rng == null)
        {
            Debug.LogWarning($"{name}: MapGenerator 없음 — 무기 롤링 생략.");
            return;
        }

        // 1. 무작위 무기 선택
        CloseWeapon selectedWeapon = dropCandidateWeapons[rng.Range(0, dropCandidateWeapons.Length)];

        // 2. 장비 생성 및 강화 레벨 결정
        _weaponEnhanceLevel = ItemRoll.Enhance(rng);

        // 3. WeaponInstance 인스턴스화
        var instance = selectedWeapon.CreateInstance() as WeaponInstance;
        if (instance != null)
        {
            instance.enhanceLevel = _weaponEnhanceLevel;
            instance.curse = EnemyLootDropper.RollCurseForEquipment(instance, rng);
            // 골렘이 들고 있던 무기 — 감정 완료 상태로 드롭
            instance.isAnalyzed = true;
            _holdingWeaponInstance = instance;
            Debug.Log($"{name}: '{selectedWeapon.Name} (+{_weaponEnhanceLevel})' 장착 완료.");
        }
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
        if (_golemDead) return;
        _golemDead = true;
        // 사망 연출 및 Base 사망 처리 전 무기 드롭 실행
        DropWeaponOnFloor();

        base.Die();
    }

    /// <summary>
    /// 골렘 위치에 장착 중인 WeaponInstance를 바닥 픽업 오브젝트로 생성합니다.
    /// GroundItemController가 있으면 그쪽으로 위임하고, 없으면 직접 ItemPickup을 스폰합니다.
    /// </summary>
    private void DropWeaponOnFloor()
    {
        if (_holdingWeaponInstance == null) return;

        // ── 우선 경로: GroundItemController에 위임 (인벤토리 시스템 통합) ──
        var groundItemController = FindFirstObjectByType<GroundItemController>();
        if (groundItemController != null)
        {
            groundItemController.DropItem(_holdingWeaponInstance, 1, GridPosition);
            Debug.Log($"{name} 처치 완료: '{_holdingWeaponInstance.Weapon.Name} (+{_weaponEnhanceLevel})' GroundItemController로 드롭.");
            return;
        }

        // ── 폴백 경로: 직접 ItemPickup 스폰 ──
        if (itemPickupPrefab == null)
        {
            Debug.LogWarning($"{name}: GroundItemController 없고 itemPickupPrefab 미할당 — 무기 드롭 실패.");
            return;
        }

        GameObject dropGo = Instantiate(itemPickupPrefab, transform.position, Quaternion.identity);
        ItemPickup pickup = dropGo.GetComponent<ItemPickup>();

        if (pickup != null)
        {
            // itemSO를 설정하면 ItemPickup.Start()에서 새 인스턴스로 덮어써 강화도가 사라지므로
            // item 인스턴스만 직접 주입하고 itemSO는 설정하지 않는다.
            pickup.item = _holdingWeaponInstance;
            pickup.Initialize(GridPosition);

            Debug.Log($"{name} 처치 완료: '{_holdingWeaponInstance.Weapon.Name} (+{_weaponEnhanceLevel})' 직접 드롭.");
        }
    }
}
