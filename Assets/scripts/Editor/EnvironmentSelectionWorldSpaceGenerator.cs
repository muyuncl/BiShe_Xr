using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// 一键生成“场景选择”World Space UI：342x418 面板、可滚动环境卡片、以及 70x21 的 VR/MR 切换条。
/// 需要你在场景里把 VR/MR 按钮绑定到 <see cref="PassthroughVRMRFader"/>（或用 <see cref="XRPassthroughToggleController"/>）。
/// </summary>
public static class EnvironmentSelectionWorldSpaceGenerator
{
    private const string CanvasName = "WorldSpaceEnvironmentSelectionCanvas";
    private const float WorldScale = 0.0012f;

    private const float PanelW = 342f;
    private const float PanelH = 418f;
    private const float CardSize = 123f;
    private const float PreviewSize = 73f;
    private const float ToggleW = 70f;
    private const float ToggleH = 21f;

    [MenuItem("Tools/UI/Generate Environment Selection UI")]
    public static void Generate()
    {
        var existing = GameObject.Find(CanvasName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Generate Environment Selection UI");

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        if (Camera.main != null)
            canvas.worldCamera = Camera.main;
        if (canvasGo.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(520f, 520f);
        canvasRect.localScale = Vector3.one * WorldScale;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        // Panel root
        var panel = NewRect("SceneSelectionPanel", canvasGo.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(PanelW, PanelH);
        panel.anchoredPosition = Vector2.zero;
        var panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.13f, 0.88f);

        // Title
        var title = Tmp("Title", panel, "场 景 选 择", 28f, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.Center;
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -18f);
        titleRt.sizeDelta = new Vector2(PanelW - 40f, 36f);

        var subtitle = Tmp("Subtitle", panel, "Scene Selection", 10f, FontStyles.Normal);
        subtitle.color = new Color(1f, 1f, 1f, 0.7f);
        subtitle.alignment = TextAlignmentOptions.Center;
        var subRt = subtitle.rectTransform;
        subRt.anchorMin = new Vector2(0.5f, 1f);
        subRt.anchorMax = new Vector2(0.5f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -44f);
        subRt.sizeDelta = new Vector2(PanelW - 40f, 18f);

        // Scroll (2 columns grid)
        var scroll = NewRect("EnvScroll", panel);
        scroll.anchorMin = new Vector2(0f, 0f);
        scroll.anchorMax = new Vector2(1f, 1f);
        scroll.pivot = new Vector2(0.5f, 0.5f);
        scroll.offsetMin = new Vector2(20f, 70f);
        scroll.offsetMax = new Vector2(-20f, -70f);
        var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var viewport = NewRect("Viewport", scroll);
        Stretch(viewport, 0, 0, 0, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        var content = NewRect("Content", viewport);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 400f);

        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(CardSize, CardSize);
        grid.spacing = new Vector2(12f, 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.padding = new RectOffset(0, 0, 0, 0);

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = contentRt;

        // Toggle strip (VR/MR)
        var toggleStrip = NewRect("VRMR_ToggleStrip", panel);
        toggleStrip.anchorMin = new Vector2(0.5f, 0f);
        toggleStrip.anchorMax = new Vector2(0.5f, 0f);
        toggleStrip.pivot = new Vector2(0.5f, 0f);
        toggleStrip.anchoredPosition = new Vector2(0f, 18f);
        toggleStrip.sizeDelta = new Vector2(ToggleW, ToggleH);
        var stripImg = toggleStrip.gameObject.AddComponent<Image>();
        stripImg.color = new Color(1f, 1f, 1f, 0.12f);

        var vrLabel = Tmp("VRLabel", toggleStrip, "VR", 12f, FontStyles.Bold);
        vrLabel.alignment = TextAlignmentOptions.Left;
        var vrRt = vrLabel.rectTransform;
        vrRt.anchorMin = new Vector2(0f, 0.5f);
        vrRt.anchorMax = new Vector2(0f, 0.5f);
        vrRt.pivot = new Vector2(0f, 0.5f);
        vrRt.anchoredPosition = new Vector2(0f, 0f);
        vrRt.sizeDelta = new Vector2(20f, ToggleH);

        var mrLabel = Tmp("MRLabel", toggleStrip, "MR", 12f, FontStyles.Bold);
        mrLabel.alignment = TextAlignmentOptions.Right;
        var mrRt = mrLabel.rectTransform;
        mrRt.anchorMin = new Vector2(1f, 0.5f);
        mrRt.anchorMax = new Vector2(1f, 0.5f);
        mrRt.pivot = new Vector2(1f, 0.5f);
        mrRt.anchoredPosition = new Vector2(0f, 0f);
        mrRt.sizeDelta = new Vector2(20f, ToggleH);

        // Switch toggle in middle (bind to PassthroughVRMRFader via VRMRSwitchToggleBinder)
        var switchRt = NewRect("VRMR_Switch", toggleStrip);
        switchRt.anchorMin = new Vector2(0.5f, 0.5f);
        switchRt.anchorMax = new Vector2(0.5f, 0.5f);
        switchRt.pivot = new Vector2(0.5f, 0.5f);
        switchRt.sizeDelta = new Vector2(28f, 16f);

        // Background
        var bgImg = switchRt.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.33f, 0.33f, 0.37f, 1f);

        // Checkmark/Handle
        var handleRt = NewRect("Handle", switchRt);
        handleRt.anchorMin = new Vector2(0f, 0.5f);
        handleRt.anchorMax = new Vector2(0f, 0.5f);
        handleRt.pivot = new Vector2(0f, 0.5f);
        handleRt.anchoredPosition = new Vector2(2f, 0f);
        handleRt.sizeDelta = new Vector2(12f, 12f);
        var handleImg = handleRt.gameObject.AddComponent<Image>();
        handleImg.color = Color.white;

        // Toggle component
        var toggle = switchRt.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = handleImg;
        toggle.isOn = false; // default VR

        // Move handle with state using simple helper (no animator)
        var slide = switchRt.gameObject.AddComponent<SimpleToggleHandleSlider>();
        slide.Init(handleRt, bgImg, handleImg, onX: 14f, offX: 2f);

        // Binder (user drags PassthroughVRMRFader)
        switchRt.gameObject.AddComponent<VRMRSwitchToggleBinder>();

        // Templates
        var templates = NewRect("Templates", canvasGo.transform);
        templates.gameObject.SetActive(false);
        var cardTemplate = BuildCardTemplate(templates);

        // Hook controllers
        var envSwitch = panel.gameObject.AddComponent<EnvironmentRuntimeSwitcher>();
        var panelCtrl = panel.gameObject.AddComponent<EnvironmentSelectionPanelController>();
        Set(panelCtrl, "switcher", envSwitch);
        Set(panelCtrl, "contentRoot", contentRt);
        Set(panelCtrl, "cardPrefab", cardTemplate.gameObject);

        Selection.activeGameObject = canvasGo;
        Debug.Log("已生成“场景选择”UI。请：1) 在 EnvironmentRuntimeSwitcher 配 environments；2) 在 VRMR_Switch 上的 VRMRSwitchToggleBinder 绑定 PassthroughVRMRFader。");
    }

    private static RectTransform BuildCardTemplate(Transform parent)
    {
        var card = NewRect("EnvCardTemplate", parent);
        var rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(CardSize, CardSize);
        var bg = card.gameObject.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.08f);

        var btn = card.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;

        var preview = NewRect("Preview", card);
        var pRt = preview.GetComponent<RectTransform>();
        pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 0.62f);
        pRt.pivot = new Vector2(0.5f, 0.5f);
        pRt.sizeDelta = new Vector2(PreviewSize, PreviewSize);
        var pImg = preview.gameObject.AddComponent<Image>();
        pImg.color = new Color(1f, 1f, 1f, 0.18f);

        var label = Tmp("Label", card, "场景名称", 16f, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.Center;
        var lRt = label.rectTransform;
        lRt.anchorMin = new Vector2(0f, 0f);
        lRt.anchorMax = new Vector2(1f, 0f);
        lRt.pivot = new Vector2(0.5f, 0f);
        lRt.anchoredPosition = new Vector2(0f, 10f);
        lRt.sizeDelta = new Vector2(0f, 30f);

        return card;
    }

    private static TextMeshProUGUI Tmp(string name, Transform parent, string text, float size, FontStyles style)
    {
        var rt = NewRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        return tmp;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
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

