using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// 旧版完整模型生成 UI 生成器（已停用，保留仅用于历史参考）。
/// 请改用 <see cref="ModelGenGenerateButtonWorldSpaceGenerator"/>。
/// </summary>
public static class ModelGenUIWorldSpaceGenerator
{
    private const string CanvasName = "WorldSpaceModelGenCanvas";
    private const float CanvasW = 1600f;
    private const float CanvasH = 900f;
    private const float WorldScale = 0.0012f;

    // 已移除菜单入口，避免误生成旧版完整 UI。
    public static void Generate()
    {
        var canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Generate Model Generation World Space UI");

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        if (Camera.main != null)
            canvas.worldCamera = Camera.main;

        if (canvasGo.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(CanvasW, CanvasH);
        canvasRect.localScale = Vector3.one * WorldScale;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        var controller = canvasGo.AddComponent<ModelGenUIController>();

        var root = NewRect("MainLayout", canvasGo.transform);
        Stretch(root, 0, 0, 0, 0);

        var leftCol = NewRect("LeftColumn", root.transform);
        AnchorLeftFraction(leftCol, 0f, 0.3f, 16f, 24f, 8f, 24f);

        var leftStack = NewRect("LeftStack", leftCol.transform);
        Stretch(leftStack, 0, 0, 0, 0);
        var leftV = leftStack.gameObject.AddComponent<VerticalLayoutGroup>();
        leftV.spacing = 12f;
        leftV.padding = new RectOffset(0, 0, 0, 0);
        leftV.childAlignment = TextAnchor.UpperCenter;
        leftV.childControlHeight = true;
        leftV.childControlWidth = true;
        leftV.childForceExpandHeight = true;
        leftV.childForceExpandWidth = true;

        var panelMat = CreatePanelShell(leftStack.transform, "PanelMaterial", 1f);
        TmpTitle(panelMat.transform, "材质纹理");
        var matScroll = CreateHorizontalScroll(panelMat.transform, "MaterialScroll");
        AddFlexibleScrollLayout(matScroll.parent.parent.gameObject, 200f);

        var centerCol = NewRect("CenterColumn", root.transform);
        AnchorLeftFraction(centerCol, 0.3f, 0.9f, 0f, 24f, 0f, 24f);

        var stage = NewRect("StageRoot", centerCol.transform);
        var stageRt = stage.GetComponent<RectTransform>();
        stageRt.anchorMin = new Vector2(0.5f, 0f);
        stageRt.anchorMax = new Vector2(0.5f, 0f);
        stageRt.pivot = new Vector2(0.5f, 0f);
        stageRt.anchoredPosition = new Vector2(0f, 72f);
        stageRt.sizeDelta = new Vector2(Mathf.Min(420f, CanvasW * 0.45f), 420f);

        var vase = TmpCenter("VasePlaceholder", stage.transform, "🏺", 72);
        vase.alignment = TextAlignmentOptions.Center;
        var vaseRt = vase.GetComponent<RectTransform>();
        vaseRt.anchorMin = vaseRt.anchorMax = new Vector2(0.5f, 0.65f);
        vaseRt.sizeDelta = new Vector2(120f, 120f);

        var glow = BoxImage("GlowRing", stage.transform, new Color(1f, 1f, 1f, 0.2f));
        var glowRt = glow.GetComponent<RectTransform>();
        glowRt.anchorMin = glowRt.anchorMax = new Vector2(0.5f, 0.48f);
        glowRt.sizeDelta = new Vector2(180f, 22f);

        var paper = BoxImage("PaperSketch", stage.transform, new Color(0.96f, 0.95f, 0.9f, 1f));
        var paperRt = paper.GetComponent<RectTransform>();
        paperRt.anchorMin = new Vector2(0.1f, 0.08f);
        paperRt.anchorMax = new Vector2(0.9f, 0.35f);

        var status = TmpCenter("StatusLine", stage.transform, "准备就绪", 16);
        status.fontStyle = FontStyles.Bold;
        status.color = new Color32(18, 18, 24, 255);
        var statusRt = status.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0.1f, 0f);
        statusRt.anchorMax = new Vector2(0.9f, 0.12f);
        statusRt.offsetMin = statusRt.offsetMax = Vector2.zero;

        var rightCol = NewRect("RightColumn", root.transform);
        AnchorLeftFraction(rightCol, 0.9f, 1f, 8f, 24f, 16f, 24f);

        var actions = NewRect("ActionsPanel", rightCol.transform);
        var actionsRt = actions.GetComponent<RectTransform>();
        actionsRt.anchorMin = new Vector2(0.5f, 0f);
        actionsRt.anchorMax = new Vector2(0.5f, 0f);
        actionsRt.pivot = new Vector2(0.5f, 0f);
        actionsRt.anchoredPosition = new Vector2(0f, 64f);
        actionsRt.sizeDelta = new Vector2(137f, 187f);

        var actionsBg = actions.gameObject.AddComponent<Image>();
        actionsBg.color = new Color(0.67f, 0.67f, 0.67f, 0.95f);

        var genBtn = CreateGenerateButtonPanel(actions.transform);
        var tip = TmpCenter("GenerateTip", actions.transform, "请先上色\n再点击生成", 20);
        tip.fontStyle = FontStyles.Bold;
        tip.alignment = TextAlignmentOptions.Center;
        var tipRt = tip.GetComponent<RectTransform>();
        tipRt.anchorMin = new Vector2(0f, 0f);
        tipRt.anchorMax = new Vector2(1f, 0f);
        tipRt.pivot = new Vector2(0.5f, 0f);
        tipRt.anchoredPosition = new Vector2(0f, 8f);
        tipRt.sizeDelta = new Vector2(0f, 62f);

        var templates = NewRect("Templates", canvasGo.transform);
        templates.gameObject.SetActive(false);

        var matCardTemplate = CreateMaterialCardTemplate(templates.transform);

        Set(controller, "materialContent", matScroll);
        Set(controller, "materialCardTemplate", matCardTemplate.GetComponent<ModelGenMaterialCardUI>());
        Set(controller, "statusText", status);
        Set(controller, "generateButton", genBtn.GetComponent<Button>());
        Set(controller, "libraryButton", null);

        Selection.activeGameObject = canvasGo;
        Debug.Log("已生成 " + CanvasName + "。请将 Canvas 挂到 XRHeadFollowUIManager，并把 VRPhotoTo3DController 拖到 ModelGenUIController。");
    }

    private static GameObject CreatePanelShell(Transform parent, string name, float flexHeight)
    {
        var shell = NewRect(name, parent);
        var le = shell.gameObject.AddComponent<LayoutElement>();
        le.flexibleHeight = flexHeight;
        le.minHeight = flexHeight > 1.5f ? 200f : 240f;

        var bg = shell.gameObject.AddComponent<Image>();
        bg.color = new Color(0.17f, 0.17f, 0.19f, 0.94f);

        var v = shell.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(14, 14, 14, 14);
        v.spacing = 8f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = true;
        v.childForceExpandWidth = true;

        Stretch(shell.GetComponent<RectTransform>(), 0, 0, 0, 0);
        return shell.gameObject;
    }

    private static void TmpTitle(Transform parent, string title)
    {
        var t = TmpCenter("Title", parent, title, 15);
        t.fontStyle = FontStyles.Bold;
        t.color = new Color(0.94f, 0.94f, 0.96f);
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 30f;
        le.minHeight = 28f;
    }

    private static Transform CreateVerticalScroll(Transform parent, string name)
    {
        var scrollGo = NewRect(name, parent);
        Stretch(scrollGo.GetComponent<RectTransform>(), 0, 0, 0, 0);
        var scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        var scrollImg = scrollGo.gameObject.AddComponent<Image>();
        scrollImg.color = new Color(0f, 0f, 0f, 0.22f);

        var viewport = NewRect("Viewport", scrollGo.transform);
        Stretch(viewport, 0, 0, 0, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        var vpImg = viewport.gameObject.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0f);

        var content = NewRect("Content", viewport.transform);
        var cr = content.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0f, 1f);
        cr.anchorMax = new Vector2(1f, 1f);
        cr.pivot = new Vector2(0.5f, 1f);
        cr.anchoredPosition = Vector2.zero;

        var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 4f;
        vl.childAlignment = TextAnchor.UpperCenter;
        vl.childControlWidth = true;
        vl.childForceExpandWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandHeight = false;

        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = cr;
        return content.transform;
    }

    private static Transform CreateHorizontalScroll(Transform parent, string name)
    {
        var scrollGo = NewRect(name, parent);
        Stretch(scrollGo.GetComponent<RectTransform>(), 0, 0, 0, 0);
        var scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        var scrollImg = scrollGo.gameObject.AddComponent<Image>();
        scrollImg.color = new Color(1f, 1f, 1f, 0f);

        var viewport = NewRect("Viewport", scrollGo.transform);
        Stretch(viewport, 0, 0, 0, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        var vpClear = viewport.gameObject.AddComponent<Image>();
        vpClear.color = new Color(1f, 1f, 1f, 0f);

        var content = NewRect("Content", viewport.transform);
        var cr = content.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0f, 0f);
        cr.anchorMax = new Vector2(0f, 1f);
        cr.pivot = new Vector2(0f, 0.5f);
        cr.anchoredPosition = Vector2.zero;

        var hl = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 12f;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = false;
        hl.childControlHeight = false;
        hl.childForceExpandHeight = false;
        hl.childForceExpandWidth = false;

        var hFit = content.gameObject.AddComponent<ContentSizeFitter>();
        hFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        hFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = cr;
        return content.transform;
    }

    private static GameObject CreateMaterialCardTemplate(Transform parent)
    {
        ModelGenMaterialCardPrefabGenerator.EnsurePrefabExists();
        return ModelGenMaterialCardPrefabBuilder.InstantiateTemplate(parent);
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

    private static GameObject BoxImage(string name, Transform parent, Color c)
    {
        var go = NewRect(name, parent);
        go.gameObject.AddComponent<Image>().color = c;
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

    private static void AnchorLeftFraction(RectTransform r, float xmin, float xmax, float padLeft, float padRight, float padBottom, float padTop)
    {
        r.anchorMin = new Vector2(xmin, 0f);
        r.anchorMax = new Vector2(xmax, 1f);
        r.offsetMin = new Vector2(padLeft, padBottom);
        r.offsetMax = new Vector2(-padRight, -padTop);
    }

    private static void Set(object target, string field, object value)
    {
        if (target == null)
            return;
        var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f == null)
            return;
        f.SetValue(target, value);
        if (target is Object o)
            EditorUtility.SetDirty(o);
    }

    private static void AddFlexibleScrollLayout(GameObject scrollRoot, float minHeight)
    {
        var le = scrollRoot.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f;
        le.minHeight = minHeight;
    }
}
