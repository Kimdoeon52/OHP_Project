using UnityEngine;

[CreateAssetMenu(fileName = "FragmentsOfLife", menuName = "Item/Etc/FragmentsOfLife")]
public class FragmentsOfLife : ItemBase
{
    public override void Use()
    {
        GridPlayerHealth health = GridPlayerHealth.Instance;
        if (health == null)
        {
            Debug.LogWarning("FragmentsOfLife: GridPlayerHealth가 씬에 없어 효과를 적용하지 못했습니다.");
            return;
        }
        if (health.CurrentHp >= health.MaxHp)
            return;

        int healAmount = Mathf.Max(1, health.MaxHp / 20);
        health.Heal(healAmount);
    }

    public override ItemInstance CreateInstance()
    {
        return new FragmentOfLifeInstance { data = this, count = 1 };
    }

    public override string GetToolTipStats()
    {
        return $"[{LocalizedName}]\n" +
               $"--------------------\n" +
               $"{LocalizedDescription}\n" +
               $"{LocalizationManager.GetOrDefault("msg_생명파편효과", "Effect: Heals 1/20 of Max HP")}";
    }
}
