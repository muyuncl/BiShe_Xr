using UnityEngine;

/// <summary>
/// 手势识别处理器（预留）
/// 未来实现双指捏合提取卡片图片
/// </summary>
public class GestureHandler : MonoBehaviour
{
    [Header("手势设置（预留）")]
    [Tooltip("双指捏合触发阈值（像素）")]
    public float pinchThreshold = 50f;

    [Tooltip("捏合检测范围（世界单位）")]
    public float detectionRadius = 0.1f;

    // 预留：当前检测到的卡片
    private CardUI targetCard = null;

    // 预留：双指初始距离
    private float initialPinchDistance = 0f;

    // 预留：是否正在捏合
    private bool isPinching = false;

    private void Update()
    {
        // 预留：手势检测入口
        // 未来在此调用 DetectPinchGesture()
    }

    // =============================================
    // 预留：所有方法均为空实现
    // 未来根据 XR Interaction Toolkit 版本补全
    // =============================================

    /// <summary>
    /// 预留：检测双指捏合手势
    /// </summary>
    private void DetectPinchGesture()
    {
        // 未来实现：
        // 1. 获取两根手指的位置
        // 2. 计算两指距离变化
        // 3. 如果距离减小超过阈值 → 触发捏合
    }

    /// <summary>
    /// 预留：检测捏合目标卡片
    /// </summary>
    private CardUI DetectTargetCard(Vector3 position)
    {
        // 未来实现：
        // 在 position 附近做 Raycast
        // 找到最近的 CardUI 组件
        return null;
    }

    /// <summary>
    /// 预留：处理卡片图片提取
    /// </summary>
    private void HandleCardExtraction(CardUI card)
    {
        if (card == null) return;

        // 未来实现：
        // 1. 调用 card.OnPinchGestureDetected()
        // 2. 创建 FloatingImage
        // 3. 设置 FloatingImage 位置
        Debug.Log($"[预留] 触发卡片提取: {card.GetCardData()?.name}");
        card.OnPinchGestureDetected();
    }

    /// <summary>
    /// 预留：捏合开始
    /// </summary>
    private void OnPinchStart(Vector3 pinchCenter)
    {
        // 未来实现
    }

    /// <summary>
    /// 预留：捏合更新
    /// </summary>
    private void OnPinchUpdate(float currentDistance)
    {
        // 未来实现
    }

    /// <summary>
    /// 预留：捏合结束
    /// </summary>
    private void OnPinchEnd()
    {
        // 未来实现
        isPinching = false;
        targetCard = null;
    }
}

/// <summary>
/// 浮动图片（预留）
/// 未来实现：从卡片提取的可交互图片
/// </summary>
public class FloatingImage : MonoBehaviour
{
    [Header("浮动图片设置（预留）")]
    public UnityEngine.UI.Image image;
    public float floatHeight = 0.3f;

    // 预留：图片来源卡片
    private CardUI sourceCard;

    /// <summary>
    /// 预留：初始化浮动图片
    /// </summary>
    public void Initialize(UnityEngine.Sprite sprite, CardUI source)
    {
        sourceCard = source;
        if (image != null)
            image.sprite = sprite;

        // 未来实现：设置位置、添加物理效果
        Debug.Log($"[预留] 浮动图片初始化: {source?.GetCardData()?.name}");
    }

    /// <summary>
    /// 预留：拖拽图片
    /// </summary>
    public void OnDrag(Vector3 position)
    {
        // 未来实现
    }

    /// <summary>
    /// 预留：旋转图片
    /// </summary>
    public void OnRotate(float angle)
    {
        // 未来实现
    }

    /// <summary>
    /// 预留：删除图片
    /// </summary>
    public void Dismiss()
    {
        // 未来实现
        Destroy(gameObject);
    }
}
