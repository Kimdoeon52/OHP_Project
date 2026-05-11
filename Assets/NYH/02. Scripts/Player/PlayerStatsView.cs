using TMPro;
using UnityEngine;

public class PlayerStatsView : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private GridPlayerHealth playerHealth;

    [Header("통합 표시")]
    [Tooltip("한 TextMeshPro 텍스트에 모든 스탯을 줄 단위로 표시합니다.")]
    [SerializeField] private TMP_Text summaryText;

    [Header("개별 표시")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text strengthText;
    [SerializeField] private TMP_Text moveSpeedText;
    [SerializeField] private TMP_Text attackSpeedText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text evasionText;

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Start()
    {
        ResolveReferences();
        Subscribe();
        Refresh();
    }

    private void ResolveReferences()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<GridPlayerHealth>();
    }

    private void Subscribe()
    {
        if (NYHPlayerStats.Instance != null)
        {
            NYHPlayerStats.Instance.OnStatsChanged -= Refresh;
            NYHPlayerStats.Instance.OnStatsChanged += Refresh;
            NYHPlayerStats.Instance.OnStatAdded -= OnStatAdded;
            NYHPlayerStats.Instance.OnStatAdded += OnStatAdded;
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= Refresh;
            playerHealth.OnHealthChanged += Refresh;
        }
    }

    private void Unsubscribe()
    {
        if (NYHPlayerStats.Instance != null)
        {
            NYHPlayerStats.Instance.OnStatsChanged -= Refresh;
            NYHPlayerStats.Instance.OnStatAdded -= OnStatAdded;
        }

        if (playerHealth != null)
            playerHealth.OnHealthChanged -= Refresh;
    }

    public void Refresh()
    {
        if (playerHealth == null)
            ResolveReferences();

        NYHPlayerStats playerStats = NYHPlayerStats.Instance;
        if (playerStats == null && playerHealth == null)
        {
            SetText(summaryText, "");
            return;
        }

        string health = playerHealth != null ? $"{playerHealth.CurrentHp}/{playerHealth.MaxHp}" : "-";
        string strength = playerStats != null ? playerStats.Strength.ToString() : "-";
        string moveSpeed = playerStats != null ? playerStats.MoveSpeed.ToString() : "-";
        string attackSpeed = playerStats != null ? playerStats.AttackSpeed.ToString() : "-";
        string level = playerStats != null ? playerStats.Level.ToString() : "-";
        string defense = playerStats != null ? playerStats.Defense.ToString() : "-";
        string evasion = playerStats != null ? $"{playerStats.Evasion}%" : "-";

        SetText(healthText, $"체력 {health}");
        SetText(strengthText, $"힘 {strength}");
        SetText(moveSpeedText, $"이동 속도 {moveSpeed}");
        SetText(attackSpeedText, $"공격 속도 {attackSpeed}");
        SetText(levelText, $"레벨 {level}");
        SetText(defenseText, $"방어력 {defense}");
        SetText(evasionText, $"회피 {evasion}");

        SetText(summaryText,
            $"체력 {health}\n" +
            $"힘 {strength}\n" +
            $"이동 속도 {moveSpeed}\n" +
            $"공격 속도 {attackSpeed}\n" +
            $"레벨 {level}\n" +
            $"방어력 {defense}\n" +
            $"회피 {evasion}");
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private void OnStatAdded(NYHStatType type, int amount)
    {
        Refresh();
    }
}
