using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(GridPlayer))]
public class PlayerDamageFeedback : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.12f;

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
        _spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(flashDuration);
        _spriteRenderer.color = _baseColor;
        _flashCoroutine = null;
    }
}
