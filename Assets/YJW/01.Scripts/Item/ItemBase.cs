using UnityEngine;

public enum ItemType
{
    Equipment,
    potion,
    Scroll,
    Others,
    Relic
}
public enum EquipmentSlot
{
    None,          // 장착 불가
    Weapon,        // 무기
    Armor,         // 갑옷
    Ring,          // 반지
    Artifact,      // 유물
    Rune           // 룬
}
public class ItemBase : ScriptableObject
{
    [SerializeField] private int id;
    [SerializeField] protected string itemName;
    [SerializeField] protected string description;
    [SerializeField] protected Sprite icon;
    [SerializeField] protected ItemType itemType;
    [SerializeField] protected EquipmentSlot slot;
    [SerializeField] protected int width;
    [SerializeField] protected int height;
    [SerializeField] private bool canEnhance;
    [SerializeField] private bool canCurse;
    [SerializeField] private bool canAnalyze;

    public int Id => id;
    public string Name { get => itemName; set => itemName = value; }
    public string Description { get => description; set => description = value; }
    public Sprite Icon { get => icon; set => icon = value; }
    public ItemType ItemType { get => itemType; set => itemType = value; }
    public EquipmentSlot Slot => slot;
    public int Width { get => width; set => width = value; }
    public int Height { get => height; set => height = value; }
    public bool CanEnhance => canEnhance;
    public bool CanCurse => canCurse;
    public bool CanAnalyze => canAnalyze;

    public virtual void Use() { }
    public virtual void Use(ItemInstance target) { }
    public virtual string GetToolTipStats() { 
        return $"{itemName} \n + " +
               $"{Description}"; 
    }
	public virtual ItemInstance CreateInstance(){ return new ItemInstance { data = this }; }
}
