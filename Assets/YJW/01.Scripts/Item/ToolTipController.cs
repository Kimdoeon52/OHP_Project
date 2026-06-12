using TMPro;
using UnityEngine;

public class ToolTipController : MonoBehaviour
{
    public static ToolTipController Instance { get; private set; }

    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Vector2 offset = new Vector2(12f, -12f);

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

        _panelRect.position = (Vector2)Input.mousePosition + offset;
    }
}
