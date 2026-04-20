using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class UISceneAlignmentGuides
{
    private const string EnabledKey = "BiSheXR_UIAlignmentGuides_Enabled";
    private const string SnapThresholdKey = "BiSheXR_UIAlignmentGuides_SnapThreshold";

    [MenuItem("Window/UI/Toggle Scene Alignment Guides")]
    private static void ToggleGuidesMenu()
    {
        Enabled = !Enabled;
        SceneView.RepaintAll();
    }

    [MenuItem("Window/UI/Toggle Scene Alignment Guides", true)]
    private static bool ToggleGuidesMenuValidate()
    {
        Menu.SetChecked("Window/UI/Toggle Scene Alignment Guides", Enabled);
        return true;
    }

    public static bool Enabled
    {
        get => EditorPrefs.GetBool(EnabledKey, true);
        set => EditorPrefs.SetBool(EnabledKey, value);
    }

    public static float SnapThreshold
    {
        get => EditorPrefs.GetFloat(SnapThresholdKey, 6f);
        set => EditorPrefs.SetFloat(SnapThresholdKey, Mathf.Max(1f, value));
    }

    static UISceneAlignmentGuides()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private struct GuideLine
    {
        public bool vertical;
        public float position;
        public float min;
        public float max;
        public Color color;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!Enabled) return;
        if (Selection.activeTransform is not RectTransform active) return;
        if (Selection.transforms.Length != 1) return;

        Event e = Event.current;
        if (e == null) return;
        if (Tools.current != Tool.Move && Tools.current != Tool.Rect) return;

        var activeParent = active.parent as RectTransform;
        if (activeParent == null) return;

        var siblings = CollectSiblingRects(active, activeParent);
        if (siblings.Count == 0) return;

        var activeWorld = GetWorldCorners(active);
        var activeBounds = ToBounds(activeWorld);

        List<GuideLine> guides = new List<GuideLine>();
        EvaluateVerticalGuides(activeBounds, siblings, guides);
        EvaluateHorizontalGuides(activeBounds, siblings, guides);

        if (guides.Count == 0) return;

        Handles.BeginGUI();
        foreach (var guide in guides)
            DrawGuide(sceneView, guide);
        Handles.EndGUI();
        sceneView.Repaint();
    }

    private static List<RectTransform> CollectSiblingRects(RectTransform active, RectTransform parent)
    {
        var list = new List<RectTransform>();
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i) as RectTransform;
            if (child == null || child == active || !child.gameObject.activeInHierarchy) continue;
            list.Add(child);
        }
        return list;
    }

    private static void EvaluateVerticalGuides(Bounds activeBounds, List<RectTransform> siblings, List<GuideLine> guides)
    {
        float best = SnapThreshold + 1f;
        GuideLine? bestGuide = null;

        foreach (var sibling in siblings)
        {
            var bounds = ToBounds(GetWorldCorners(sibling));
            TryVertical(activeBounds.min.x, bounds.min.x, activeBounds, bounds, ref best, ref bestGuide, new Color(0.2f, 0.73f, 1f, 0.95f));
            TryVertical(activeBounds.center.x, bounds.center.x, activeBounds, bounds, ref best, ref bestGuide, new Color(0.45f, 1f, 0.78f, 0.95f));
            TryVertical(activeBounds.max.x, bounds.max.x, activeBounds, bounds, ref best, ref bestGuide, new Color(1f, 0.78f, 0.32f, 0.95f));
        }

        if (bestGuide.HasValue)
            guides.Add(bestGuide.Value);
    }

    private static void EvaluateHorizontalGuides(Bounds activeBounds, List<RectTransform> siblings, List<GuideLine> guides)
    {
        float best = SnapThreshold + 1f;
        GuideLine? bestGuide = null;

        foreach (var sibling in siblings)
        {
            var bounds = ToBounds(GetWorldCorners(sibling));
            TryHorizontal(activeBounds.min.y, bounds.min.y, activeBounds, bounds, ref best, ref bestGuide, new Color(0.2f, 0.73f, 1f, 0.95f));
            TryHorizontal(activeBounds.center.y, bounds.center.y, activeBounds, bounds, ref best, ref bestGuide, new Color(0.45f, 1f, 0.78f, 0.95f));
            TryHorizontal(activeBounds.max.y, bounds.max.y, activeBounds, bounds, ref best, ref bestGuide, new Color(1f, 0.78f, 0.32f, 0.95f));
        }

        if (bestGuide.HasValue)
            guides.Add(bestGuide.Value);
    }

    private static void TryVertical(float a, float b, Bounds active, Bounds other, ref float best, ref GuideLine? guide, Color color)
    {
        float diff = Mathf.Abs(a - b);
        if (diff > SnapThreshold || diff >= best) return;
        best = diff;
        guide = new GuideLine
        {
            vertical = true,
            position = b,
            min = Mathf.Min(active.min.y, other.min.y),
            max = Mathf.Max(active.max.y, other.max.y),
            color = color
        };
    }

    private static void TryHorizontal(float a, float b, Bounds active, Bounds other, ref float best, ref GuideLine? guide, Color color)
    {
        float diff = Mathf.Abs(a - b);
        if (diff > SnapThreshold || diff >= best) return;
        best = diff;
        guide = new GuideLine
        {
            vertical = false,
            position = b,
            min = Mathf.Min(active.min.x, other.min.x),
            max = Mathf.Max(active.max.x, other.max.x),
            color = color
        };
    }

    private static Vector3[] GetWorldCorners(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return corners;
    }

    private static Bounds ToBounds(Vector3[] corners)
    {
        Bounds bounds = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
            bounds.Encapsulate(corners[i]);
        return bounds;
    }

    private static void DrawGuide(SceneView sceneView, GuideLine guide)
    {
        Vector2 p1;
        Vector2 p2;

        if (guide.vertical)
        {
            p1 = HandleUtility.WorldToGUIPoint(new Vector3(guide.position, guide.min, 0f));
            p2 = HandleUtility.WorldToGUIPoint(new Vector3(guide.position, guide.max, 0f));
        }
        else
        {
            p1 = HandleUtility.WorldToGUIPoint(new Vector3(guide.min, guide.position, 0f));
            p2 = HandleUtility.WorldToGUIPoint(new Vector3(guide.max, guide.position, 0f));
        }

        Handles.color = guide.color;
        Handles.DrawAAPolyLine(3f, p1, p2);
    }
}
