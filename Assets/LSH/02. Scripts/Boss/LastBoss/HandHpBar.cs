using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HandHpBar : MonoBehaviour
{
    [Header("체력이미지")]
    public GameObject allHpBar;
    public Image fillimage; // HP 바의 Fill 이미지
    private LastBossHand hand;

    [Header("HP 변동 진동")]
    [SerializeField] private float shakeDuration = 0.2f;   // 진동 시간
    [SerializeField] private float shakeStrength = 4f;     // 진동 폭(픽셀)
    [SerializeField] private int shakeVibrato = 16;        // 진동 횟수
    [SerializeField] private float shakeRandomness = 90f;  // 방향 무작위성

    private Vector3 _originalLocalPos;     // 진동 후 복귀 기준점 복사본
    private bool _hasOriginalPos;
    private Vector3 originalLocalPos
    {
        get
        {
            if (!_hasOriginalPos && fillimage != null)
            {
                _originalLocalPos = fillimage.rectTransform.localPosition;
                _hasOriginalPos = true;
            }
            return _originalLocalPos;
        }
    }
    private Tween activeShakeTween;       // 같은 트윈이 누적되지 않게 관리

    private void Awake()
    {
        if (fillimage != null)
        {
            _originalLocalPos = fillimage.rectTransform.localPosition;
            _hasOriginalPos = true;
        }
    }

    private void OnEnable()
    {
        if (allHpBar != null) allHpBar.SetActive(true);
    }

    public void Init(LastBossHand target)
    {
        hand = target;
        RefreshVisual().Forget(); // 초기화는 진동 없이 표시만
    }

    public void UpdateBar()
    {
        if (hand == null) return;
        if (allHpBar != null && !allHpBar.activeSelf)
        {
            allHpBar.SetActive(true); // 혹시 꺼져 있었다면 다시 켜기
        }
        RefreshVisual().Forget();
        PlayShake();
    }

    private async UniTask RefreshVisual()
    {
        if (hand == null || fillimage == null) return;

        float ratio = (float)hand.CurrentHp / hand.MaxHp;
        fillimage.fillAmount = ratio;

        if (ratio <= 0f)
        {
            await UniTask.Delay(1000); // 1초 대기 후 HP 바 사라짐
            // 1초 지연 후 대기하는 동안 다른 손으로 타겟이 전환되지 않았고, 현재 타겟의 체력이 여전히 0 이하인 경우에만 비활성화합니다.
            if (hand != null && hand.CurrentHp <= 0)
            {
                if (allHpBar != null) allHpBar.SetActive(false);
            }
        }
    }

    private void PlayShake()
    {
        if (fillimage == null) return;

        if (activeShakeTween != null && activeShakeTween.IsActive())
        {
            activeShakeTween.Kill();
            fillimage.rectTransform.localPosition = originalLocalPos;
        }
        activeShakeTween = fillimage.rectTransform.DOShakePosition(
            shakeDuration, shakeStrength, shakeVibrato, shakeRandomness, fadeOut: true)
            .OnComplete(() => fillimage.rectTransform.localPosition = originalLocalPos);
    }
}
