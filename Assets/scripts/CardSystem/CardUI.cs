using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 单张卡片 UI
/// 负责显示卡片内容，并预留手势识别接口
/// </summary>
public class CardUI : MonoBehaviour
{
    [Header("UI 组件")]
    public Image cardImage;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI descriptionText;

    [Header("字号缩放基准（按 prefab 设计稿填写）")]
    [SerializeField] private float designCardHeight = 800f;
    [SerializeField] private float designTitleFontSize = 34f;
    [SerializeField] private float designDescriptionFontSize = 24f;

    // 当前卡片数据
    private CardData cardData;

    // 预留：手势识别回调
    public event System.Action<CardUI> OnGestureDetected;

    /// <summary>
    /// 初始化卡片
    /// </summary>
    public void Initialize(CardData data)
    {
        cardData = data;

        // 设置卡片名称
        if (cardNameText != null)
            cardNameText.text = data.name;

        // 设置描述
        if (descriptionText != null)
            descriptionText.text = data.description;

        // 加载图片
        if (cardImage != null)
        {
            Sprite sprite = CardManager.Instance?.LoadCardImage(data.image);
            if (sprite != null)
                cardImage.sprite = sprite;
        }
    }

    /// <summary>
    /// 按目标卡片高度等比缩放字体（基于 prefab 设计稿字号）
    /// </summary>
    public void ApplyScaledTypography(float targetCardHeight)
    {
        if (targetCardHeight <= 0f || designCardHeight <= 0f) return;

        float ratio = targetCardHeight / designCardHeight;
        float titleSize = Mathf.Max(1f, designTitleFontSize * ratio);
        float descSize = Mathf.Max(1f, designDescriptionFontSize * ratio);

        if (cardNameText != null)
        {
            cardNameText.enableAutoSizing = false;
            cardNameText.fontSize = titleSize;
            cardNameText.enableWordWrapping = false;
            cardNameText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (descriptionText != null)
        {
            descriptionText.enableAutoSizing = false;
            descriptionText.fontSize = descSize;
            descriptionText.enableWordWrapping = true;
            descriptionText.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    /// <summary>
    /// 获取卡片数据
    /// </summary>
    public CardData GetCardData() => cardData;

    /// <summary>
    /// 获取卡片图片 Sprite
    /// </summary>
    public Sprite GetCardSprite()
    {
        if (cardImage != null)
            return cardImage.sprite;
        return null;
    }

    // =============================================
    // 预留：手势识别接口
    // 未来实现双指捏合提取图片
    // =============================================

    /// <summary>
    /// 预留：当检测到双指捏合手势时调用
    /// </summary>
    public void OnPinchGestureDetected()
    {
        Debug.Log($"[预留] 检测到手势：{cardData?.name}");
        OnGestureDetected?.Invoke(this);
    }

    /// <summary>
    /// 预留：提取卡片图片为浮动物体
    /// </summary>
    public void ExtractImage()
    {
        // 未来实现：
        // 1. 获取卡片图片
        // 2. 创建 FloatingImage 物体
        // 3. 设置位置为卡片位置
        Debug.Log($"[预留] 提取图片：{cardData?.name}");
    }
}
