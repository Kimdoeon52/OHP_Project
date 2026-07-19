using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 힘 부족 등 플레이어 행동 실패를 화면 중앙에 잠깐 알려주는 텍스트.
// 씬에 직접 배치해도 되고, 없으면 런타임에 자동 생성된다. 씬 전환에도 살아남아야
// 어느 씬에서 실패 이벤트가 와도 받을 수 있으므로 PersistentSingleton으로 관리한다.
public class PlayerNoticeView : PersistentSingleton<PlayerNoticeView>
{
    [Tooltip("표시할 TMP 텍스트. 비워두면 런타임에 화면 중앙 Canvas와 함께 생성한다.")]
    [SerializeField] private TextMeshProUGUI noticeText;

    [Tooltip("텍스트 색상")]
    [SerializeField] private Color noticeColor = Color.red;

    [Tooltip("텍스트 크기")]
    [SerializeField] private float fontSize = 48f;

    [Tooltip("표시 유지 시간(초)")]
    [SerializeField] private float displayDuration = 1f;

    // 인벤토리 등 다른 UI Canvas보다 항상 위에 그려지도록 매우 높은 값을 씁니다.
    private const int NoticeSortingOrder = 30000;

    private Coroutine _hideRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject root = new GameObject("PlayerNoticeView");
        root.AddComponent<PlayerNoticeView>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        if (noticeText == null)
            noticeText = CreateRuntimeText();

        noticeText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerNoticeRequested += Show;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerNoticeRequested -= Show;
    }

    private void Show(string message)
    {
        if (noticeText == null || string.IsNullOrEmpty(message))
            return;

        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);

        noticeText.text = message;
        noticeText.gameObject.SetActive(true);
        _hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        noticeText.gameObject.SetActive(false);
        _hideRoutine = null;
    }

    private TextMeshProUGUI CreateRuntimeText()
    {
        GameObject canvasObj = new GameObject("PlayerNoticeCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = NoticeSortingOrder;
        canvasObj.AddComponent<CanvasScaler>();

        GameObject textObj = new GameObject("PlayerNoticeText");
        textObj.transform.SetParent(canvasObj.transform, false);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = noticeColor;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(800f, 100f);

        return text;
    }
}
