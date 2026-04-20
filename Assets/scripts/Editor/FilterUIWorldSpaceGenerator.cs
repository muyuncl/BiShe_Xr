using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class FilterUIWorldSpaceGenerator
{
    [MenuItem("Window/UI/Generate World Space Filter UI")]
    public static void Generate()
    {
        var canvasGo = new GameObject("WorldSpaceFilterCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Generate World Space Filter UI");
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1200, 1800);
        canvasRect.localScale = Vector3.one * 0.0012f;

        var panel = Box("FilterPanel", canvasGo.transform, new Vector2(570, 860), new Vector2(0, 0), new Color(0.79f, 0.78f, 0.76f, 0.92f));
        var fp = panel.AddComponent<FilterPanelController>();
        var repo = new GameObject("RecipientRepository", typeof(RecipientRepository)).GetComponent<RecipientRepository>(); repo.transform.SetParent(panel.transform, false);
        var rs = new GameObject("RecipientSelectorControllerHost", typeof(RecipientSelectorController)).GetComponent<RecipientSelectorController>(); rs.transform.SetParent(panel.transform, false);
        var ps = new GameObject("PoliticalSelectorControllerHost", typeof(PoliticalSelectorController)).GetComponent<PoliticalSelectorController>(); ps.transform.SetParent(panel.transform, false);
        var sc = new GameObject("SelectedConditionsControllerHost", typeof(SelectedConditionsController)).GetComponent<SelectedConditionsController>(); sc.transform.SetParent(panel.transform, false);
        var templates = new GameObject("Templates", typeof(RectTransform)); templates.transform.SetParent(panel.transform, false); templates.SetActive(false);

        var rf = ButtonObj("RecipientFoldButton", panel.transform, new Vector2(500, 72), new Vector2(0, 320), "Recipient / 赠送对象");
        var rd = Box("RecipientDropdown", panel.transform, new Vector2(500, 210), new Vector2(0, 200), new Color(1, 1, 1, 0.06f));
        var c1 = Column("Column1Root", rd.transform, new Vector2(-170, 0));
        var c2 = Column("Column2Root", rd.transform, new Vector2(0, 0)); c2.SetActive(false);
        var c3 = Column("Column3Root", rd.transform, new Vector2(170, 0)); c3.SetActive(false);

        var pfold = ButtonObj("PoliticalFoldButton", panel.transform, new Vector2(500, 72), new Vector2(0, 60), "Political / 政治意向");
        var pd = Box("PoliticalDropdown", panel.transform, new Vector2(500, 180), new Vector2(0, -70), new Color(1, 1, 1, 0.06f)); pd.SetActive(false);
        var options = new GameObject("OptionsContent", typeof(RectTransform), typeof(GridLayoutGroup)); options.transform.SetParent(pd.transform, false); Stretch(options.GetComponent<RectTransform>(), 10, 10, 10, 10); var grid = options.GetComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(145, 36); grid.spacing = new Vector2(10, 10); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;

        var selected = Box("SelectedConditionsSection", panel.transform, new Vector2(500, 120), new Vector2(0, -270), new Color(0, 0, 0, 0));
        var rt = Row("RecipientTagsContent", selected.transform, new Vector2(0, 25));
        var pt = Row("PoliticalTagsContent", selected.transform, new Vector2(0, -25));
        var reset = ButtonObj("ResetButton", panel.transform, new Vector2(144, 90), new Vector2(-110, -365), "重置");
        var apply = ButtonObj("ApplyButton", panel.transform, new Vector2(256, 90), new Vector2(90, -365), "应用筛选");

        var opt = RecipientTemplate(templates.transform);
        var pol = PoliticalTemplate(templates.transform);
        var tag = SelectedTagTemplate(templates.transform);

        Set(fp, "recipientFoldButton", rf.GetComponent<Button>());
        Set(fp, "politicalFoldButton", pfold.GetComponent<Button>());
        Set(fp, "recipientDropdown", rd);
        Set(fp, "politicalDropdown", pd);
        Set(fp, "recipientRepository", repo);
        Set(fp, "recipientSelector", rs);
        Set(fp, "politicalSelector", ps);
        Set(fp, "selectedConditions", sc);
        Set(fp, "cardManager", Object.FindObjectOfType<CardManager>());

        Set(rs, "repository", repo);
        Set(rs, "column1Root", c1); Set(rs, "column1Content", c1.transform.Find("Scroll View/Viewport/Content"));
        Set(rs, "column2Root", c2); Set(rs, "column2Content", c2.transform.Find("Scroll View/Viewport/Content"));
        Set(rs, "column3Root", c3); Set(rs, "column3Content", c3.transform.Find("Scroll View/Viewport/Content"));
        Set(rs, "optionItemPrefab", opt.GetComponent<RecipientOptionItemUI>());

        Set(ps, "optionsContent", options.transform);
        Set(ps, "optionPrefab", pol.GetComponent<PoliticalOptionTagUI>());

        Set(sc, "recipientTagsContent", rt.transform);
        Set(sc, "politicalTagsContent", pt.transform);
        Set(sc, "tagPrefab", tag.GetComponent<SelectedConditionTagUI>());

        reset.GetComponent<Button>().onClick.AddListener(fp.ResetFilter);
        apply.GetComponent<Button>().onClick.AddListener(fp.ApplyFilter);

        var manager = Object.FindObjectOfType<CardManager>();
        if (manager != null) Set(manager, "filterPanelController", fp);
        Selection.activeGameObject = canvasGo;
    }

    static GameObject Column(string name, Transform parent, Vector2 pos)
    {
        var root = Box(name, parent, new Vector2(150, 170), pos, new Color(1, 1, 1, 0.12f));
        var scroll = new GameObject("Scroll View", typeof(RectTransform), typeof(ScrollRect), typeof(Image)); scroll.transform.SetParent(root.transform, false); Stretch(scroll.GetComponent<RectTransform>(), 0, 0, 0, 0); scroll.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image)); viewport.transform.SetParent(scroll.transform, false); Stretch(viewport.GetComponent<RectTransform>(), 0, 0, 0, 0); viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)); content.transform.SetParent(viewport.transform, false);
        var cr = content.GetComponent<RectTransform>(); cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1); cr.pivot = new Vector2(.5f, 1);
        var vl = content.GetComponent<VerticalLayoutGroup>(); vl.spacing = 2; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.childForceExpandWidth = true;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var sr = scroll.GetComponent<ScrollRect>(); sr.viewport = viewport.GetComponent<RectTransform>(); sr.content = cr; sr.horizontal = false; sr.vertical = true;
        return root;
    }

    static GameObject Row(string name, Transform parent, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup)); go.transform.SetParent(parent, false); var r = go.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(500, 40); r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = pos; var h = go.GetComponent<HorizontalLayoutGroup>(); h.spacing = 10; h.childForceExpandWidth = false; h.childForceExpandHeight = false; h.childControlWidth = false; h.childControlHeight = false; h.childAlignment = TextAnchor.MiddleLeft; return go;
    }

    static GameObject RecipientTemplate(Transform parent)
    {
        var root = new GameObject("RecipientOptionTemplate", typeof(RectTransform), typeof(Image), typeof(RecipientOptionItemUI)); root.transform.SetParent(parent, false); root.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 36); root.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        var nav = ButtonObj("NavigateButton", root.transform, new Vector2(88, 30), new Vector2(-18, 0), "标签"); nav.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        var label = nav.transform.Find("Text").GetComponent<TextMeshProUGUI>(); label.alignment = TextAlignmentOptions.Left;
        var arrow = TextObj("ArrowObject", root.transform, "›", 18, new Vector2(58, 0));
        var select = ButtonObj("SelectButton", root.transform, new Vector2(46, 24), new Vector2(28, 0), "选择");
        var mark = TextObj("SelectedMarkObject", root.transform, "✓", 16, new Vector2(64, 0)); mark.SetActive(false);
        var ui = root.GetComponent<RecipientOptionItemUI>(); Set(ui, "navigateButton", nav.GetComponent<Button>()); Set(ui, "selectButton", select.GetComponent<Button>()); Set(ui, "labelText", label); Set(ui, "selectButtonText", select.transform.Find("Text").GetComponent<TextMeshProUGUI>()); Set(ui, "arrowObject", arrow); Set(ui, "selectedMarkObject", mark); Set(ui, "background", root.GetComponent<Image>()); return root;
    }

    static GameObject PoliticalTemplate(Transform parent)
    {
        var root = ButtonObj("PoliticalOptionTemplate", parent, new Vector2(145, 36), Vector2.zero, "政治意向"); root.AddComponent<PoliticalOptionTagUI>(); var ui = root.GetComponent<PoliticalOptionTagUI>(); Set(ui, "button", root.GetComponent<Button>()); Set(ui, "labelText", root.transform.Find("Text").GetComponent<TextMeshProUGUI>()); Set(ui, "background", root.GetComponent<Image>()); return root;
    }

    static GameObject SelectedTagTemplate(Transform parent)
    {
        var root = Box("SelectedTagTemplate", parent, new Vector2(110, 34), Vector2.zero, new Color(1, 1, 1, 0.08f)); root.AddComponent<SelectedConditionTagUI>();
        var text = TextObj("LabelText", root.transform, "标签", 18, new Vector2(-18, 0));
        var remove = ButtonObj("RemoveButton", root.transform, new Vector2(24, 24), new Vector2(36, 0), "×"); remove.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        var ui = root.GetComponent<SelectedConditionTagUI>(); Set(ui, "labelText", text.GetComponent<TextMeshProUGUI>()); Set(ui, "removeButton", remove.GetComponent<Button>()); return root;
    }

    static GameObject ButtonObj(string name, Transform parent, Vector2 size, Vector2 pos, string txt)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); if (parent != null) go.transform.SetParent(parent, false); var r = go.GetComponent<RectTransform>(); r.sizeDelta = size; r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = pos; go.GetComponent<Image>().color = new Color(1, 1, 1, 0.08f); var t = TextObj("Text", go.transform, txt, 20, Vector2.zero); Stretch(t.GetComponent<RectTransform>(), 0, 0, 0, 0); t.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center; return go;
    }

    static GameObject Box(string name, Transform parent, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); if (parent != null) go.transform.SetParent(parent, false); var r = go.GetComponent<RectTransform>(); r.sizeDelta = size; r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = pos; go.GetComponent<Image>().color = color; return go;
    }

    static GameObject TextObj(string name, Transform parent, string text, float size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false); var r = go.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(120, 24); r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = pos; var tmp = go.GetComponent<TextMeshProUGUI>(); tmp.text = text; tmp.fontSize = size; tmp.color = Color.white; return go;
    }

    static void Stretch(RectTransform rect, float l, float r, float t, float b) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(l, b); rect.offsetMax = new Vector2(-r, -t); }
    static void Set(object target, string field, object value) { if (target == null) return; var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); if (f == null) return; f.SetValue(target, value); if (target is Object o) EditorUtility.SetDirty(o); }
}
