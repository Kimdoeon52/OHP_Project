using UnityEngine;
using UnityEngine.UI;

// [개발용 디버그] 현재 해상도/비율/캔버스 스케일을 화면 좌상단에 표시한다.
// Game 뷰에서 해상도를 바꾸면 값이 실시간으로 갱신돼 레이아웃 확인에 쓴다.
// 출시 빌드에는 이 컴포넌트를 끄거나 삭제할 것.
[ExecuteAlways]
public class ScreenDebugOverlay : MonoBehaviour
{
    [Tooltip("스케일을 읽을 캔버스 스케일러. 비우면 씬에서 자동으로 찾는다.")]
    [SerializeField] private CanvasScaler canvasScaler;

    [Tooltip("글자 크기")]
    [SerializeField] private int fontSize = 22;

    private void OnGUI()
    {
        float w = Screen.width;
        float h = Screen.height;
        float aspect = h > 0f ? w / h : 0f;

        if (canvasScaler == null)
            canvasScaler = FindFirstObjectByType<CanvasScaler>();

        Canvas canvas = canvasScaler != null ? canvasScaler.GetComponent<Canvas>() : null;
        float scale = canvas != null ? canvas.scaleFactor : 1f;

        GUIStyle style = new GUIStyle
        {
            fontSize = fontSize,
            normal = { textColor = Color.yellow }
        };

        string text =
            $"Res: {w:0} x {h:0}\n" +
            $"Aspect: {aspect:0.000}  (16:9 = 1.778)\n" +
            $"Canvas scale: {scale:0.000}";

        // 그림자 한 겹 깔아 밝은 배경에서도 읽히게 한다.
        GUIStyle shadow = new GUIStyle(style) { normal = { textColor = Color.black } };
        GUI.Label(new Rect(11f, 11f, 500f, 120f), text, shadow);
        GUI.Label(new Rect(10f, 10f, 500f, 120f), text, style);
    }
}
