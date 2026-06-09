using UnityEngine;

public enum CurseType
{
    Slow,       // 둔화 저주
    Bleed,      // 출혈 저주
    Displace,   // 변위 저주
    Alert,      // 경보 저주
    Drain,      // 착취 저주
    Polarize    // 양극화 저주
}

[CreateAssetMenu(fileName = "New Curse", menuName = "Item/Curse")]
public class CurseData : ScriptableObject
{
    [SerializeField] private string curseName;
    [SerializeField] private string description;
    [SerializeField] private CurseType curseType;
    [SerializeField] private float triggerChance;   // 발동 확률 (0~1)
    [SerializeField] private EquipmentSlot targetSlot;

    [Header("출혈 저주")]
    [SerializeField] private int bleedMin;
    [SerializeField] private int bleedMax;

    [Header("착취 저주")]
    [SerializeField] private int drainPercentage;

    public string CurseName => curseName;
    public string Description => description;
    public CurseType Type => curseType;
    public EquipmentSlot TargetSlot => targetSlot;

    // 장착 시 호출 (둔화 저주)
    public void OnEquip()
    {
        switch (curseType)
        {
            case CurseType.Slow:
                NYHPlayerStats.Instance.AddStat(NYHStatType.MoveSpeed, -1);
                break;
        }
    }

    // 장착 해제 시 호출 (둔화 저주 원상복구)
    public void OnUnequip()
    {
        switch (curseType)
        {
            case CurseType.Slow:
				NYHPlayerStats.Instance.AddStat(NYHStatType.MoveSpeed, 1);
				break;
        }
    }

    // 피격 시 호출 (갑옷 저주)
    public void OnHit()
    {
        switch (curseType)
        {
            case CurseType.Bleed:
                if (Random.value < triggerChance)
                {
                    int damage = Random.Range(bleedMin, bleedMax + 1);
                    PlayerStats.Instance.AddStat(StatType.Hp, -damage);
                }
                break;

            case CurseType.Displace:
                if (Random.value < triggerChance)
                {
                    GridSystem.Instance.MoveRanPos();
                }
                break;
        }
    }

    // 공격 시 호출 (무기 저주), ref로 데미지 수정 가능
    public void OnAttack(ref int damage)
    {
        switch (curseType)
        {
            case CurseType.Alert:
                if (Random.value < triggerChance)
                {
                    GameManager.Instance.isAlerted = true;
                }
                break;

            case CurseType.Drain:
                if (Random.value < triggerChance)
                {
                    PlayerStats.Instance.AddStat(StatType.Hp, -drainPercentage);
                }
                break;

            case CurseType.Polarize:
                damage = Random.value < 0.5f ? Mathf.RoundToInt(damage * 1.5f) : 0;
                break;
        }
    }
}
