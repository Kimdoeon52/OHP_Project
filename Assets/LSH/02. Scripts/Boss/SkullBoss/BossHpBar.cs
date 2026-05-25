using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BossHpBar : MonoBehaviour
{
    [Header("체력이미지")]
    public Image fillimage; // HP 바의 Fill 이미지
    private BossBase boss;
    //public Text text;

    [Header("HP 변동 진동")]
    [SerializeField] private float shakeDuration = 0.2f;   // 진동 시간
    [SerializeField] private float shakeStrength = 4f;     // 진동 폭(픽셀)
    [SerializeField] private int shakeVibrato = 16;        // 진동 횟수
    [SerializeField] private float shakeRandomness = 90f;  // 방향 무작위성

    private Vector3 originalLocalPos;     // 진동 후 복귀 기준점
    private Tween activeShakeTween;       // 같은 트윈이 누적되지 않게 관리

    private void Awake()
    {
        originalLocalPos = transform.localPosition;
    }

    public void Init(BossBase target)
    {
        boss = target;
        RefreshVisual(); // 초기화는 진동 없이 표시만
    }

    public void UpdateBar()
    {
        if (boss == null) return;
        RefreshVisual();
        PlayShake();
    }

    private void RefreshVisual()
    {
        float ratio = (float)boss.currentHp / boss.maxHp;
        fillimage.fillAmount = ratio;
        //text.text = boss.currentHp.ToString() + " / " + boss.maxHp.ToString();
    }

    // HP가 줄어들 때마다 호출. 같은 진동이 진행 중이면 끊고 새로 시작 — 위치 어긋남 방지.
    private void PlayShake()
    {
        if (activeShakeTween != null && activeShakeTween.IsActive())
        {
            activeShakeTween.Kill();
            transform.localPosition = originalLocalPos;
        }
        activeShakeTween = transform.DOShakePosition(
            shakeDuration, shakeStrength, shakeVibrato, shakeRandomness, fadeOut: true)
            .OnComplete(() => transform.localPosition = originalLocalPos);
    }
}
