using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [Tooltip("0 = 즉시 스냅, 0.1~0.3 = 부드러운 추적")]
    [SerializeField, Range(0f, 1f)] private float smoothing = 0.12f;

    [Header("Safe Room 플래닝 모드 (스크롤 줌 + 좌클릭 드래그 팬, 첫 이탈 시 자동 해제)")]
    [Tooltip("줌을 적용할 카메라. 비우면 GetComponent<Camera>().")]
    [SerializeField] private Camera planningCamera;
    [Tooltip("플레이어 — 이 셀이 safe 집합 밖으로 나가면 플래닝 모드 해제.")]
    [SerializeField] private GridPlayer player;
    [SerializeField] private float planningZoomSpeed = 1.5f;
    [SerializeField] private float planningMinSize = 3f;
    [SerializeField] private float planningMaxSize = 15f;

    [Tooltip("ON: 마우스가 화면 가장자리에 있을 때 팬. OFF(기본): 좌클릭 드래그로 팬.")]
    [SerializeField] private bool useMouseEdgePan = false;
    [Tooltip("엣지 모드 — 마우스가 화면 끝에서 이 픽셀 이내일 때 팬.")]
    [SerializeField] private float planningEdgePixels = 40f;
    [Tooltip("엣지 모드 — 초당 팬 속도(월드 유닛).")]
    [SerializeField] private float planningPanSpeed = 8f;

    private Vector3 _velocity;
    private bool _planningMode;
    private HashSet<Vector2Int> _safeCells;
    private Vector2 _planningPan;
    private float _defaultSize;
    private bool _dragging;
    private Vector3 _lastMouseScreen;

    /// <summary>현재 플래닝 모드 여부 — GridPlayerInput 등 외부에서 클릭 무시 판단에 사용.</summary>
    public bool IsPlanningMode => _planningMode;

    private bool _hasPanned;
    /// <summary>ArmSafeRoom 이후 플레이어가 줌/팬을 한 적이 있는가 — 튜토리얼 "맵 둘러보기" 판정용.</summary>
    public bool HasPannedSinceArm => _hasPanned;

    public static CameraFollow Instance { get; private set; }

    // 팬 클램프용 명시적 맵 경계 — MapGenerator 가 없는 씬(튜토리얼 등)에서 주입.
    private bool _hasExplicitBounds;
    private Bounds _explicitBounds;

    /// <summary>MapGenerator 없는 씬에서 팬 제한 경계를 직접 지정(튜토리얼). MapGenerator 보다 우선.</summary>
    public void SetMapBounds(Bounds bounds)
    {
        _explicitBounds = bounds;
        _hasExplicitBounds = true;
    }

    private Vector3 _shakeOffset = Vector3.zero;
    private Coroutine _shakeCoroutine;

    private void Awake()
    {
        Instance = this;
        if (planningCamera == null) planningCamera = GetComponent<Camera>();
        if (planningCamera != null) _defaultSize = planningCamera.orthographicSize;
        if (player == null) player = FindFirstObjectByType<GridPlayer>();
    }

    /// <summary>카메라를 지정된 시간과 세기로 흔듭니다.</summary>
    public void Shake(float duration, float strength)
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
        }
        _shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, strength));
    }

    private System.Collections.IEnumerator ShakeCoroutine(float duration, float strength)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float percentComplete = elapsed / duration;
            float damper = 1.0f - percentComplete; // 시간이 지날수록 감쇠

            float x = Random.Range(-1f, 1f) * strength * damper;
            if(x == 0f)
                x = 0.05f * strength * damper; // 완전 0 방지
            _shakeOffset = new Vector3(x, 0f, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }
        _shakeOffset = Vector3.zero;
        _shakeCoroutine = null;
    }

    /// <summary>층 진입 시 호출 — safe 셀을 등록하면 플래닝 모드 ON, 플레이어가 처음 이 셀들을 벗어나면 자동 OFF.</summary>
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

        // 플레이어 첫 이탈 감지 — 그 즉시 줌/팬 리셋.
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
                planningCamera.orthographicSize = Mathf.Clamp(ns, planningMinSize, planningMaxSize);
                _hasPanned = true;
            }
        }

        if (useMouseEdgePan) UpdateEdgePan();
        else UpdateDragPan();

        ClampPanToMapBounds();
    }

    /// <summary>
    /// _planningPan 을 클램프해 카메라 뷰포트가 맵 월드 바운드 밖으로 나가지 않게 함.
    /// 맵이 뷰포트보다 작으면 맵 중앙에 고정.
    /// </summary>
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

    /// <summary>팬 제한 경계 — 명시적(튜토리얼) 우선, 없으면 MapGenerator. 둘 다 없으면 false.</summary>
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

    /// <summary>카메라 중심 위치를 뷰포트가 맵 밖으로 나가지 않게 클램프(맵이 뷰포트보다 작으면 중앙).</summary>
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
        if (Input.GetMouseButtonDown(0))
        {
            _dragging = true;
            _lastMouseScreen = Input.mousePosition;
            return;
        }
        if (Input.GetMouseButtonUp(0)) _dragging = false;
        if (!_dragging) return;

        Vector3 cur = Input.mousePosition;
        Vector3 delta = cur - _lastMouseScreen;
        _lastMouseScreen = cur;

        // 화면 픽셀 → 월드 유닛 환산 (orthographic 카메라).
        // worldPerPixel = (2 * orthoSize) / Screen.height — X/Y 동일 (aspect 캔슬).
        // 드래그 방향과 반대로 팬을 누적해야 "월드를 손으로 잡고 끄는" 느낌.
        if (planningCamera == null || Screen.height <= 0) return;
        float worldPerPixel = 2f * planningCamera.orthographicSize / Screen.height;
        _planningPan -= new Vector2(delta.x, delta.y) * worldPerPixel;
        if (delta.sqrMagnitude > 0f) _hasPanned = true;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset + new Vector3(_planningPan.x, _planningPan.y, 0f) + _shakeOffset;

        if (smoothing <= 0f)
            transform.position = desired;
        else
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothing);

        // 플래닝 중엔 렌더되는 최종 위치를 경계로 하드 클램프 — 줌을 확 뺄 때 SmoothDamp 지연으로
        // 맵 밖이 잠깐 보이는 것 방지.
        if (_planningMode)
        {
            Vector3 p = transform.position;
            float z = p.z;
            p = ClampCameraPos(p);
            p.z = z;
            transform.position = p;
        }
    }

    /// <summary>층 전환 등 타깃이 순간이동할 때 호출 — smoothdamp 누적 속도를 리셋하고 위치를 즉시 스냅.</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = target.position + offset + new Vector3(_planningPan.x, _planningPan.y, 0f) + _shakeOffset;
        _velocity = Vector3.zero;
    }
}
