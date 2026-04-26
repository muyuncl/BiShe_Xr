using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UIFlowControlBarGenerator
{
    private const string FlowCanvasName = "FlowUICanvas";

    [MenuItem("Tools/UI/Generate Flow Control Bar")]
    public static void Generate()
    {
        var canvas = FindOrCreateCanvas();
        EnsureEventSystem();

        var flowHost = FindOrCreateFlowController(canvas.transform);
        var flowController = flowHost.GetComponent<UIFlowController>();

        var barRoot = CreateBarRoot(canvas.transform);
        var guideBtn = CreateSmallButton(barRoot.transform, "GuideButton", "教程", "GUIDE", new Vector2(-360f, 0f));
        var backBtn = CreateSmallButton(barRoot.transform, "BackButton", "返回", "BACK", new Vector2(-180f, 0f));
        var nextBtn = CreateCenterButton(barRoot.transform, "NextButton", "下一步", "PROCEED", new Vector2(0f, 0f));
        var assetsBtn = CreateSmallButton(barRoot.transform, "AssetsButton", "模型库", "ASSETS", new Vector2(180f, 0f));
        var exitBtn = CreateSmallButton(barRoot.transform, "ExitButton", "退出", "EXIT", new Vector2(360f, 0f));

        EnsurePageRoots(canvas.transform, flowController);
        BindFlowButtons(flowController, nextBtn, backBtn, exitBtn);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = barRoot;
        Debug.Log("✅ 已生成总控 UI（下一步/返回/退出可用，教程/模型库占位）");
    }

    private static Canvas FindOrCreateCanvas()
    {
        var existing = GameObject.Find(FlowCanvasName);
        if (existing != null)
        {
            var existingCanvas = existing.GetComponent<Canvas>();
            if (existingCanvas != null)
                return existingCanvas;
            Object.DestroyImmediate(existing);
        }

        var canvasGo = new GameObject(FlowCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        // Quest 风格“总控 HUD”更推荐 World Space + 相机相对位姿锁定（运行时与预览一致）
        canvas.renderMode = RenderMode.WorldSpace;
        if (Camera.main != null)
            canvas.worldCamera = Camera.main;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 10f;

        // 给一个“看起来像屏幕”的基准尺寸，配合缩放到世界空间
        var rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1920f, 1080f);
        rect.localScale = Vector3.one * 0.001f; // 1920px -> 1.92m（大致可调）

        // 自动补一个相机相对位姿锁定（如果用户没手动加）
        if (canvasGo.GetComponent<XRRelativePlacement>() == null)
        {
            var rel = canvasGo.AddComponent<XRRelativePlacement>();
            // 默认：略低于视线、向前 1.2m
            var so = new SerializedObject(rel);
            so.FindProperty("localPositionOffset").vector3Value = new Vector3(0f, -0.25f, 1.2f);
            so.FindProperty("localEulerOffset").vector3Value = Vector3.zero;
            so.FindProperty("applyOnStart").boolValue = true;
            so.FindProperty("followContinuously").boolValue = true;
            so.FindProperty("startDelayFrames").intValue = 2;
            so.FindProperty("positionSmoothTime").floatValue = 0.06f;
            so.FindProperty("rotationLerpSpeed").floatValue = 12f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject FindOrCreateFlowController(Transform parent)
    {
        var existing = Object.FindObjectOfType<UIFlowController>();
        if (existing != null) return existing.gameObject;
        var host = new GameObject("UIFlowControllerHost", typeof(UIFlowController));
        host.transform.SetParent(parent, false);
        return host;
    }

    private static GameObject CreateBarRoot(Transform parent)
    {
        var existing = parent.Find("FlowControlBar");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = new GameObject("FlowControlBar", typeof(RectTransform), typeof(RoundedGraphic), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 24f);
        rect.sizeDelta = new Vector2(1080f, 120f);

        var bg = root.GetComponent<RoundedGraphic>();
        bg.color = new Color(0.92f, 0.92f, 0.92f, 0.95f);
        bg.CornerRadius = 56f;
        return root;
    }

    private static Button CreateSmallButton(Transform parent, string name, string zh, string en, Vector2 pos)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image));
        root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(120f, 96f);

        var image = root.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);

        CreateLabel(root.transform, "LabelEN", en, 14f, new Vector2(0f, 10f), new Color(0.67f, 0.67f, 0.67f, 1f), FontStyles.Normal);
        CreateLabel(root.transform, "LabelZH", zh, 24f, new Vector2(0f, -22f), new Color(0.42f, 0.42f, 0.42f, 1f), FontStyles.Bold);
        return root.GetComponent<Button>();
    }

    private static Button CreateCenterButton(Transform parent, string name, string zh, string en, Vector2 pos)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(RoundedGraphic));
        root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(130f, 130f);

        var bg = root.GetComponent<RoundedGraphic>();
        bg.color = new Color(0.08f, 0.09f, 0.1f, 0.98f);
        bg.CornerRadius = 65f;

        CreateLabel(root.transform, "Arrow", "→", 60f, new Vector2(0f, 24f), new Color(1f, 1f, 1f, 1f), FontStyles.Bold);
        CreateLabel(root.transform, "LabelEN", en, 13f, new Vector2(0f, -6f), new Color(0.82f, 0.82f, 0.82f, 1f), FontStyles.Normal);
        CreateLabel(root.transform, "LabelZH", zh, 22f, new Vector2(0f, -32f), new Color(1f, 1f, 1f, 1f), FontStyles.Bold);
        return root.GetComponent<Button>();
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float size, Vector2 anchoredPos, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(140f, 36f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void EnsurePageRoots(Transform canvasRoot, UIFlowController controller)
    {
        var p1 = FindOrCreateRoot(canvasRoot, "Page1_DeskAnchor");
        var p2 = FindOrCreateRoot(canvasRoot, "Page2_CardsAndFilter");
        var p3 = FindOrCreateRoot(canvasRoot, "Page3");

        var so = new SerializedObject(controller);
        var pagesProp = so.FindProperty("pageRoots");
        pagesProp.arraySize = 3;
        pagesProp.GetArrayElementAtIndex(0).objectReferenceValue = p1;
        pagesProp.GetArrayElementAtIndex(1).objectReferenceValue = p2;
        pagesProp.GetArrayElementAtIndex(2).objectReferenceValue = p3;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject FindOrCreateRoot(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t.gameObject;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go;
    }

    private static void BindFlowButtons(UIFlowController controller, Button next, Button back, Button exit)
    {
        var so = new SerializedObject(controller);
        so.FindProperty("nextButton").objectReferenceValue = next;
        so.FindProperty("backButton").objectReferenceValue = back;
        so.FindProperty("exitButton").objectReferenceValue = exit;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
