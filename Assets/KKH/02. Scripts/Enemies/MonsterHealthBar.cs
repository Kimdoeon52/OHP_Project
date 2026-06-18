using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 몬스터 머리 위에 배치되어 체력 상태를 표시하는 월드 스페이스 UI 스크립트.
/// GDD 및 프리미엄 게임 연출을 위해 피격 시에만 노출(Auto-Hide)되며, 
/// 데미지량이 천천히 깎이는 듀얼 바(Delayed Bar) 연출을 지원합니다.
/// </summary>
public class MonsterHealthBar : MonoBehaviour
{
    [Header("연동 컴포넌트")]
    [Tooltip("체력을 추적할 대상 몬스터. 비워둘 경우 부모 오브젝트에서 자동으로 탐색합니다.")]
    [SerializeField] private EnemyBase targetEnemy;

    [Header("UI 슬라이더 설정")]
    [Tooltip("실제 체력을 즉시 반영하는 메인 슬라이더 (예: Red Bar)")]
    [SerializeField] private Slider mainHpSlider;
    
    [Tooltip("데미지를 입은 후 천천히 줄어드는 지연 슬라이더 (예: Yellow/White Bar)")]
    [SerializeField] private Slider delayedHpSlider;

    [Header("연출 옵션")]
    [Tooltip("체력이 100%일 때는 체력바를 숨길지 여부")]
    [SerializeField] private bool autoHideOnFullHp = true;
    
    [Tooltip("피격 후 지연 슬라이더가 깎이기 시작할 때까지 대기 시간 (초)")]
    [SerializeField] private float delayDuration = 0.5f;

    [Tooltip("지연 슬라이더가 깎이는 속도")]
    [SerializeField] private float drainSpeed = 2f;

    private Canvas _canvas;
    private Coroutine _drainCoroutine;
    private bool _isInitialized = false;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        if (targetEnemy == null)
        {
            targetEnemy = GetComponentInParent<EnemyBase>();
        }
    }

    private void Start()
    {
        if (targetEnemy == null)
        {
            Debug.LogError($"{name}: Target Enemy가 할당되지 않았으며, 부모 오브젝트에서도 찾을 수 없습니다.");
            gameObject.SetActive(false);
            return;
        }

        // 초기 상태 설정
        UpdateSliderValuesInstant();
        CheckVisibility();
        _isInitialized = true;
    }

    private void OnEnable()
    {
        if (_isInitialized)
        {
            UpdateSliderValuesInstant();
            CheckVisibility();
        }
    }

    private void Update()
    {
        if (targetEnemy == null || targetEnemy.IsDead)
        {
            SetVisibility(false);
            return;
        }

        float targetRatio = (float)targetEnemy.CurrentHp / targetEnemy.MaxHp;

        // 메인 HP 바의 값과 다를 경우 (체력이 변한 경우)
        if (Mathf.Abs(mainHpSlider.value - targetRatio) > 0.001f)
        {
            // 체력이 감소한 경우 듀얼 바 연출 시작
            if (targetRatio < mainHpSlider.value)
            {
                mainHpSlider.value = targetRatio;

                // 기존 연출 코루틴 재생 중이면 중지 후 새로 시작
                if (_drainCoroutine != null) StopCoroutine(_drainCoroutine);
                _drainCoroutine = StartCoroutine(DrainDelayedBarRoutine(targetRatio));
            }
            else
            {
                // 체력이 회복된 경우는 즉시 둘 다 업데이트
                UpdateSliderValuesInstant();
            }

            CheckVisibility();
        }
    }

    /// <summary>
    /// 두 슬라이더 값을 즉시 현재 타겟 체력 비율로 동기화합니다.
    /// </summary>
    private void UpdateSliderValuesInstant()
    {
        if (targetEnemy == null) return;
        float ratio = (float)targetEnemy.CurrentHp / targetEnemy.MaxHp;
        
        mainHpSlider.value = ratio;
        delayedHpSlider.value = ratio;
    }

    /// <summary>
    /// 체력 상태에 따라 체력바 자체를 노출하거나 숨깁니다.
    /// </summary>
    private void CheckVisibility()
    {
        if (targetEnemy == null || targetEnemy.IsDead)
        {
            SetVisibility(false);
            return;
        }

        if (autoHideOnFullHp)
        {
            // 데미지를 입었을 때만 활성화 (현재 체력 < 최대 체력)
            bool shouldShow = targetEnemy.CurrentHp < targetEnemy.MaxHp;
            SetVisibility(shouldShow);
        }
        else
        {
            SetVisibility(true);
        }
    }

    private void SetVisibility(bool visible)
    {
        if (_canvas != null)
        {
            _canvas.enabled = visible;
        }
        else
        {
            // 캔버스 컴포넌트가 없으면 게임 오브젝트 자체를 켜고 끔
            if (mainHpSlider.gameObject.activeSelf != visible)
            {
                mainHpSlider.gameObject.SetActive(visible);
                if (delayedHpSlider != null) delayedHpSlider.gameObject.SetActive(visible);
            }
        }
    }

    /// <summary>
    /// 메인 바가 즉시 깎인 후, 일정 시간 대기했다가 서서히 지연 바를 깎는 연출 코루틴
    /// </summary>
    private IEnumerator DrainDelayedBarRoutine(float targetRatio)
    {
        yield return new WaitForSeconds(delayDuration);

        while (delayedHpSlider.value > targetRatio)
        {
            delayedHpSlider.value = Mathf.MoveTowards(delayedHpSlider.value, targetRatio, Time.deltaTime * drainSpeed);
            yield return null;
        }

        delayedHpSlider.value = targetRatio;
    }
}
