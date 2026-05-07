using Unity.VisualScripting;
using UnityEngine;

public enum ItmeType
{
    Equipment,
    potion,
    Scroll,
    Others,
    Relic
}
public class ItemBase : ScriptableObject
{
    [SerializeField] protected string itemName;
    [SerializeField] protected string description;
    [SerializeField] protected Sprite icon;
    [SerializeField] protected ItmeType itemType;
    [SerializeField] protected int width;
    [SerializeField] protected int height;

    public string Name { get => itemName; set => itemName = value; }
    public string Description { get => description; set => description = value; }
    public Sprite Icon { get => icon; set => icon = value; }
    public ItmeType ItemType { get => itemType; set => itemType = value; }
    public int Width { get => width; set => width = value; }
    public int Height { get => height; set => height = value; }

    public virtual void Use() { }
}
