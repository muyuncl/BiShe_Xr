using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIFrameData : MonoBehaviour
{
    [SerializeField] private float cornerRadius = 24f;
    [SerializeField] private Vector2 figmaSize = new Vector2(160f, 48f);

    public float CornerRadius
    {
        get => cornerRadius;
        set => cornerRadius = Mathf.Max(0f, value);
    }

    public Vector2 FigmaSize
    {
        get => figmaSize;
        set => figmaSize = value;
    }
}
