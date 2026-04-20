using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 单个卡片组 UI
/// 负责管理堆叠/展开动画和卡片滚动
/// </summary>
public class CardGroupUI : MonoBehaviour
{
    [Header("组件引用")]
    public TextMeshProUGUI groupTitleText;
    public Button expandButton;
    public TextMeshProUGUI expandButtonText;
    public GameObject cardScrollArea;
    public Transform cardContainer;
    public ScrollRect scrollRect;

    [Header("卡片预制体")]
    [Tooltip("新方案：纯 UI 简化卡片预制体（推荐，优先使用）")]
    public GameObject uiCardPrefab;
    [Tooltip("国礼卡片预制体")]
    public GameObject giftCardPrefab;
    [Tooltip("传统文化元素卡片预制体")]
    public GameObject cultureCardPrefab;
    [Tooltip("目标国家文化元素卡片预制体")]
    public GameObject targetCountryCardPrefab;
    [Tooltip("文化禁忌卡片预制体")]
    public GameObject tabooCardPrefab;

    [Header("卡片布局")]
    [Tooltip("同时显示的最大卡片数量")]
    public int visibleCardCount = 5;
    [Tooltip("卡片宽度")]
    public float cardWidth = 200f;
    [Tooltip("卡片高度")]
    public float cardHeight = 300f;
    [Tooltip("卡片间距")]
    public float cardSpacing = 20f;

    [Header("堆叠设置")]
    [Tooltip("堆叠时的偏移量（营造层叠效果）")]
    public float stackOffsetX = 8f;
    public float stackOffsetY = -8f;

    [Header("动画设置")]
    public float expandDuration = 0.4f;
    public AnimationCurve expandCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 卡片组类型
    private CardGroupType groupType;
    // 当前卡片列表
    private List<CardData> cards = new List<CardData>();
    // 生成的卡片UI列表
    private List<CardUI> cardUIs = new List<CardUI>();
    // 是否已展开
    private bool isExpanded = false;

    private void Awake()
    {
        if (expandButton != null)
            expandButton.onClick.AddListener(ToggleExpand);

        // 初始隐藏滚动区域
        if (cardScrollArea != null)
            cardScrollArea.SetActive(false);
    }

    /// <summary>
    /// 初始化卡片组
    /// </summary>
    public void Initialize(CardGroupType type, List<CardData> cardList)
    {
        groupType = type;
        cards = cardList ?? new List<CardData>();

        // 设置标题
        if (groupTitleText != null)
            groupTitleText.text = GetGroupTitle(type);

        // 更新展开按钮文字
        UpdateExpandButtonText();

        // 收起状态
        Collapse(immediate: true);
    }

    /// <summary>
    /// 切换展开/收起
    /// </summary>
    public void ToggleExpand()
    {
        if (isExpanded)
            StartCoroutine(CollapseAnimation());
        else
            StartCoroutine(ExpandAnimation());
    }

    /// <summary>
    /// 展开卡片组
    /// </summary>
    public void Expand(bool immediate = false)
    {
        if (immediate)
        {
            isExpanded = true;
            if (cardScrollArea != null) cardScrollArea.SetActive(true);
            GenerateCards();
            UpdateExpandButtonText();
        }
        else
        {
            StartCoroutine(ExpandAnimation());
        }
    }

    /// <summary>
    /// 收起卡片组
    /// </summary>
    public void Collapse(bool immediate = false)
    {
        if (immediate)
        {
            isExpanded = false;
            if (cardScrollArea != null) cardScrollArea.SetActive(false);
            ClearCards();
            UpdateExpandButtonText();
        }
        else
        {
            StartCoroutine(CollapseAnimation());
        }
    }

    /// <summary>
    /// 展开动画
    /// </summary>
    private IEnumerator ExpandAnimation()
    {
        isExpanded = true;
        UpdateExpandButtonText();

        // 显示滚动区域
        if (cardScrollArea != null)
            cardScrollArea.SetActive(true);

        // 生成卡片
        GenerateCards();

        // 卡片逐个出现动画
        for (int i = 0; i < cardUIs.Count; i++)
        {
            if (cardUIs[i] != null)
            {
                var canvasGroup = cardUIs[i].GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = cardUIs[i].gameObject.AddComponent<CanvasGroup>();

                canvasGroup.alpha = 0f;
                StartCoroutine(FadeIn(canvasGroup, i * 0.05f));
            }
        }

        yield return new WaitForSeconds(expandDuration);
    }

    /// <summary>
    /// 收起动画
    /// </summary>
    private IEnumerator CollapseAnimation()
    {
        isExpanded = false;
        UpdateExpandButtonText();

        // 淡出
        float elapsed = 0f;
        float duration = expandDuration * 0.5f;

        var canvasGroup = cardScrollArea?.GetComponent<CanvasGroup>();
        if (canvasGroup == null && cardScrollArea != null)
            canvasGroup = cardScrollArea.AddComponent<CanvasGroup>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f - (elapsed / duration);
            yield return null;
        }

        if (cardScrollArea != null)
            cardScrollArea.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        ClearCards();
    }

    /// <summary>
    /// 淡入动画
    /// </summary>
    private IEnumerator FadeIn(CanvasGroup group, float delay)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        float duration = expandDuration * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = elapsed / duration;
            yield return null;
        }
        group.alpha = 1f;
    }

    /// <summary>
    /// 生成卡片UI
    /// </summary>
    private GameObject GetCardPrefab()
    {
        // 优先使用新方案的纯 UI 预制体，避免旧 prefab 的层级/坐标残留问题
        if (uiCardPrefab != null)
            return uiCardPrefab;

        switch (groupType)
        {
            case CardGroupType.GiftCards:             return giftCardPrefab;
            case CardGroupType.CultureElements:       return cultureCardPrefab;
            case CardGroupType.TargetCountryElements: return targetCountryCardPrefab;
            case CardGroupType.Taboos:                return tabooCardPrefab;
            default:                                  return giftCardPrefab;
        }
    }

    private void GenerateCards()
    {
        ClearCards();

        GameObject prefab = GetCardPrefab();
        if (prefab == null || cardContainer == null)
        {
            Debug.LogWarning($"[{GetGroupTitle(groupType)}] 卡片预制体未设置！");
            return;
        }

        // 设置 ScrollRect 内容宽度
        float totalWidth = cards.Count > 0
            ? cards.Count * (cardWidth + cardSpacing) - cardSpacing
            : 0f;
        var contentRect = cardContainer.GetComponent<RectTransform>();
        if (contentRect != null)
            contentRect.sizeDelta = new Vector2(totalWidth, cardHeight);

        for (int i = 0; i < cards.Count; i++)
        {
            GameObject cardObj = Instantiate(prefab);
            cardObj.transform.SetParent(cardContainer, false);
            cardObj.transform.localScale = Vector3.one;
            cardObj.transform.localRotation = Quaternion.identity;

            CardUI cardUI = cardObj.GetComponent<CardUI>();

            if (cardUI != null)
            {
                cardUI.Initialize(cards[i]);
                cardUIs.Add(cardUI);
            }

            // 设置卡片位置
            RectTransform rect = cardObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(cardWidth, cardHeight);
                rect.anchoredPosition = new Vector2(i * (cardWidth + cardSpacing), 0);
            }
        }

        // 重置滚动位置
        if (scrollRect != null)
            scrollRect.horizontalNormalizedPosition = 0f;
    }

    /// <summary>
    /// 清除所有卡片UI
    /// </summary>
    private void ClearCards()
    {
        // 先清理已记录的 CardUI
        foreach (var cardUI in cardUIs)
        {
            if (cardUI != null)
                Destroy(cardUI.gameObject);
        }
        cardUIs.Clear();

        // 再兜底清理容器下所有子物体，避免无 CardUI 的实例残留
        if (cardContainer != null)
        {
            for (int i = cardContainer.childCount - 1; i >= 0; i--)
                Destroy(cardContainer.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 更新展开按钮文字
    /// </summary>
    private void UpdateExpandButtonText()
    {
        if (expandButtonText == null) return;
        expandButtonText.text = isExpanded
            ? $"收起 ({cards.Count})"
            : $"展开 ({cards.Count})";
    }

    /// <summary>
    /// 获取卡片组标题
    /// </summary>
    private string GetGroupTitle(CardGroupType type)
    {
        switch (type)
        {
            case CardGroupType.GiftCards:             return "🎁 国礼推荐";
            case CardGroupType.CultureElements:       return "🏮 传统文化元素";
            case CardGroupType.TargetCountryElements: return "🌍 目标国家文化元素";
            case CardGroupType.Taboos:                return "⚠️ 文化禁忌";
            default:                                  return "卡片组";
        }
    }

    /// <summary>
    /// 获取当前展开状态
    /// </summary>
    public bool IsExpanded() => isExpanded;

    /// <summary>
    /// 获取卡片数量
    /// </summary>
    public int CardCount => cards.Count;
}
