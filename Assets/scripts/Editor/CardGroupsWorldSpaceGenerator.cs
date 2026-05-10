using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Culture 720×410 顶栏横标题；Gift 720×175 左侧竖标题；Taboos+Target 共用一个 310×600（宽×高）面板，内左右两列竖向列表。
/// </summary>
public static class CardGroupsWorldSpaceGenerator
{
    private const float DuoShellW = 310f;
    private const float DuoShellH = 600f;
    /// <summary>单列宽 ≈ (外壳宽 − 左右边距 − 中间缝) / 2</summary>
    private const float DuoGutterSide = 6f;
    private const float DuoMidGap = 4f;
    private static float DuoColumnW => (DuoShellW - DuoGutterSide * 2f - DuoMidGap) * 0.5f;
    private static float DuoColumnH => DuoShellH - 16f;

    [MenuItem("Window/UI/Generate World Space Card Groups UI")]
    [MenuItem("Tools/UI/Generate World Space Card Groups UI")]
    public static void Generate()
    {
        var canvasGo = new GameObject("WorldSpaceCardGroupsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Generate World Space Card Groups UI");

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        if (Camera.main != null)
            canvas.worldCamera = Camera.main;

        if (canvasGo.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1500, 1120);
        canvasRect.localScale = Vector3.one * 0.0012f;

        var root = Box("CardGroupsRoot", canvasGo.transform, new Vector2(1280, 920), Vector2.zero, new Color(0, 0, 0, 0));

        var host = new GameObject("CardGroupsDisplayControllerHost", typeof(RectTransform), typeof(CardGroupsDisplayController));
        host.transform.SetParent(root.transform, false);
        Stretch(host.GetComponent<RectTransform>(), 0, 0, 0, 0);
        var displayController = host.GetComponent<CardGroupsDisplayController>();

        var culture = CreatePanel(root.transform, "CultureElementsPanel", new Vector2(720f, 410f), new Vector2(-280f, 248f), CardGroupType.CultureElements, "传统文化意象", CardGroupPanelLayoutMode.Horizontal, false);
        var gifts = CreatePanel(root.transform, "GiftCardsPanel", new Vector2(720f, 175f), new Vector2(-280f, -73f), CardGroupType.GiftCards, "过往国礼", CardGroupPanelLayoutMode.Horizontal, true);

        var (taboos, target) = CreateTaboosTargetDuoPanel(root.transform, new Vector2(400f, 5f));

        var carousel = culture.AddComponent<CardGroupCarouselScaler>();
        carousel.SetScrollRect(culture.GetComponentInChildren<ScrollRect>());

        Set(displayController, "taboosPanel", taboos);
        Set(displayController, "cultureElementsPanel", culture.GetComponent<CardGroupPanelView>());
        Set(displayController, "targetCountryElementsPanel", target);
        Set(displayController, "giftCardsPanel", gifts.GetComponent<CardGroupPanelView>());

        var manager = Object.FindObjectOfType<CardManager>();
        if (manager != null)
            Set(manager, "groupsDisplayController", displayController);

        Selection.activeGameObject = canvasGo;
    }

    /// <summary>外壳 310×600，内左右两列各一块 CardGroupPanelView（无独立底板）。</summary>
    static (CardGroupPanelView taboos, CardGroupPanelView target) CreateTaboosTargetDuoPanel(Transform parent, Vector2 shellPos)
    {
        var shell = Box("TaboosTargetDuoPanel", parent, new Vector2(DuoShellW, DuoShellH), shellPos, new Color(0.73f, 0.72f, 0.70f, 0.96f));

        float colW = DuoColumnW;
        float colH = DuoColumnH;
        var colSize = new Vector2(colW, colH);
        // 左列中心：距外壳左缘 gutter + colW/2 → 相对外壳中心为 -(shellW/2 - gutter - colW/2)
        float xOff = DuoShellW * 0.5f - DuoGutterSide - colW * 0.5f;
        var taboosGo = CreateCenteredSubPanel(shell.transform, "TaboosColumn", colSize, new Vector2(-xOff, 0f), CardGroupType.Taboos, "文化禁忌", CardGroupPanelLayoutMode.Vertical, false);
        var targetGo = CreateCenteredSubPanel(shell.transform, "TargetCountryColumn", colSize, new Vector2(xOff, 0f), CardGroupType.TargetCountryElements, "异域文化意象", CardGroupPanelLayoutMode.Vertical, false);

        return (taboosGo.GetComponent<CardGroupPanelView>(), targetGo.GetComponent<CardGroupPanelView>());
    }

    /// <summary>在父节点内居中摆放的子面板（透明底，仅标题+滚动区）。</summary>
    static GameObject CreateCenteredSubPanel(Transform parent, string name, Vector2 size, Vector2 anchoredPos, CardGroupType type, string title, CardGroupPanelLayoutMode mode, bool verticalTitle)
    {
        var panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = size;
        panelRect.anchoredPosition = anchoredPos;

        BuildPanelContent(panel, panelRect, size, type, title, mode, verticalTitle);
        return panel;
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 panelSize, Vector2 panelPos, CardGroupType type, string title, CardGroupPanelLayoutMode mode, bool verticalTitle)
    {
        var panel = Box(name, parent, panelSize, panelPos, new Color(0.73f, 0.72f, 0.70f, 0.96f));
        var panelRect = panel.GetComponent<RectTransform>();
        BuildPanelContent(panel, panelRect, panelSize, type, title, mode, verticalTitle);
        return panel;
    }

    static void BuildPanelContent(GameObject panel, RectTransform panelRect, Vector2 panelSize, CardGroupType type, string title, CardGroupPanelLayoutMode mode, bool verticalTitle)
    {
        float titleBarW = Mathf.Min(320f, panelSize.x - 32f);
        Vector2 titleSize;
        Vector2 titlePos;
        if (verticalTitle)
        {
            float verticalTitleH = Mathf.Clamp(panelSize.y - 48f, 72f, 380f);
            titleSize = new Vector2(46f, verticalTitleH);
            titlePos = new Vector2(-panelSize.x * 0.5f + 34f, panelSize.y * 0.5f - 14f);
        }
        else
        {
            titleSize = new Vector2(titleBarW, 28f);
            titlePos = new Vector2(0f, panelSize.y * 0.5f - 14f);
        }

        var titlePlate = Box("TitlePlate", panel.transform, titleSize, titlePos, new Color(0.91f, 0.90f, 0.88f, 1f));
        var titleRect = titlePlate.GetComponent<RectTransform>();
        titleRect.pivot = new Vector2(0.5f, 1f);

        var titleText = TextObj("TitleText", titlePlate.transform, verticalTitle ? Verticalize(title) : title, 12f, Vector2.zero, new Color(0.56f, 0.55f, 0.53f, 1f));
        Stretch(titleText.GetComponent<RectTransform>(), 0, 0, 0, 0);
        titleText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var scroll = new GameObject("Scroll View", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scroll.transform.SetParent(panel.transform, false);
        var scrollRt = scroll.GetComponent<RectTransform>();
        if (verticalTitle) Stretch(scrollRt, 68, 14, 10, 10); else Stretch(scrollRt, 14, 14, 56, 14);
        scroll.GetComponent<Image>().color = new Color(1, 1, 1, 0);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scroll.transform, false);
        Stretch(viewport.GetComponent<RectTransform>(), 0, 0, 0, 0);
        viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0);

        var content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(0, 1);
        contentRect.pivot = new Vector2(0, 1);
        contentRect.anchoredPosition = Vector2.zero;

        var scrollComponent = scroll.GetComponent<ScrollRect>();
        scrollComponent.viewport = viewport.GetComponent<RectTransform>();
        scrollComponent.content = contentRect;
        scrollComponent.movementType = ScrollRect.MovementType.Clamped;
        scrollComponent.scrollSensitivity = 20f;
        scrollComponent.horizontal = mode == CardGroupPanelLayoutMode.Horizontal;
        scrollComponent.vertical = mode != CardGroupPanelLayoutMode.Horizontal;

        HorizontalLayoutGroup horizontal = null;
        VerticalLayoutGroup vertical = null;
        GridLayoutGroup grid = null;

        if (mode == CardGroupPanelLayoutMode.Horizontal)
        {
            horizontal = content.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 8f;
            horizontal.childAlignment = TextAnchor.UpperLeft;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;
        }
        else if (mode == CardGroupPanelLayoutMode.Vertical)
        {
            vertical = content.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 10f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = false;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = false;
            vertical.childForceExpandHeight = false;
        }
        else
        {
            grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(72, 115.2f);
            grid.spacing = new Vector2(8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.childAlignment = TextAnchor.UpperLeft;
        }

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = mode == CardGroupPanelLayoutMode.Horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var settings = CreateLayoutSettings(panelSize, verticalTitle, mode, type);

        var existing = panel.GetComponent<CardGroupPanelView>();
        if (existing != null)
            Object.DestroyImmediate(existing);
        var panelView = panel.AddComponent<CardGroupPanelView>();
        panelView.Configure(
            type,
            title,
            mode,
            settings,
            panelRect,
            titleRect,
            titleText.GetComponent<TextMeshProUGUI>(),
            scrollComponent,
            viewport.GetComponent<RectTransform>(),
            contentRect,
            horizontal,
            vertical,
            grid,
            fitter);
    }

    static CardGroupPanelLayoutSettings CreateLayoutSettings(Vector2 panelSize, bool verticalTitle, CardGroupPanelLayoutMode mode, CardGroupType groupType)
    {
        var settings = new CardGroupPanelLayoutSettings
        {
            panelSize = panelSize,
            verticalTitle = verticalTitle,
            cardAspectWidth = 5f,
            cardAspectHeight = 8f
        };

        switch (groupType)
        {
            case CardGroupType.CultureElements:
                if (verticalTitle)
                {
                    settings.titlePlateSize = new Vector2(46f, Mathf.Clamp(panelSize.y - 52f, 92f, 360f));
                    settings.titlePlateAnchoredPosition = new Vector2(-panelSize.x * 0.5f + 34f, -10f);
                    settings.paddingLeft = 68f;
                    settings.paddingRight = 14f;
                    settings.paddingTop = 14f;
                    settings.paddingBottom = 14f;
                    settings.cardWidth = 108f;
                    settings.spacingX = 14f;
                }
                else
                {
                    settings.titlePlateSize = new Vector2(Mathf.Min(320f, panelSize.x - 32f), 28f);
                    settings.titlePlateAnchoredPosition = new Vector2(0f, -14f);
                    settings.paddingLeft = 14f;
                    settings.paddingRight = 14f;
                    settings.paddingTop = 52f;
                    settings.paddingBottom = 12f;
                    settings.cardWidth = 108f;
                    settings.spacingX = 14f;
                }

                break;
            case CardGroupType.GiftCards:
                if (verticalTitle)
                {
                    settings.titlePlateSize = new Vector2(46f, Mathf.Clamp(panelSize.y - 40f, 72f, 135f));
                    settings.titlePlateAnchoredPosition = new Vector2(-panelSize.x * 0.5f + 34f, -10f);
                    settings.paddingLeft = 68f;
                    settings.paddingRight = 14f;
                    settings.paddingTop = 12f;
                    settings.paddingBottom = 12f;
                    settings.cardWidth = 96f;
                    settings.spacingX = 12f;
                }
                else
                {
                    settings.titlePlateSize = new Vector2(Mathf.Min(280f, panelSize.x - 32f), 28f);
                    settings.titlePlateAnchoredPosition = new Vector2(0f, -14f);
                    settings.paddingLeft = 14f;
                    settings.paddingRight = 14f;
                    settings.paddingTop = 52f;
                    settings.paddingBottom = 12f;
                    settings.cardWidth = 96f;
                    settings.spacingX = 12f;
                }

                break;
            case CardGroupType.Taboos:
                settings.titlePlateSize = new Vector2(Mathf.Min(120f, panelSize.x - 8f), 28f);
                settings.titlePlateAnchoredPosition = new Vector2(0f, -14f);
                settings.paddingLeft = 4f;
                settings.paddingRight = 4f;
                settings.paddingTop = 52f;
                settings.paddingBottom = 10f;
                settings.cardWidth = Mathf.Max(48f, panelSize.x - 12f);
                settings.spacingY = 10f;
                settings.gridColumnCount = 1;
                break;
            case CardGroupType.TargetCountryElements:
                settings.titlePlateSize = new Vector2(Mathf.Min(120f, panelSize.x - 8f), 28f);
                settings.titlePlateAnchoredPosition = new Vector2(0f, -14f);
                settings.paddingLeft = 4f;
                settings.paddingRight = 4f;
                settings.paddingTop = 52f;
                settings.paddingBottom = 10f;
                settings.cardWidth = Mathf.Max(48f, panelSize.x - 12f);
                settings.spacingY = 10f;
                settings.gridColumnCount = 1;
                break;
            default:
                settings.titlePlateSize = new Vector2(180f, 28f);
                settings.titlePlateAnchoredPosition = new Vector2(0f, -14f);
                settings.paddingLeft = 14f;
                settings.paddingRight = 14f;
                settings.paddingTop = 56f;
                settings.paddingBottom = 14f;
                settings.cardWidth = 86f;
                settings.spacingX = 8f;
                settings.spacingY = 8f;
                settings.gridColumnCount = 4;
                break;
        }

        return settings;
    }

    static string Verticalize(string text)
    {
        return string.IsNullOrEmpty(text) ? string.Empty : string.Join("\n", text.ToCharArray());
    }

    static GameObject Box(string name, Transform parent, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        if (parent != null) go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        go.GetComponent<Image>().color = color;
        return go;
    }

    static GameObject TextObj(string name, Transform parent, string text, float size, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 24);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        return go;
    }

    static void Stretch(RectTransform rect, float l, float r, float t, float b)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(l, b);
        rect.offsetMax = new Vector2(-r, -t);
    }

    static void Set(object target, string fieldName, object value)
    {
        if (target == null) return;
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null) return;
        field.SetValue(target, value);
        if (target is Object unityObject) EditorUtility.SetDirty(unityObject);
    }
}
