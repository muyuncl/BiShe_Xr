using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 横向卡组滚动时按与视口中心的距离缩放子项（中间大、两侧小），挂在含 <see cref="ScrollRect"/> 的面板上。
/// </summary>
public class CardGroupCarouselScaler : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;

    [Tooltip("距视口水平中心越远，缩放越小；数值越大过渡越缓")]
    [Min(1f)]
    public float referenceFalloffPixels = 240f;

    [Range(0.5f, 1f)]
    public float minScale = 0.76f;

    [Range(0.6f, 1.2f)]
    public float maxScale = 1f;

    private readonly Vector3[] _viewportCorners = new Vector3[4];
    private readonly Vector3[] _childCorners = new Vector3[4];

    private void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>();
    }

    private void LateUpdate()
    {
        if (scrollRect == null || scrollRect.viewport == null || scrollRect.content == null)
            return;

        scrollRect.viewport.GetWorldCorners(_viewportCorners);
        float midX = (_viewportCorners[0].x + _viewportCorners[3].x) * 0.5f;

        var content = scrollRect.content;
        for (int i = 0; i < content.childCount; i++)
        {
            if (content.GetChild(i) is not RectTransform rt)
                continue;
            rt.GetWorldCorners(_childCorners);
            float cx = (_childCorners[0].x + _childCorners[3].x) * 0.5f;
            float d = Mathf.Abs(cx - midX);
            float t = Mathf.Clamp01(d / Mathf.Max(1f, referenceFalloffPixels));
            float s = Mathf.Lerp(maxScale, minScale, t);
            rt.localScale = new Vector3(s, s, 1f);
        }
    }

    public void SetScrollRect(ScrollRect sr)
    {
        scrollRect = sr;
    }
}
