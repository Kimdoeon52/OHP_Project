using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(GridPlayer))]
public class PlayerDamageFeedback : MonoBehaviour
{
    [Header("피격 깜빡임")]
    [Tooltip("피격 한 번에 투명해졌다 돌아오는 횟수입니다.")]
    [SerializeField, Min(1)] private int blinkCount = 3;

    [Tooltip("투명 상태와 원래 상태를 각각 유지하는 시간입니다. 값이 작을수록 빠르게 깜빡입니다.")]
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.05f;

    [Tooltip("깜빡일 때 적용할 알파값입니다. 0에 가까울수록 더 투명해집니다.")]
    [SerializeField, Range(0f, 1f)] private float hitAlpha = 0.25f;

    private GridPlayer _player;
    private SpriteRenderer _spriteRenderer;
    private Coroutine _flashCoroutine;
    private Color _baseColor;
    private bool _isSubscribed;

    private void Awake()
    {
        _player = GetComponent<GridPlayer>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _baseColor = _spriteRenderer.color;
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        if (_spriteRenderer != null)
            _spriteRenderer.color = _baseColor;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (_isSubscribed) return;

        if (_player == null)
            _player = GetComponent<GridPlayer>();

        if (_player == null || _player.Health == null)
            return;

        _player.Health.OnDamaged += OnDamaged;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed || _player == null || _player.Health == null) return;

        _player.Health.OnDamaged -= OnDamaged;
        _isSubscribed = false;
    }

    private void OnDamaged(int damage)
    {
        if (_spriteRenderer == null || _player.Health.IsDead) return;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        Color hitColor = _baseColor;
        hitColor.a = hitAlpha;

        // 짧은 알파 변화만 사용해 피격 인지는 주되 조작 흐름을 방해하지 않는다.
        for (int i = 0; i < blinkCount; i++)
        {
            _spriteRenderer.color = hitColor;
            yield return new WaitForSeconds(blinkInterval);

            _spriteRenderer.color = _baseColor;
            yield return new WaitForSeconds(blinkInterval);
        }

        _spriteRenderer.color = _baseColor;
        _flashCoroutine = null;
    }
}
