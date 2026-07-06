using System.Collections.Generic;
using PrototypeRT;
using UnityEngine;

// 장착 중인 미감정 장비를 시간이 흐르는 동안 자동 감정한다 (세이프룸 정지 시간 제외).
public class AutoAppraiseController : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("장비 컨트롤러. 비우면 씬에서 자동으로 찾음")]
    [SerializeField] private NYHEquipmentController equipment;

    [Header("설정")]
    [Tooltip("자동 감정 기준 시간(초)")]
    [SerializeField] private float baseAppraiseSeconds = 90f;

    // 슬롯별 감정 진행 시간 — 장비 인스턴스 기준으로 누적
    private readonly Dictionary<EquipmentInstance, float> _progress = new();

    // 이번 프레임에 장착 확인된 미감정 인스턴스
    private readonly HashSet<EquipmentInstance> _active = new();

    // 이번 프레임에 더는 장착되어 있지 않아 제거할 진행 항목
    private readonly List<EquipmentInstance> _stale = new();

    private TraitController _trait;

    // 스캔할 장비 슬롯 목록 (무기/방어구/반지 2칸)
    private static readonly (EquipmentSlot Slot, int Index)[] Slots =
    {
        (EquipmentSlot.Weapon, 0),
        (EquipmentSlot.Armor, 0),
        (EquipmentSlot.Ring, 0),
        (EquipmentSlot.Ring, 1)
    };

    private void Update()
    {
        NYHEquipmentController controller = ResolveEquipment();
        if (controller == null)
            return;

        // 세이프룸 등 타이머 정지 구간에서는 감정 시간이 흐르지 않는다.
        TimeSystem time = TimeSystem.Instance;
        if (time == null || !time.IsRunning)
            return;

        float dt = Time.deltaTime;
        _active.Clear();

        for (int i = 0; i < Slots.Length; i++)
        {
            EquipmentSlot slot = Slots[i].Slot;
            int index = Slots[i].Index;

            if (!controller.TryGetEquipped(slot, index, out NYHEquipmentItemInstance item) || item == null)
                continue;

            if (item.SourceInstance is not EquipmentInstance equip)
                continue;
            if (equip.data == null || !equip.data.CanAnalyze || equip.isAnalyzed)
                continue;

            _active.Add(equip);

            float threshold = GetThreshold(slot);
            _progress.TryGetValue(equip, out float elapsed);
            elapsed += dt;

            if (elapsed >= threshold)
            {
                Appraise(controller, slot, index, equip);
                _progress.Remove(equip);
            }
            else
            {
                _progress[equip] = elapsed;
            }
        }

        PruneStale();
    }

    // 장착이 풀렸거나 감정된 인스턴스의 진행값을 정리한다.
    private void PruneStale()
    {
        if (_progress.Count == 0)
            return;

        _stale.Clear();
        foreach (EquipmentInstance key in _progress.Keys)
        {
            if (!_active.Contains(key))
                _stale.Add(key);
        }

        for (int i = 0; i < _stale.Count; i++)
            _progress.Remove(_stale[i]);
    }

    // 슬롯별 감정 완료 시간 — 감정 특성이 무기/방어구 속도를 올리고 2레벨은 무기 즉시 감정
    private float GetThreshold(EquipmentSlot slot)
    {
        TraitController trait = ResolveTrait();

        if (slot == EquipmentSlot.Weapon)
        {
            if (trait != null && trait.ShouldInstantlyIdentifyWeapon())
                return 0f;

            float mult = trait != null ? trait.GetIdentifyWeaponSpeedMultiplier() : 1f;
            return baseAppraiseSeconds / mult;
        }

        if (slot == EquipmentSlot.Armor)
        {
            float mult = trait != null ? trait.GetIdentifyArmorSpeedMultiplier() : 1f;
            return baseAppraiseSeconds / mult;
        }

        return baseAppraiseSeconds;
    }

    // 감정 처리 + 장착 UI/스탯 갱신
    private void Appraise(NYHEquipmentController controller, EquipmentSlot slot, int index, EquipmentInstance equip)
    {
        equip.isAnalyzed = true;

        if (controller.TryGetEquippedInventoryItem(slot, index, out InventoryItem view) && view != null)
            controller.NotifyEquippedInventoryItemStateChanged(view);
    }

    private NYHEquipmentController ResolveEquipment()
    {
        if (equipment == null)
            equipment = FindFirstObjectByType<NYHEquipmentController>();

        return equipment;
    }

    private TraitController ResolveTrait()
    {
        if (_trait == null)
            _trait = TraitController.FindAvailableInstance();

        return _trait;
    }
}
