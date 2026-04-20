using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIFigmaLayoutWindow : EditorWindow
{
    private float spacing = 16f;
    private float snapStep = 8f;
    private float cornerRadius = 24f;
    private bool applyRoundedGraphic = true;
    private bool showSceneGuides = true;
    private float sceneGuideThreshold = 6f;

    [MenuItem("Tools/UI/Figma Layout Helper")]
    [MenuItem("Window/UI/Figma Layout Helper")]
    public static void Open()
    {
        GetWindow<UIFigmaLayoutWindow>("Figma Layout");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("对齐", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("左对齐")) AlignLeft();
        if (GUILayout.Button("水平居中")) AlignCenterX();
        if (GUILayout.Button("右对齐")) AlignRight();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("顶对齐")) AlignTop();
        if (GUILayout.Button("垂直居中")) AlignCenterY();
        if (GUILayout.Button("底对齐")) AlignBottom();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("分布与间距", EditorStyles.boldLabel);
        spacing = EditorGUILayout.FloatField("目标间距", spacing);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("横向等间距")) DistributeHorizontal(spacing);
        if (GUILayout.Button("纵向等间距")) DistributeVertical(spacing);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("吸附与尺寸", EditorStyles.boldLabel);
        snapStep = EditorGUILayout.FloatField("吸附步长", snapStep);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("吸附位置")) SnapPositions();
        if (GUILayout.Button("吸附尺寸")) SnapSizes();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("统一宽度")) MatchWidth();
        if (GUILayout.Button("统一高度")) MatchHeight();
        if (GUILayout.Button("统一尺寸")) MatchSize();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("圆角 / 倒角", EditorStyles.boldLabel);
        cornerRadius = EditorGUILayout.FloatField("圆角半径", cornerRadius);
        applyRoundedGraphic = EditorGUILayout.Toggle("自动补 RoundedGraphic", applyRoundedGraphic);
        if (GUILayout.Button("应用圆角到所选对象")) ApplyCornerRadius();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Scene 对齐参考线", EditorStyles.boldLabel);
        showSceneGuides = EditorGUILayout.Toggle("启用参考线", UISceneAlignmentGuides.Enabled);
        sceneGuideThreshold = EditorGUILayout.FloatField("触发阈值", UISceneAlignmentGuides.SnapThreshold);
        if (GUILayout.Button("应用参考线设置"))
        {
            UISceneAlignmentGuides.Enabled = showSceneGuides;
            UISceneAlignmentGuides.SnapThreshold = sceneGuideThreshold;
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Figma 风格提示", EditorStyles.helpBox);
        EditorGUILayout.HelpBox("先多选同级 UI 节点，再做对齐/分布。统一尺寸以第一个选中对象为基准。移动单个 UI 节点时，如果与同级对象边或中心接近，会在 Scene 里显示参考线。", MessageType.Info);
    }

    private List<RectTransform> GetSelection()
    {
        var result = new List<RectTransform>();
        foreach (var obj in Selection.transforms)
        {
            if (obj is RectTransform rect)
                result.Add(rect);
        }
        return result;
    }

    private void AlignLeft()
    {
        ApplyToSelection(rects =>
        {
            float left = float.MaxValue;
            foreach (var rect in rects)
                left = Mathf.Min(left, GetLeft(rect));
            foreach (var rect in rects)
                SetLeft(rect, left);
        });
    }

    private void AlignCenterX()
    {
        ApplyToSelection(rects =>
        {
            float sum = 0f;
            foreach (var rect in rects)
                sum += rect.anchoredPosition.x;
            float center = sum / rects.Count;
            foreach (var rect in rects)
            {
                Undo.RecordObject(rect, "Align Center X");
                rect.anchoredPosition = new Vector2(center, rect.anchoredPosition.y);
                EditorUtility.SetDirty(rect);
            }
        });
    }

    private void AlignRight()
    {
        ApplyToSelection(rects =>
        {
            float right = float.MinValue;
            foreach (var rect in rects)
                right = Mathf.Max(right, GetRight(rect));
            foreach (var rect in rects)
                SetRight(rect, right);
        });
    }

    private void AlignTop()
    {
        ApplyToSelection(rects =>
        {
            float top = float.MinValue;
            foreach (var rect in rects)
                top = Mathf.Max(top, GetTop(rect));
            foreach (var rect in rects)
                SetTop(rect, top);
        });
    }

    private void AlignCenterY()
    {
        ApplyToSelection(rects =>
        {
            float sum = 0f;
            foreach (var rect in rects)
                sum += rect.anchoredPosition.y;
            float center = sum / rects.Count;
            foreach (var rect in rects)
            {
                Undo.RecordObject(rect, "Align Center Y");
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, center);
                EditorUtility.SetDirty(rect);
            }
        });
    }

    private void AlignBottom()
    {
        ApplyToSelection(rects =>
        {
            float bottom = float.MaxValue;
            foreach (var rect in rects)
                bottom = Mathf.Min(bottom, GetBottom(rect));
            foreach (var rect in rects)
                SetBottom(rect, bottom);
        });
    }

    private void DistributeHorizontal(float gap)
    {
        ApplyToSelection(rects =>
        {
            rects.Sort((a, b) => GetLeft(a).CompareTo(GetLeft(b)));
            float cursor = GetLeft(rects[0]);
            foreach (var rect in rects)
            {
                SetLeft(rect, cursor);
                cursor += rect.rect.width + gap;
            }
        }, 2);
    }

    private void DistributeVertical(float gap)
    {
        ApplyToSelection(rects =>
        {
            rects.Sort((a, b) => GetTop(b).CompareTo(GetTop(a)));
            float cursor = GetTop(rects[0]);
            foreach (var rect in rects)
            {
                SetTop(rect, cursor);
                cursor -= rect.rect.height + gap;
            }
        }, 2);
    }

    private void SnapPositions()
    {
        ApplyToSelection(rects =>
        {
            foreach (var rect in rects)
            {
                Undo.RecordObject(rect, "Snap Positions");
                rect.anchoredPosition = new Vector2(
                    Mathf.Round(rect.anchoredPosition.x / snapStep) * snapStep,
                    Mathf.Round(rect.anchoredPosition.y / snapStep) * snapStep);
                EditorUtility.SetDirty(rect);
            }
        });
    }

    private void SnapSizes()
    {
        ApplyToSelection(rects =>
        {
            foreach (var rect in rects)
            {
                Undo.RecordObject(rect, "Snap Sizes");
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Round(rect.rect.width / snapStep) * snapStep);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Round(rect.rect.height / snapStep) * snapStep);
                EditorUtility.SetDirty(rect);
            }
        });
    }

    private void MatchWidth()
    {
        ApplyToSelection(rects =>
        {
            float width = rects[0].rect.width;
            foreach (var rect in rects)
            {
                Undo.RecordObject(rect, "Match Width");
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                EditorUtility.SetDirty(rect);
            }
        }, 2);
    }

    private void MatchHeight()
    {
        ApplyToSelection(rects =>
        {
            float height = rects[0].rect.height;
            foreach (var rect in rects)
            {
                Undo.RecordObject(rect, "Match Height");
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                EditorUtility.SetDirty(rect);
            }
        }, 2);
    }

    private void MatchSize()
    {
        ApplyToSelection(rects =>
        {
            float width = rects[0].rect.width;
            float height = rects[0].rect.height;
            foreach (var rect in rects)
            {
                Undo.RecordObject(rect, "Match Size");
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                EditorUtility.SetDirty(rect);
            }
        }, 2);
    }

    private void ApplyCornerRadius()
    {
        ApplyToSelection(rects =>
        {
            foreach (var rect in rects)
            {
                var frame = rect.GetComponent<UIFrameData>();
                if (frame == null)
                    frame = Undo.AddComponent<UIFrameData>(rect.gameObject);
                Undo.RecordObject(frame, "Apply Corner Radius");
                frame.CornerRadius = cornerRadius;
                frame.FigmaSize = rect.rect.size;
                EditorUtility.SetDirty(frame);

                if (applyRoundedGraphic)
                {
                    var rounded = rect.GetComponent<RoundedGraphic>();
                    if (rounded == null)
                        rounded = Undo.AddComponent<RoundedGraphic>(rect.gameObject);
                    Undo.RecordObject(rounded, "Apply Corner Radius");
                    rounded.color = ResolveGraphicColor(rect.gameObject);
                    rounded.CornerRadius = cornerRadius;
                    EditorUtility.SetDirty(rounded);
                }
            }
        });
    }

    private Color ResolveGraphicColor(GameObject go)
    {
        var image = go.GetComponent<Image>();
        if (image != null)
            return image.color;
        return Color.white;
    }

    private void ApplyToSelection(System.Action<List<RectTransform>> action, int minimumCount = 1)
    {
        var rects = GetSelection();
        if (rects.Count < minimumCount)
        {
            EditorUtility.DisplayDialog("提示", $"请至少选择 {minimumCount} 个 UI 节点。", "确定");
            return;
        }
        action(rects);
    }

    private float GetLeft(RectTransform rect) => rect.anchoredPosition.x - rect.rect.width * rect.pivot.x;
    private float GetRight(RectTransform rect) => rect.anchoredPosition.x + rect.rect.width * (1f - rect.pivot.x);
    private float GetTop(RectTransform rect) => rect.anchoredPosition.y + rect.rect.height * (1f - rect.pivot.y);
    private float GetBottom(RectTransform rect) => rect.anchoredPosition.y - rect.rect.height * rect.pivot.y;

    private void SetLeft(RectTransform rect, float left)
    {
        Undo.RecordObject(rect, "Align Left");
        rect.anchoredPosition = new Vector2(left + rect.rect.width * rect.pivot.x, rect.anchoredPosition.y);
        EditorUtility.SetDirty(rect);
    }

    private void SetRight(RectTransform rect, float right)
    {
        Undo.RecordObject(rect, "Align Right");
        rect.anchoredPosition = new Vector2(right - rect.rect.width * (1f - rect.pivot.x), rect.anchoredPosition.y);
        EditorUtility.SetDirty(rect);
    }

    private void SetTop(RectTransform rect, float top)
    {
        Undo.RecordObject(rect, "Align Top");
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, top - rect.rect.height * (1f - rect.pivot.y));
        EditorUtility.SetDirty(rect);
    }

    private void SetBottom(RectTransform rect, float bottom)
    {
        Undo.RecordObject(rect, "Align Bottom");
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, bottom + rect.rect.height * rect.pivot.y);
        EditorUtility.SetDirty(rect);
    }
}
