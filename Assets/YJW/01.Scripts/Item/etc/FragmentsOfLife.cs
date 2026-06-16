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
        return $"[{Name}]\n" +
               $"--------------------\n" +
               $"{Description}\n" +
               $"효과: 최대 체력의 1/20 회복";
    }
}
