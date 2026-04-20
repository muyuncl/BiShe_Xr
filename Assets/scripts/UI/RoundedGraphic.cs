using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
public class RoundedGraphic : MaskableGraphic
{
    [SerializeField, Min(0f)] private float cornerRadius = 24f;
    [SerializeField, Min(1)] private int cornerSegments = 6;

    public float CornerRadius
    {
        get => cornerRadius;
        set
        {
            cornerRadius = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float radius = Mathf.Clamp(cornerRadius, 0f, maxRadius);

        if (radius <= 0.01f)
        {
            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;

            vert.position = new Vector2(rect.xMin, rect.yMin); vh.AddVert(vert);
            vert.position = new Vector2(rect.xMin, rect.yMax); vh.AddVert(vert);
            vert.position = new Vector2(rect.xMax, rect.yMax); vh.AddVert(vert);
            vert.position = new Vector2(rect.xMax, rect.yMin); vh.AddVert(vert);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
            return;
        }

        Vector2 center = rect.center;
        UIVertex centerVert = UIVertex.simpleVert;
        centerVert.color = color;
        centerVert.position = center;
        vh.AddVert(centerVert);

        var points = new System.Collections.Generic.List<Vector2>();
        AddCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f);
        AddCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f);
        AddCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f);
        AddCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f);

        for (int i = 0; i < points.Count; i++)
        {
            UIVertex v = UIVertex.simpleVert;
            v.color = color;
            v.position = points[i];
            vh.AddVert(v);
        }

        for (int i = 0; i < points.Count; i++)
        {
            int next = i + 1;
            if (next >= points.Count) next = 0;
            vh.AddTriangle(0, i + 1, next + 1);
        }
    }

    private void AddCorner(System.Collections.Generic.List<Vector2> points, Vector2 cornerCenter, float radius, float startAngle, float endAngle)
    {
        for (int i = 0; i <= cornerSegments; i++)
        {
            float t = i / (float)cornerSegments;
            float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
            points.Add(new Vector2(
                cornerCenter.x + Mathf.Cos(angle) * radius,
                cornerCenter.y + Mathf.Sin(angle) * radius));
        }
    }
}
