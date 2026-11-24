using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 인벤토리 스탯 판 재배치 — 전투 스탯 6칸 숨김, 체력·경험치 바, 레벨·힘·허기·골드 2열. 다시 실행하면 배치만 재적용
public static class StatsPanelRelayout
{
    private const string StatsPath = "InventoryWindow_RT/MyInventoryGround/InventoryUI/PlayerStats";
    private const string HudHpPath = "UICanvas/PlayerHpBar";
    private const string HudExpPath = "UICanvas/PlayerEXPBar";

    // 숨길 칸
    private static readonly string[] HiddenCells = { "ATK_BG", "AS_BG", "DF_BG", "Accuracy_BG", "MS_BG", "DG_BG" };

    // 배치 범위
    private const float Left = -329.4f;
    private const float Right = 333.4f;
    private const float Top = 99f;
    private const float Bottom = -100f;
    private const float Gap = 10f;
    private const float ColumnGap = 14.2f;
    private const float SmallHeight = 33.291f;

    // 바 칸 안 비율 (라벨 | 바+숫자)
    private const float LabelEnd = 0.175f;
    private const float BarStart = 0.18f;
    private const float BarEnd = 0.985f;

    // 라벨 왼쪽 여백, 바 좌우 안쪽 여백 (px)
    private const float LabelPadding = 14f;
    private const float BarInset = 8f;

    // 바 세로 범위 — HUD 테두리가 칸 안에 들어오는 높이
    private const float BarBottom = 0.17f;
    private const float BarTop = 0.83f;

    [MenuItem("Tools/KDU/인벤토리 스탯 판 재배치")]
    private static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform stats = FindInRoots(scene, "InventoryPanel", StatsPath);
        Transform hudHp = FindInRoots(scene, "UIManager", HudHpPath);
        Transform hudExp = FindInRoots(scene, "UIManager", HudExpPath);

        if (stats == null || hudHp == null || hudExp == null)
        {
            Debug.LogError("[StatsPanelRelayout] NYH_Grid 씬을 열고 실행 (PlayerStats / HUD 바 못 찾음)");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(stats.gameObject, "스탯 판 재배치");

        foreach (string cell in HiddenCells)
        {
            Transform t = stats.Find(cell);
            if (t != null)
                t.gameObject.SetActive(false);
        }

        // 세로: 바 2행이 작은 2행·간격을 뺀 나머지 공간을 나눠 가짐
        float barHeight = ((Top - Bottom) - SmallHeight * 2f - Gap * 3f) / 2f;
        float hpY = Top - barHeight / 2f;
        float expY = hpY - barHeight - Gap;
        float lvY = expY - barHeight / 2f - Gap - SmallHeight / 2f;
        float foodY = lvY - SmallHeight - Gap;

        float fullWidth = Right - Left;
        float halfWidth = (fullWidth - ColumnGap) / 2f;
        float center = (Left + Right) / 2f;
        float leftX = Left + halfWidth / 2f;
        float rightX = Right - halfWidth / 2f;

        Place(stats, "HP_BG", center, hpY, fullWidth, barHeight);
        Place(stats, "EXP_BG", center, expY, fullWidth, barHeight);
        Place(stats, "LV_BG", leftX, lvY, halfWidth, SmallHeight);
        Place(stats, "STR_BG", rightX, lvY, halfWidth, SmallHeight);
        Place(stats, "Food_BG", leftX, foodY, halfWidth, SmallHeight);
        Place(stats, "Gold", rightX, foodY, halfWidth, SmallHeight);

        Transform goldPoint = stats.Find("Gold/GoldPoint");
        if (goldPoint != null)
            Stretch((RectTransform)goldPoint, 0f, 1f, 0f, 1f);

        CopyCellLayout(stats, "LV_BG/LV", "Food_BG/Food");
        CopyCellLayout(stats, "LV_BG/LVValue", "Food_BG/FoodValue");

        TMP_Text labelStyle = FindText(stats, "LV_BG/LV");
        TMP_Text valueStyle = FindText(stats, "LV_BG/LVValue");
        Image hpFill = BuildBarCell(stats.Find("HP_BG"), "HP", "HPValue", hudHp, "HPBar", "CurrentHp", labelStyle, valueStyle);
        Image expFill = BuildBarCell(stats.Find("EXP_BG"), "EXP", "EXPValue", hudExp, "EXPBar", "CurrentExp", labelStyle, valueStyle);

        var barUi = stats.GetComponent<PlayerHealthBarUI>();
        if (barUi == null)
            barUi = Undo.AddComponent<PlayerHealthBarUI>(stats.gameObject);

        var so = new SerializedObject(barUi);
        so.FindProperty("fillImage").objectReferenceValue = hpFill;
        so.FindProperty("expFillImage").objectReferenceValue = expFill;
        so.FindProperty("healthText").objectReferenceValue = FindText(stats, "HP_BG/HPValue");
        so.FindProperty("expText").objectReferenceValue = FindText(stats, "EXP_BG/EXPValue");
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[StatsPanelRelayout] 완료 — 확인 후 씬 저장 (되돌리기: Ctrl+Z)");
    }

    // 칸 위치·크기 설정
    private static void Place(Transform stats, string name, float x, float y, float width, float height)
    {
        var rt = stats.Find(name) as RectTransform;
        if (rt == null)
            return;

        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, height);
    }

    // 기준 칸의 라벨/숫자 위치·글자 설정을 대상에 복사 (색은 유지)
    private static void CopyCellLayout(Transform stats, string fromPath, string toPath)
    {
        var from = stats.Find(fromPath) as RectTransform;
        var to = stats.Find(toPath) as RectTransform;
        if (from == null || to == null)
            return;

        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.pivot = from.pivot;
        to.anchoredPosition = from.anchoredPosition;
        to.sizeDelta = from.sizeDelta;

        var fromText = from.GetComponent<TMP_Text>();
        var toText = to.GetComponent<TMP_Text>();
        if (fromText == null || toText == null)
            return;

        CopyTextStyle(fromText, toText);
        toText.alignment = fromText.alignment;
    }

    // 글자 크기 설정 복사 (색·정렬 제외)
    private static void CopyTextStyle(TMP_Text from, TMP_Text to)
    {
        if (from == null || to == null)
            return;

        Undo.RecordObject(to, "스탯 글자 정렬");
        to.enableAutoSizing = from.enableAutoSizing;
        to.fontSizeMin = from.fontSizeMin;
        to.fontSizeMax = from.fontSizeMax;
        to.fontSize = from.fontSize;
        to.margin = from.margin;
    }

    // 라벨 | 바(숫자 겹침) 배치, 바 없으면 HUD 바 그림 복제. 채움 Image 반환
    private static Image BuildBarCell(Transform cell, string labelName, string valueName, Transform hudBar, string barName, string fillName, TMP_Text labelStyle, TMP_Text valueStyle)
    {
        if (cell == null)
            return null;

        if (cell.Find(labelName) is RectTransform label)
        {
            Stretch(label, 0f, LabelEnd, 0f, 1f);
            label.offsetMin = new Vector2(LabelPadding, 0f);
            CopyTextStyle(labelStyle, label.GetComponent<TMP_Text>());
        }

        var barRt = cell.Find(barName) as RectTransform;
        if (barRt == null)
            barRt = CreateBar(cell, barName, hudBar);

        Stretch(barRt, BarStart, BarEnd, BarBottom, BarTop);
        barRt.offsetMin = new Vector2(BarInset, 0f);
        barRt.offsetMax = new Vector2(-BarInset, 0f);

        // 숫자는 바 위에 겹쳐 가운데, 흰색
        if (cell.Find(valueName) is RectTransform value)
        {
            Stretch(value, BarStart, BarEnd, 0f, 1f);
            value.offsetMin = new Vector2(BarInset, 0f);
            value.offsetMax = new Vector2(-BarInset, 0f);
            value.SetAsLastSibling();

            var text = value.GetComponent<TMP_Text>();
            CopyTextStyle(valueStyle, text);
            if (text != null)
            {
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
            }
        }

        Transform fill = barRt.Find(fillName);
        return fill != null ? fill.GetComponent<Image>() : null;
    }

    private static RectTransform CreateBar(Transform cell, string barName, Transform hudBar)
    {
        var bar = new GameObject(barName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(bar, "스탯 바 생성");
        var barRt = (RectTransform)bar.transform;
        barRt.SetParent(cell, false);

        Vector2 srcSize = ((RectTransform)hudBar).rect.size;
        foreach (Transform child in hudBar)
        {
            Transform copy = Object.Instantiate(child, barRt, false);
            copy.name = child.name;
            ToProportional((RectTransform)child, (RectTransform)copy, srcSize);
        }

        return barRt;
    }

    // 고정 크기 자식을 부모 대비 비율 앵커로 변환 (바 크기가 바뀌어도 모양 유지)
    private static void ToProportional(RectTransform src, RectTransform dst, Vector2 parentSize)
    {
        if (src.anchorMin != src.anchorMax || parentSize.x <= 0f || parentSize.y <= 0f)
            return;

        Vector2 c = src.anchorMin + new Vector2(src.anchoredPosition.x / parentSize.x, src.anchoredPosition.y / parentSize.y);
        Vector2 half = new Vector2(src.sizeDelta.x / parentSize.x, src.sizeDelta.y / parentSize.y) / 2f;
        dst.anchorMin = c - half;
        dst.anchorMax = c + half;
        dst.offsetMin = Vector2.zero;
        dst.offsetMax = Vector2.zero;
    }

    private static void Stretch(RectTransform rt, float xMin, float xMax, float yMin, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static TMP_Text FindText(Transform root, string path)
    {
        Transform t = root.Find(path);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    // 비활성 루트도 찾기 위해 씬 루트에서 직접 탐색
    private static Transform FindInRoots(Scene scene, string rootName, string path)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName)
                continue;

            Transform t = root.transform.Find(path);
            if (t != null)
                return t;
        }

        return null;
    }
}
