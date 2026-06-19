using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrototypeRT;

// 마법 부여 팝업 — 무기/방어구 선택 후 장착 장비에 랜덤 마법 부여
public class EnchantPopup : MonoBehaviour
{
    public bool IsOpen => panel != null && panel.activeSelf;

    [Header("패널")]
    [SerializeField] private GameObject panel;

    [Header("표시")]
    [SerializeField] private TMP_Text messageText;

    [Header("버튼")]
    [SerializeField] private Button weaponButton;
    [SerializeField] private Button armorButton;
    [SerializeField] private Button cancelButton;

    private EnchantAltar _altar;
    private NYHEquipmentController _equipment;

    private void Awake()
    {
        if (weaponButton != null)
            weaponButton.onClick.AddListener(OnWeapon);
        if (armorButton != null)
            armorButton.onClick.AddListener(OnArmor);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);
        if (panel != null)
            panel.SetActive(false);
    }

    public void Open(EnchantAltar altar)
    {
        if (altar == null)
            return;

        _altar = altar;
        SetText(messageText, "");
        if (panel != null)
            panel.SetActive(true);
    }

    public void Close()
    {
        _altar = null;
        if (panel != null)
            panel.SetActive(false);
    }

    private void OnWeapon()
    {
        ApplyMagic(EquipmentSlot.Weapon, _altar != null ? _altar.RolledWeaponMagic : null, "장착 무기 없음");
    }

    private void OnArmor()
    {
        ApplyMagic(EquipmentSlot.Armor, _altar != null ? _altar.RolledArmorMagic : null, "장착 방어구 없음");
    }

    // 장착 슬롯 장비에 마법 부여 (덮어쓰기 허용)
    private void ApplyMagic(EquipmentSlot slot, MagicData magic, string emptyMessage)
    {
        if (_altar == null)
            return;

        if (magic == null)
        {
            SetText(messageText, "부여할 마법 없음");
            return;
        }

        if (_equipment == null)
            _equipment = FindFirstObjectByType<NYHEquipmentController>();
        if (_equipment == null)
        {
            SetText(messageText, "장비 시스템 없음");
            return;
        }

        if (!_equipment.TryGetEquipped(slot, out NYHEquipmentItemInstance item)
            || item.SourceInstance is not EquipmentInstance inst)
        {
            SetText(messageText, emptyMessage);
            return;
        }

        inst.magic = magic;

        // 장착 UI/스탯 재계산 (해제·재장착 없이 내부 값만 변경)
        if (_equipment.TryGetEquippedInventoryItem(slot, 0, out InventoryItem view))
            _equipment.NotifyEquippedInventoryItemStateChanged(view);

        _altar.MarkUsed();
        Close();
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
