using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolTipController : MonoBehaviour
{
    public static ToolTipController Instance { get; private set; }

    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Vector2 offset = new Vector2(12f, -12f);

    [Tooltip("화면 가장자리 여백")]
    [SerializeField] private float edgePadding = 8f;

    private RectTransform _panelRect;
    private RectTransform _canvasRect;

    private void Awake()
    {
        Instance = this;
        _panelRect = tooltipPanel.GetComponent<RectTransform>();
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();
        _canvasRect = rootCanvas.GetComponent<RectTransform>();
        tooltipPanel.SetActive(false);
    }

    public void Show(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        tooltipText.text = text;
        tooltipPanel.SetActive(true);

        // 크기 갱신 후 배치 — 첫 프레임에 잘리거나 튀는 것 방지
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRect);
        PlaceAtMouse();
    }

    public void Hide()
    {
        tooltipPanel.SetActive(false);
    }

    // 마우스를 매 프레임 따라다님
    private void Update()
    {
        if (!tooltipPanel.activeSelf)
            return;

        PlaceAtMouse();
    }

    // 커서 옆에 붙이되 화면 밖으로 나가면 반대편으로 뒤집고 가장자리로 클램프
    private void PlaceAtMouse()
    {
        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, Input.mousePosition, cam, out Vector2 mouseLocal))
            return;

        Vector2 canvasSize = _canvasRect.rect.size;
        Vector2 canvasPivot = _canvasRect.pivot;
        float minX = -canvasSize.x * canvasPivot.x + edgePadding;
        float maxX = canvasSize.x * (1f - canvasPivot.x) - edgePadding;
        float minY = -canvasSize.y * canvasPivot.y + edgePadding;
        float maxY = canvasSize.y * (1f - canvasPivot.y) - edgePadding;

        Vector2 panelSize = PanelSizeInCanvas();
        Vector2 offsetLocal = rootCanvas.scaleFactor > 0f ? offset / rootCanvas.scaleFactor : offset;

        // corner = 패널 왼쪽 위 모서리
        Vector2 corner = mouseLocal + offsetLocal;

        if (corner.x + panelSize.x > maxX)
            corner.x = mouseLocal.x - offsetLocal.x - panelSize.x;

        if (corner.y - panelSize.y < minY)
            corner.y = mouseLocal.y - offsetLocal.y + panelSize.y;

        corner.x = Mathf.Clamp(corner.x, minX, Mathf.Max(minX, maxX - panelSize.x));
        corner.y = Mathf.Clamp(corner.y, Mathf.Min(maxY, minY + panelSize.y), maxY);

        Vector2 local = new Vector2(corner.x + _panelRect.pivot.x * panelSize.x, corner.y - (1f - _panelRect.pivot.y) * panelSize.y);
        _panelRect.position = _canvasRect.TransformPoint(local);
    }

    // 패널 크기를 캔버스 로컬 단위로 환산
    private Vector2 PanelSizeInCanvas()
    {
        Vector3 panelScale = _panelRect.lossyScale;
        Vector3 canvasScale = _canvasRect.lossyScale;
        float sx = Mathf.Approximately(canvasScale.x, 0f) ? 1f : panelScale.x / canvasScale.x;
        float sy = Mathf.Approximately(canvasScale.y, 0f) ? 1f : panelScale.y / canvasScale.y;
        return new Vector2(_panelRect.rect.width * sx, _panelRect.rect.height * sy);
    }
}
