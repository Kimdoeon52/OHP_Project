using UnityEngine;
using UnityEngine.UI;

// 플레이어 HP UIbar 업데이트 스크립트
public class PlayerHealthBarUI : MonoBehaviour
{
    [SerializeField] private GridPlayerHealth helth;
    [SerializeField] private Image fillImage;

    private void Awake()
    {
        if(helth == null)
            helth = FindFirstObjectByType<GridPlayerHealth>();
    }

    private void OnEnable()
    {
        if(helth == null) return;

        helth.OnHealthChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if(helth == null) return;

        helth.OnHealthChanged -= Refresh;
    }


    // Hp Uibar에 정보를 전달
    private void Refresh()
    {
        if(fillImage == null || helth == null) return;
        fillImage.fillAmount = helth.MaxHp > 0
        ? (float)helth.CurrentHp / helth.MaxHp
        : 0f;
    }
}