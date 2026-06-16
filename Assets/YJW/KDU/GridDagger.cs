using System.Collections;
using UnityEngine;

/// <summary>
/// 던져진 후 비행 → 착지 → 맵에 남음. 플레이어가 착지 칸 밟으면 회수됨.
/// 비행 중엔 항상 보이고, 착지 후엔 시야 룰 적용.
/// TurnManager에 등록하지 않음 (턴 행동 없음).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GridDagger : MonoBehaviour
{
    [SerializeField] private float flightDuration = 0.25f;

    public Vector2Int GridPosition { get; private set; }
    public int Durability { get; private set; }
    public bool IsLanded { get; private set; }

    private SpriteRenderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        GameEvents.OnVisionUpdated += OnVisionUpdated;
    }

    private void OnDisable()
    {
        GameEvents.OnVisionUpdated -= OnVisionUpdated;
    }

    /// <summary>
    /// fromWorld 위치에서 toCell까지 lerp 비행 후 착지. onLand는 비행 종료 콜백.
    /// </summary>
    public void Throw(Vector3 fromWorld, Vector2Int toCell, int durability, Sprite sprite, System.Action onLand)
    {
        GridPosition = toCell;
        Durability = durability;
        IsLanded = false;

        if (sprite != null) _renderer.sprite = sprite;

        transform.position = fromWorld;
        _renderer.enabled = true;  // 비행 중엔 항상 표시

        GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.DaggerThrow);
        StartCoroutine(FlightCoroutine(fromWorld, toCell, onLand));
    }

    private IEnumerator FlightCoroutine(Vector3 fromWorld, Vector2Int toCell, System.Action onLand)
    {
        Vector3 toWorld = GridSystem.Instance.GridToWorld(toCell);
        float t = 0f;

        while (t < flightDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / flightDuration);
            transform.position = Vector3.Lerp(fromWorld, toWorld, a);
            yield return null;
        }

        transform.position = toWorld;
        IsLanded = true;
        RefreshVisibility();
        onLand?.Invoke();
    }

    private void OnVisionUpdated(System.Collections.Generic.IReadOnlyCollection<Vector2Int> visible,
                                  System.Collections.Generic.IReadOnlyCollection<Vector2Int> explored)
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (_renderer == null) return;
        if (!IsLanded) { _renderer.enabled = true; return; }
        _renderer.enabled = VisionSystem.Instance != null && VisionSystem.Instance.IsVisible(GridPosition);
    }
}
