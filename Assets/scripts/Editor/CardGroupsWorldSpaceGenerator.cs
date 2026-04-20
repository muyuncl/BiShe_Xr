using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CardGroupsWorldSpaceGenerator
{
    [MenuItem("Window/UI/Generate World Space Card Groups UI")]
    [MenuItem("Tools/UI/Generate World Space Card Groups UI")]
    public static void Generate()
    {
        var canvasGo = new GameObject("WorldSpaceCardGroupsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Generate World Space Card Groups UI");

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1500, 1120);
        canvasRect.localScale = Vector3.one * 0.0012f;

        var root = Box("CardGroupsRoot", canvasGo.transform, new Vector2(1280, 920), Vector2.zero, new Color(0, 0, 0, 0));

        var host = new GameObject("CardGroupsDisplayControllerHost", typeof(RectTransform), typeof(CardGroupsDisplayController));
        host.transform.SetParent(root.transform, false);
        Stretch(host.GetComponent<RectTransform>(), 0, 0, 0, 0);
        var displayController = host.GetComponent<CardGroupsDisplayController>();

        var taboos = CreatePanel(root.transform, "TaboosPanel", new Vector2(920, 120), new Vector2(-134, 360), CardGroupType.Taboos, "禁忌卡组", CardGroupPanelLayoutMode.Horizontal, true);
        var culture = CreatePanel(root.transform, "CultureElementsPanel", new Vector2(450, 580), new Vector2(-286, -10), CardGroupType.CultureElements, "文化元素卡组", CardGroupPanelLayoutMode.Grid, false);
        var target = CreatePanel(root.transform, "TargetCountryElementsPanel", new Vector2(450, 580), new Vector2(178, -10), CardGroupType.TargetCountryElements, "目标国家文化元素卡组", CardGroupPanelLayoutMode.Grid, false);
        var gifts = CreatePanel(root.transform, "GiftCardsPanel", new Vector2(180, 720), new Vector2(550, 60), CardGroupType.GiftCards, "国礼卡组", CardGroupPanelLayoutMode.Vertical, false);

        Set(displayController, "taboosPanel", taboos.GetComponent<CardGroupPanelView>());
        Set(displayController, "cultureElementsPanel", culture.GetComponent<CardGroupPanelView>());
        Set(displayController, "targetCountryElementsPanel", target.GetComponent<CardGroupPanelView>());
        Set(displayController, "giftCardsPanel", gifts.GetComponent<CardGroupPanelView>());

        var manager = Object.FindObjectOfType<CardManager>();
        if (manager != null)
            Set(manager, "groupsDisplayController", displayController);

        Selection.activeGameObject = canvasGo;
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 panelSize, Vector2 panelPos, CardGroupType type, string title, CardGroupPanelLayoutMode mode, bool verticalTitle)
    {
        var panel = Box(name, parent, panelSize, panelPos, new Color(0.73f, 0.72f, 0.70f, 0.96f));
        var panelRect = panel.GetComponent<RectTransform>();

        var titleSize = verticalTitle ? new Vector2(46, 92) : new Vector2(180, 28);
        var titlePos = verticalTitle ? new Vector2(-panelSize.x * 0.5f + 34f, panelSize.y * 0.5f - 14f) : new Vector2(0, panelSize.y * 0.5f - 14f);
        var titlePlate = Box("TitlePlate", panel.transform, titleSize, titlePos, new Color(0.91f, 0.90f, 0.88f, 1f));
        var titleRect = titlePlate.GetComponent<RectTransform>();
        titleRect.pivot = new Vector2(0.5f, 1f);

        var titleText = TextObj("TitleText", titlePlate.transform, verticalTitle ? Verticalize(title) : title, 12f, Vector2.zero, new Color(0.56f, 0.55f, 0.53f, 1f));
        Stretch(titleText.GetComponent<RectTransform>(), 0, 0, 0, 0);
        titleText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var scroll = new GameObject("Scroll View", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scroll.transform.SetParent(panel.transform, false);
        var scrollRect = scroll.GetComponent<RectTransform>();
        if (verticalTitle) Stretch(scrollRect, 68, 14, 10, 10); else Stretch(scrollRect, 14, 14, 56, 14);
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

        var settings = CreateLayoutSettings(panelSize, verticalTitle, mode);

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

        return panel;
    }

    static CardGroupPanelLayoutSettings CreateLayoutSettings(Vector2 panelSize, bool verticalTitle, CardGroupPanelLayoutMode mode)
    {
        var settings = new CardGroupPanelLayoutSettings
        {
            panelSize = panelSize,
            verticalTitle = verticalTitle,
            cardAspectWidth = 5f,
            cardAspectHeight = 8f
        };

        if (mode == CardGroupPanelLayoutMode.Horizontal)
        {
            settings.titlePlateSize = new Vector2(46f, 92f);
            settings.titlePlateAnchoredPosition = new Vector2(-panelSize.x * 0.5f + 34f, -10f);
            settings.paddingLeft = 68f;
            settings.paddingRight = 14f;
            settings.paddingTop = 10f;
            settings.paddingBottom = 10f;
            settings.cardWidth = 34f;
            settings.spacingX = 8f;
        }
        else if (mode == CardGroupPanelLayoutMode.Vertical)
        {
            settings.titlePlateSize = new Vector2(78f, 28f);
            settings.titlePlateAnchoredPosition = new Vector2(0f, -14f);
            settings.paddingLeft = 12f;
            settings.paddingRight = 12f;
            settings.paddingTop = 56f;
            settings.paddingBottom = 14f;
            settings.cardWidth = 92f;
            settings.spacingY = 10f;
            settings.gridColumnCount = 1;
        }
        else
        {
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
