using UnityEngine;

public class RedPillar : EntityBase, IDamageable
{
    [Header("비석 스탯")]
    [SerializeField] private int maxHp = 1;
    [SerializeField] private int currentHp = 1;

    private LastBoss ownerBoss;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => currentHp <= 0;

    public void InitPillar(LastBoss boss, Vector2Int startPos)
    {
        ownerBoss = boss;
        Initialize(startPos);
        
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.Register(this);
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        currentHp = Mathf.Max(0, currentHp - amount);

        // 데미지 플로팅 텍스트 요청
        GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(
            transform.position, 
            amount.ToString(), 
            amount, 
            CombatTextType.Damage
        ));

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }

    private void Die()
    {
        if (ownerBoss != null)
        {
            ownerBoss.OnPillarDestroyed();
        }
        
        Destroy(gameObject);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.Unregister(this);
        }
    }
}
