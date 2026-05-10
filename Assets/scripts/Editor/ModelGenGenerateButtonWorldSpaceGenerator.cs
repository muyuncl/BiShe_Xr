using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// 仅生成“生成按钮面板”World Space UI。
/// 面板 137x187，按钮 109x109，图标 42x42。
/// </summary>
public static class ModelGenGenerateButtonWorldSpaceGenerator
{
    private const string CanvasName = "WorldSpaceGenerateButtonCanvas";
    private const float WorldScale = 0.0012f;

    [MenuItem("Tools/UI/Generate ModelGen Generate Button UI")]
    public static void Generate()
    {
        var canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Generate ModelGen Generate Button UI");

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        if (Camera.main != null)
            canvas.worldCamera = Camera.main;
        if (canvasGo.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(220f, 260f);
        canvasRect.localScale = Vector3.one * WorldScale;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        var panel = NewRect("GeneratePanel", canvasGo.transform);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(137f, 187f);
        panelRt.anchoredPosition = Vector2.zero;
        panel.gameObject.AddComponent<Image>().color = new Color(0.67f, 0.67f, 0.67f, 0.95f);

        var generateBtn = CreateGenerateButtonPanel(panel.transform);

        var tip = TmpCenter("GenerateTip", panel.transform, "请先上色\n再点击生成", 20);
        tip.fontStyle = FontStyles.Bold;
        tip.alignment = TextAlignmentOptions.Center;
        var tipRt = tip.GetComponent<RectTransform>();
        tipRt.anchorMin = new Vector2(0f, 0f);
        tipRt.anchorMax = new Vector2(1f, 0f);
        tipRt.pivot = new Vector2(0.5f, 0f);
        tipRt.anchoredPosition = new Vector2(0f, 8f);
        tipRt.sizeDelta = new Vector2(0f, 62f);

        var status = TmpCenter("StatusText", canvasGo.transform, "准备就绪", 16);
        var statusRt = status.GetComponent<RectTransform>();
        statusRt.anchorMin = statusRt.anchorMax = new Vector2(0.5f, 0f);
        statusRt.pivot = new Vector2(0.5f, 0f);
        statusRt.anchoredPosition = new Vector2(0f, 4f);
        statusRt.sizeDelta = new Vector2(180f, 24f);

        var controller = canvasGo.AddComponent<ModelGenUIController>();
        Set(controller, "generateButton", generateBtn.GetComponent<Button>());
        Set(controller, "statusText", status);
        Set(controller, "libraryButton", null);
        Set(controller, "materialContent", null);
        Set(controller, "materialCardTemplate", null);

        Selection.activeGameObject = canvasGo;
        Debug.Log("已生成独立的 Generate Button UI。请给 ModelGenUIController 绑定 VRPhotoTo3DController。");
    }

    private static GameObject CreateGenerateButtonPanel(Transform parent)
    {
        var go = NewRect("GenerateButton", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -14f);
        rt.sizeDelta = new Vector2(109f, 109f);

        var img = go.gameObject.AddComponent<Image>();
        img.color = new Color(0.2f, 0.22f, 0.24f, 0.98f);

        var btn = go.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        var iconWrap = NewRect("IconWrap", go.transform);
        var iconRt = iconWrap.GetComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.68f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(42f, 42f);

        var icon = TmpCenter("Icon", iconWrap.transform, "✦", 42);
        icon.fontStyle = FontStyles.Bold;
        Stretch(icon.GetComponent<RectTransform>(), 0, 0, 0, 0);

        var en = TmpCenter("EnLabel", go.transform, "G e n e r a t e", 9);
        var enRt = en.GetComponent<RectTransform>();
        enRt.anchorMin = enRt.anchorMax = new Vector2(0.5f, 0.38f);
        enRt.pivot = new Vector2(0.5f, 0.5f);
        enRt.sizeDelta = new Vector2(88f, 16f);

        var cn = TmpCenter("CnLabel", go.transform, "生 成", 24);
        cn.fontStyle = FontStyles.Bold;
        var cnRt = cn.GetComponent<RectTransform>();
        cnRt.anchorMin = cnRt.anchorMax = new Vector2(0.5f, 0.2f);
        cnRt.pivot = new Vector2(0.5f, 0.5f);
        cnRt.sizeDelta = new Vector2(88f, 28f);

        return go.gameObject;
    }

    private static TextMeshProUGUI TmpCenter(string name, Transform parent, string text, float size)
    {
        var go = NewRect(name, parent);
        var tmp = go.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform r, float left, float right, float top, float bottom)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(left, bottom);
        r.offsetMax = new Vector2(-right, -top);
    }

    private static void Set(object target, string field, object value)
    {
        if (target == null)
            return;
        var f = target.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (f == null)
            return;
        f.SetValue(target, value);
        if (target is Object o)
            EditorUtility.SetDirty(o);
    }
}
