using UnityEngine;
using UnityEngine.UI;

public class BossHpBar : MonoBehaviour
{
    public Image fillimage; // HP 바의 Fill 이미지
    private BossBase boss;
    public Text text;
    public void Init(BossBase target)
    {
        boss = target;
        UpdateBar();
    }
    public void UpdateBar()
    {
        if (boss == null) return;
        float ratio = (float)boss.currentHp / boss.maxHp;
        fillimage.fillAmount = ratio;
        text.text = boss.currentHp.ToString() + " / " + boss.maxHp.ToString();
    }
}
