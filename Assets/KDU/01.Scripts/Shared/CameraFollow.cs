using System.Collections.Generic;
using UnityEngine;
using PrototypeRT;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField, Range(0f, 1f)] private float smoothing = 0.12f;

    [Header("Safe Room 플래닝 모드")]
    [SerializeField] private Camera planningCamera;
    [SerializeField] private GridPlayer player;
    [SerializeField] private float planningZoomSpeed = 1.5f;
    [SerializeField] private float planningMinSize = 3f;
    [SerializeField] private float planningMaxSize = 15f;

    [SerializeField] private bool useMouseEdgePan = false;
    [SerializeField] private float planningEdgePixels = 40f;
    [SerializeField] private float planningPanSpeed = 8f;

    private Vector3 _velocity;
    private bool _planningMode;
    private bool _deathFocusMode;
    private Transform _deathFocusTarget;
    private float _deathFocusStartSize;
    private float _deathFocusTargetSize;
    private float _deathFocusDuration;
    private float _deathFocusElapsed;
    private HashSet<Vector2Int> _safeCells;
    private Vector2 _planningPan;
    private float _defaultSize;
    private bool _dragging;
    private Vector3 _lastMouseScreen;

    // 현재 플래닝 모드 여부 (튜토리얼용)
    public bool IsPlanningMode => _planningMode;

    private bool _hasPanned;
    // ArmSafeRoom 이후 플레이어가 줌/팬을 한 적이 있는가 (튜토리얼용)
    public bool HasPannedSinceArm => _hasPanned;

    public static CameraFollow Instance { get; private set; }

    // 팬 클램프용 명시적 맵 경계 (튜토리얼용)
    private bool _hasExplicitBounds;
    private Bounds _explicitBounds;

    // MapGenerator 없는 씬에서 팬 제한 경계를 직접 지정 (튜토리얼용)
    public void SetMapBounds(Bounds bounds)
    {
        _explicitBounds = bounds;
        _hasExplicitBounds = true;
    }

    private Vector3 _shakeOffset = Vector3.zero;
    private Coroutine _shakeCoroutine;


    // 맵 밖이 안 보이는 최대 줌
    private float MaxSizeForMap()
    {
        if (planningCamera == null || !TryGetMapBounds(out Bounds b))
            return planningMaxSize;
        float byH = b.size.y * 0.5f;
        float byW = b.size.x * 0.5f / planningCamera.aspect;
        return Mathf.Min(planningMaxSize, byH, byW);
    }


    private void Awake()
    {
        Instance = this;
        if (planningCamera == null) planningCamera = GetComponent<Camera>();
        if (planningCamera != null) _defaultSize = planningCamera.orthographicSize;
        if (player == null) player = FindFirstObjectByType<GridPlayer>();
    }

    public void Shake(float duration, float strength)
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
        }
        _shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, strength));
    }

    public void BeginDeathFocus(Transform focusTarget, float targetSize, float duration)
    {
        if (focusTarget == null)
            return;

        _deathFocusTarget = focusTarget;
        _deathFocusTargetSize = Mathf.Max(0.1f, targetSize);
        _deathFocusDuration = Mathf.Max(0.01f, duration);
        _deathFocusElapsed = 0f;
        _deathFocusMode = true;
        DisablePlanning();

        if (planningCamera == null)
            planningCamera = GetComponent<Camera>();

        _deathFocusStartSize = planningCamera != null ? planningCamera.orthographicSize : _deathFocusTargetSize;
    }

    public void EndDeathFocus(bool restoreDefaultZoom)
    {
        _deathFocusMode = false;
        _deathFocusTarget = null;
        _deathFocusElapsed = 0f;

        if (restoreDefaultZoom && planningCamera != null)
            planningCamera.orthographicSize = _defaultSize;
    }

    private System.Collections.IEnumerator ShakeCoroutine(float duration, float strength)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float percentComplete = elapsed / duration;
            float damper = 1.0f - percentComplete;

            float x = Random.Range(-1f, 1f) * strength * damper;
            if(x == 0f)
                x = 0.05f * strength * damper;
            _shakeOffset = new Vector3(x, 0f, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }
        _shakeOffset = Vector3.zero;
        _shakeCoroutine = null;
    }

    public void ArmSafeRoom(IReadOnlyCollection<Vector2Int> safeCells)
    {
        if (safeCells == null || safeCells.Count == 0)
        {
            DisablePlanning();
            return;
        }
        if (player == null) player = FindFirstObjectByType<GridPlayer>();
        if (player == null)
        {
            Debug.LogWarning("[CameraFollow] player 미할당 — 플래닝 모드 이탈 감지 불가, 모드 활성화 안 함");
            DisablePlanning();
            return;
        }
        _safeCells = new HashSet<Vector2Int>(safeCells);
        _planningPan = Vector2.zero;
        _hasPanned = false;
        if (planningCamera != null) planningCamera.orthographicSize = _defaultSize;
        _planningMode = true;
    }

    private void DisablePlanning()
    {
        _planningMode = false;
        _safeCells = null;
        _planningPan = Vector2.zero;
        _dragging = false;
        if (planningCamera != null) planningCamera.orthographicSize = _defaultSize;
    }

    private void Update()
    {
        if (!_planningMode) return;

        if (KeyBindingManager.ShouldBlockGameplayInput)
        {
            // UI 창 위에서 드래그를 시작한 상태가 남으면 창을 닫은 뒤 카메라가 튀듯이 움직일 수 있습니다.
            // 게임 입력 차단 중에는 카메라 입력도 끊고 드래그 상태를 초기화합니다.
            _dragging = false;
            return;
        }

        // 플레이어 세이프룸 밖으로 나가면 플래닝 모드 종료
        if (player != null && _safeCells != null && !_safeCells.Contains(player.GridPosition))
        {
            DisablePlanning();
            return;
        }

        // 스크롤 줌
        if (planningCamera != null)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0f)
            {
                float ns = planningCamera.orthographicSize - scroll * planningZoomSpeed;
                planningCamera.orthographicSize = Mathf.Clamp(ns, planningMinSize, MaxSizeForMap());
                _hasPanned = true;
            }
        }

        if (useMouseEdgePan) UpdateEdgePan();
        else UpdateDragPan();

        ClampPanToMapBounds();
    }


    private void ClampPanToMapBounds()
    {
        if (planningCamera == null || target == null) return;
        if (!TryGetMapBounds(out Bounds b)) return;

        float halfH = planningCamera.orthographicSize;
        float halfW = halfH * planningCamera.aspect;

        Vector3 basePos = target.position + offset;

        float allowedXMin = b.min.x + halfW - basePos.x;
        float allowedXMax = b.max.x - halfW - basePos.x;
        _planningPan.x = (allowedXMax < allowedXMin)
            ? b.center.x - basePos.x
            : Mathf.Clamp(_planningPan.x, allowedXMin, allowedXMax);

        float allowedYMin = b.min.y + halfH - basePos.y;
        float allowedYMax = b.max.y - halfH - basePos.y;
        _planningPan.y = (allowedYMax < allowedYMin)
            ? b.center.y - basePos.y
            : Mathf.Clamp(_planningPan.y, allowedYMin, allowedYMax);
    }

    // 팬 제한 경계
    private bool TryGetMapBounds(out Bounds b)
    {
        if (_hasExplicitBounds) { b = _explicitBounds; return true; }
        if (MapGenerator.Instance != null && MapGenerator.Instance.MapWorldBoundsValid)
        {
            b = MapGenerator.Instance.MapWorldBounds;
            return true;
        }
        b = default;
        return false;
    }

    // 카메라 중심 설정
    private Vector3 ClampCameraPos(Vector3 pos)
    {
        if (planningCamera == null || !TryGetMapBounds(out Bounds b)) return pos;

        float halfH = planningCamera.orthographicSize;
        float halfW = halfH * planningCamera.aspect;

        float xMin = b.min.x + halfW, xMax = b.max.x - halfW;
        pos.x = (xMax < xMin) ? b.center.x : Mathf.Clamp(pos.x, xMin, xMax);

        float yMin = b.min.y + halfH, yMax = b.max.y - halfH;
        pos.y = (yMax < yMin) ? b.center.y : Mathf.Clamp(pos.y, yMin, yMax);
        return pos;
    }

    private void UpdateEdgePan()
    {
        Vector3 m = Input.mousePosition;
        Vector2 panDir = Vector2.zero;
        if (m.x < planningEdgePixels) panDir.x = -1f;
        else if (m.x > Screen.width - planningEdgePixels) panDir.x = 1f;
        if (m.y < planningEdgePixels) panDir.y = -1f;
        else if (m.y > Screen.height - planningEdgePixels) panDir.y = 1f;
        if (panDir != Vector2.zero)
        {
            _planningPan += panDir * (planningPanSpeed * Time.deltaTime);
            _hasPanned = true;
        }
    }

    private void UpdateDragPan()
    {
        // 버튼이 실제 눌려있을 때만 팬 — up 이벤트를 놓쳐도 따라다니지 않게 매 프레임 게이트.
        if (!Input.GetMouseButton(0))
        {
            _dragging = false;
            return;
        }

        Vector3 cur = Input.mousePosition;

        // 누른 첫 프레임은 기준점만 잡고 이동 없음.
        if (!_dragging)
        {
            _dragging = true;
            _lastMouseScreen = cur;
            return;
        }

        Vector3 delta = cur - _lastMouseScreen;
        _lastMouseScreen = cur;

        // 화면 픽셀 -> 월드 환산
        if (planningCamera == null || Screen.height <= 0) return;
        float worldPerPixel = 2f * planningCamera.orthographicSize / Screen.height;
        _planningPan -= new Vector2(delta.x, delta.y) * worldPerPixel;
        if (delta.sqrMagnitude > 0f) _hasPanned = true;
    }

    private void LateUpdate()
    {
        if (_deathFocusMode && _deathFocusTarget != null)
        {
            UpdateDeathFocus();
            return;
        }

        if (target == null) return;

        Vector3 desired = target.position + offset + new Vector3(_planningPan.x, _planningPan.y, 0f) + _shakeOffset;

        if (smoothing <= 0f)
            transform.position = desired;
        else
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothing);

        // 줌 뺄때 카메라가 맵 밖으로 나가서 넣음
        if (_planningMode)
        {
            Vector3 p = transform.position;
            float z = p.z;
            p = ClampCameraPos(p);
            p.z = z;
            transform.position = p;
        }
    }

    // 타깃 위치 변동시 즉시 따라감
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = target.position + offset + new Vector3(_planningPan.x, _planningPan.y, 0f) + _shakeOffset;
        _velocity = Vector3.zero;
    }

    private void UpdateDeathFocus()
    {
        Vector3 desired = _deathFocusTarget.position + offset + _shakeOffset;

        if (smoothing <= 0f)
            transform.position = desired;
        else
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothing);

        if (planningCamera == null)
            return;

        _deathFocusElapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_deathFocusElapsed / _deathFocusDuration);
        planningCamera.orthographicSize = Mathf.Lerp(_deathFocusStartSize, _deathFocusTargetSize, t);
    }
}
