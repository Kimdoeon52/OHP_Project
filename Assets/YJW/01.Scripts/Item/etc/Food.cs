using UnityEngine;

[CreateAssetMenu(fileName = "New Food", menuName = "Item/Etc/Food")]
public class Food : ItemBase
{
    [SerializeField] private int hungerBonus;

    public override ItemInstance CreateInstance()
    {
        return new FoodInstance { data = this, count = 1 };
    }

    public override void Use()
    {
        NYHPlayerStats.Instance.AddStat(NYHStatType.Hunger, hungerBonus);
    }

    public override string GetToolTipStats()
    {
        return $"[{LocalizedName}]\n" +
               $"--------------------\n" +
               $"{LocalizedDescription}\n" +
               $"{LocalizationManager.GetOrDefault("msg_생명파편효과", "Effect: Heals 1/20 of Max HP")}";
    }
}
