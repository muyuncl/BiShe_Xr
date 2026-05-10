using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// 一键生成“材质横向选择 UI”到场景（不依赖完整模型生成界面）。
/// </summary>
public static class ModelGenMaterialSelectorGenerator
{
    private const float PanelWidth = 418f;
    private const float PanelHeight = 284f;
    private const float CardSize = 126f;
    private const float CardSpacing = 4f;
    private const float CardBottom = 46f;
    private const float LeftPadding = 35f;

    [MenuItem("Tools/UI/Generate Material Selector UI")]
    public static void Generate()
    {
        ModelGenMaterialCardPrefabGenerator.EnsurePrefabExists();

        var canvasGo = new GameObject("WorldSpaceMaterialSelectorCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Generate Material Selector UI");

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(560f, 360f);
        canvasRect.localScale = Vector3.one * 0.0012f;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        var panel = NewRect("MaterialPanel", canvasGo.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panel.anchoredPosition = Vector2.zero;
        var panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.color = new Color(0.17f, 0.17f, 0.19f, 0.94f);
        var renderDisplay = CreateTmp("RenderDisplay", panel.transform, "R e n d e r   D i s p l a y", 8f);
        renderDisplay.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);
        var renderRt = renderDisplay.rectTransform;
        renderRt.anchorMin = new Vector2(0f, 1f);
        renderRt.anchorMax = new Vector2(0f, 1f);
        renderRt.pivot = new Vector2(0f, 1f);
        renderRt.anchoredPosition = new Vector2(35f, -18f);
        renderRt.sizeDelta = new Vector2(180f, 16f);

        var title = CreateTmp("Title", panel.transform, "材 质 纹 理", 44f);
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Left;
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(34f, -42f);
        titleRt.sizeDelta = new Vector2(220f, 60f);

        var iconRt = NewRect("MaterialIcon", panel.transform);
        iconRt.anchorMin = new Vector2(1f, 1f);
        iconRt.anchorMax = new Vector2(1f, 1f);
        iconRt.pivot = new Vector2(1f, 1f);
        iconRt.anchoredPosition = new Vector2(-32f, -20f);
        iconRt.sizeDelta = new Vector2(44f, 44f);
        var iconBg = iconRt.gameObject.AddComponent<Image>();
        iconBg.color = new Color(1f, 1f, 1f, 0.2f);
        var iconLabel = CreateTmp("IconText", iconRt, "材", 24f);
        iconLabel.alignment = TextAlignmentOptions.Center;
        Stretch(iconLabel.rectTransform, 0, 0, 0, 0);

        var scrollContent = CreateHorizontalScroll(panel.transform);

        var templates = NewRect("Templates", canvasGo.transform);
        templates.gameObject.SetActive(false);
        var cardTemplateGo = ModelGenMaterialCardPrefabBuilder.InstantiateTemplate(templates.transform);

        var status = CreateTmp("StatusText", canvasGo.transform, "准备就绪", 16f);
        status.alignment = TextAlignmentOptions.Center;
        var statusRt = status.rectTransform;
        statusRt.anchorMin = statusRt.anchorMax = new Vector2(0.5f, 0f);
        statusRt.pivot = new Vector2(0.5f, 0f);
        statusRt.anchoredPosition = new Vector2(0f, 8f);
        statusRt.sizeDelta = new Vector2(500f, 28f);

        var controller = canvasGo.AddComponent<ModelGenUIController>();
        Set(controller, "materialContent", scrollContent);
        Set(controller, "materialCardTemplate", cardTemplateGo.GetComponent<ModelGenMaterialCardUI>());
        Set(controller, "statusText", status);

        Selection.activeGameObject = canvasGo;
        Debug.Log("已生成材质选择UI。请在 ModelGenUIController 上绑定 Material Catalog 和 VRPhotoTo3DController。");
    }

    private static Transform CreateHorizontalScroll(Transform parent)
    {
        var scrollGo = NewRect("MaterialScroll", parent);
        scrollGo.anchorMin = new Vector2(0f, 0f);
        scrollGo.anchorMax = new Vector2(1f, 0f);
        scrollGo.pivot = new Vector2(0f, 0f);
        scrollGo.offsetMin = new Vector2(LeftPadding, CardBottom);
        scrollGo.offsetMax = new Vector2(0f, CardBottom + CardSize);
        var scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = NewRect("Viewport", scrollGo.transform);
        Stretch(viewport, 0f, 0f, 0f, 0f);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        var content = NewRect("Content", viewport.transform);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 0f);
        contentRt.anchorMax = new Vector2(0f, 1f);
        contentRt.pivot = new Vector2(0f, 0.5f);

        var hlg = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = CardSpacing;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth = false;

        var fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRt;
        return content.transform;
    }

    private static TextMeshProUGUI CreateTmp(string name, Transform parent, string text, float size)
    {
        var rt = NewRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
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
        var fi = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        fi?.SetValue(target, value);
    }
}
