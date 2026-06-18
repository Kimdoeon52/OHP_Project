using System.Collections;
using PrototypeRT;
using UnityEngine;

public class PlayerDeathSequenceController : MonoBehaviour
{
    private const string InputBlockSource = "PlayerDeathSequence";

    [Header("References")]
    [SerializeField] private GridPlayer player;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private DeathGameOverView gameOverView;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float cameraZoomSize = 2.2f;
    [SerializeField, Min(0.01f)] private float cameraZoomSeconds = 0.7f;
    [SerializeField, Min(0f)] private float deathAnimationWaitSeconds = 1.2f;
    [SerializeField, Min(0f)] private float overlayFadeSeconds = 0.7f;

    [Header("Dark Overlay")]
    [SerializeField, Range(0f, 1f)] private float darkOverlayAlpha = 0.75f;
    [SerializeField] private int darkOverlaySortingOrder = 9000;
    [SerializeField] private int playerDeathSortingOrder = 9010;
    [SerializeField] private string darkOverlaySortingLayerName;

    private bool _isPlaying;
    private int _originalPlayerSortingOrder;
    private string _originalPlayerSortingLayerName;
    private SpriteRenderer _darkOverlayRenderer;
    private Coroutine _sequenceRoutine;

    private void Awake()
    {
        ResolveReferences();
        CreateDarkOverlay();
        SetDarkOverlayAlpha(0f);
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDied += OnPlayerDied;
        GameEvents.OnTimeUp += OnTimeUp;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= OnPlayerDied;
        GameEvents.OnTimeUp -= OnTimeUp;
        KeyBindingManager.SetUiInputBlock(InputBlockSource, false);
    }

    private void OnDestroy()
    {
        KeyBindingManager.SetUiInputBlock(InputBlockSource, false);
    }

    private void LateUpdate()
    {
        UpdateDarkOverlayTransform();
    }

    private void OnPlayerDied()
    {
        if (_isPlaying)
            return;

        _sequenceRoutine = StartCoroutine(PlaySequence());
    }

    private void OnTimeUp()
    {
        if (_isPlaying)
            return;

        _sequenceRoutine = StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        _isPlaying = true;
        ResolveReferences();
        KeyBindingManager.SetUiInputBlock(InputBlockSource, true);
        TimeSystem.Instance?.StopTimer();

        RaisePlayerAboveOverlay();

        if (cameraFollow != null && player != null)
            cameraFollow.BeginDeathFocus(player.transform, cameraZoomSize, cameraZoomSeconds);

        if (gameOverView != null)
            gameOverView.ShowOverlay(0f);

        yield return FadeDarkOverlay(darkOverlayAlpha, overlayFadeSeconds);

        if (deathAnimationWaitSeconds > 0f)
            yield return new WaitForSecondsRealtime(deathAnimationWaitSeconds);

        if (gameOverView != null)
        {
            gameOverView.PlayGameOverSequence();
            yield return new WaitForSecondsRealtime(gameOverView.GameOverRevealSeconds);
        }
        else
        {
            Debug.LogWarning("PlayerDeathSequenceController: GameOverView가 연결되지 않아 버튼 UI를 표시할 수 없습니다.");
        }

        _sequenceRoutine = null;
    }

    private IEnumerator FadeDarkOverlay(float targetAlpha, float duration)
    {
        if (_darkOverlayRenderer == null)
            yield break;

        float startAlpha = _darkOverlayRenderer.color.a;
        if (duration <= 0f)
        {
            SetDarkOverlayAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetDarkOverlayAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetDarkOverlayAlpha(targetAlpha);
    }

    private void ResolveReferences()
    {
        if (player == null)
            player = FindFirstObjectByType<GridPlayer>();

        if (playerSpriteRenderer == null && player != null)
            playerSpriteRenderer = player.GetComponent<SpriteRenderer>();

        if (cameraFollow == null)
            cameraFollow = CameraFollow.Instance != null ? CameraFollow.Instance : FindFirstObjectByType<CameraFollow>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null && cameraFollow != null)
            targetCamera = cameraFollow.GetComponent<Camera>();

        if (gameOverView == null)
            gameOverView = FindFirstObjectByType<DeathGameOverView>(FindObjectsInactive.Include);
    }

    private void RaisePlayerAboveOverlay()
    {
        if (playerSpriteRenderer == null)
            return;

        _originalPlayerSortingOrder = playerSpriteRenderer.sortingOrder;
        _originalPlayerSortingLayerName = playerSpriteRenderer.sortingLayerName;

        if (!string.IsNullOrWhiteSpace(darkOverlaySortingLayerName))
            playerSpriteRenderer.sortingLayerName = darkOverlaySortingLayerName;

        playerSpriteRenderer.sortingOrder = playerDeathSortingOrder;
    }

    public void RestorePlayerSorting()
    {
        if (playerSpriteRenderer == null)
            return;

        playerSpriteRenderer.sortingLayerName = _originalPlayerSortingLayerName;
        playerSpriteRenderer.sortingOrder = _originalPlayerSortingOrder;
    }

    private void CreateDarkOverlay()
    {
        if (_darkOverlayRenderer != null)
            return;

        GameObject overlay = new GameObject("RuntimeDeathDarkOverlay");
        overlay.transform.SetParent(transform, false);

        _darkOverlayRenderer = overlay.AddComponent<SpriteRenderer>();
        _darkOverlayRenderer.sprite = CreateOverlaySprite();
        _darkOverlayRenderer.color = new Color(0f, 0f, 0f, 0f);
        _darkOverlayRenderer.sortingOrder = darkOverlaySortingOrder;

        if (!string.IsNullOrWhiteSpace(darkOverlaySortingLayerName))
            _darkOverlayRenderer.sortingLayerName = darkOverlaySortingLayerName;
        else if (playerSpriteRenderer != null)
            _darkOverlayRenderer.sortingLayerName = playerSpriteRenderer.sortingLayerName;
    }

    private static Sprite CreateOverlaySprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "RuntimeDeathDarkOverlayTexture",
            filterMode = FilterMode.Point
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void SetDarkOverlayAlpha(float alpha)
    {
        if (_darkOverlayRenderer == null)
            return;

        Color color = _darkOverlayRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        _darkOverlayRenderer.color = color;
        _darkOverlayRenderer.enabled = alpha > 0f;
        UpdateDarkOverlayTransform();
    }

    private void UpdateDarkOverlayTransform()
    {
        if (_darkOverlayRenderer == null || targetCamera == null)
            return;

        Transform overlayTransform = _darkOverlayRenderer.transform;
        Vector3 cameraPosition = targetCamera.transform.position;
        overlayTransform.position = new Vector3(cameraPosition.x, cameraPosition.y, 0f);

        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;
        overlayTransform.localScale = new Vector3(halfWidth * 2.2f, halfHeight * 2.2f, 1f);
    }
}
