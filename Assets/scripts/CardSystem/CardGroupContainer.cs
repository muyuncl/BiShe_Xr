using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡片组总容器
/// 管理4套卡片组的堆叠显示和展开状态
/// </summary>
public class CardGroupContainer : MonoBehaviour
{
    [Header("卡片组引用")]
    public CardGroupUI giftCardsGroup;
    public CardGroupUI cultureElementsGroup;
    public CardGroupUI targetCountryElementsGroup;
    public CardGroupUI taboosGroup;

    [Header("堆叠设置")]
    [Tooltip("堆叠时各卡片组的Z轴偏移（营造层叠效果）")]
    public float stackDepthOffset = 5f;
    [Tooltip("堆叠时各卡片组的垂直偏移")]
    public float stackVerticalOffset = 10f;

    [Header("展开设置")]
    [Tooltip("展开后卡片组之间的间距")]
    public float groupSpacing = 30f;

    // 当前展开的卡片组
    private CardGroupUI currentExpandedGroup = null;

    private void Start()
    {
        // 初始堆叠布局
        ArrangeStacked();
    }

    /// <summary>
    /// 显示筛选结果
    /// </summary>
    public void DisplayFilterResult(CardGroupData result)
    {
        if (result == null) return;

        // 先收起所有已展开的卡片组
        CollapseAll();

        // 初始化各卡片组
        InitGroup(giftCardsGroup,               CardGroupType.GiftCards,             result.giftCards);
        InitGroup(cultureElementsGroup,         CardGroupType.CultureElements,        result.cultureElements);
        InitGroup(targetCountryElementsGroup,   CardGroupType.TargetCountryElements,  result.targetCountryElements);
        InitGroup(taboosGroup,                  CardGroupType.Taboos,                 result.taboos);

        // 恢复堆叠布局
        ArrangeStacked();

        Debug.Log("✅ 卡片组显示更新完成");
    }

    /// <summary>
    /// 初始化单个卡片组
    /// </summary>
    private void InitGroup(CardGroupUI groupUI, CardGroupType type, List<CardData> cards)
    {
        if (groupUI == null) return;

        groupUI.Initialize(type, cards);

        // 监听展开按钮（重新绑定避免重复）
        var expandBtn = groupUI.expandButton;
        if (expandBtn != null)
        {
            expandBtn.onClick.RemoveAllListeners();
            expandBtn.onClick.AddListener(() => OnGroupExpandToggle(groupUI));
        }
    }

    /// <summary>
    /// 点击卡片组时的处理
    /// 同时只能展开一个卡片组
    /// </summary>
    private void OnGroupExpandToggle(CardGroupUI targetGroup)
    {
        if (targetGroup.IsExpanded())
        {
            // 已展开 → 收起
            targetGroup.Collapse();
            currentExpandedGroup = null;
            ArrangeStacked();
        }
        else
        {
            // 未展开 → 先收起其他组，再展开当前组
            CollapseAll();
            targetGroup.Expand();
            currentExpandedGroup = targetGroup;
            ArrangeExpanded(targetGroup);
        }
    }

    /// <summary>
    /// 收起所有卡片组
    /// </summary>
    public void CollapseAll()
    {
        CollapseGroup(giftCardsGroup);
        CollapseGroup(cultureElementsGroup);
        CollapseGroup(targetCountryElementsGroup);
        CollapseGroup(taboosGroup);
        currentExpandedGroup = null;
    }

    private void CollapseGroup(CardGroupUI groupUI)
    {
        if (groupUI != null && groupUI.IsExpanded())
            groupUI.Collapse();
    }

    /// <summary>
    /// 堆叠布局：4套卡片组堆叠在一起
    /// </summary>
    private void ArrangeStacked()
    {
        var groups = GetAllGroups();

        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i] == null) continue;

            RectTransform rect = groups[i].GetComponent<RectTransform>();
            if (rect == null) continue;

            // 每组稍微偏移，营造层叠效果
            rect.anchoredPosition = new Vector2(
                i * stackDepthOffset,
                -i * stackVerticalOffset
            );

            // Z轴深度
            Vector3 pos = rect.localPosition;
            rect.localPosition = new Vector3(pos.x, pos.y, -i * stackDepthOffset);
        }
    }

    /// <summary>
    /// 展开布局：展开指定卡片组，其他组排在下方
    /// </summary>
    private void ArrangeExpanded(CardGroupUI expandedGroup)
    {
        var groups = GetAllGroups();
        float currentY = 0f;

        foreach (var group in groups)
        {
            if (group == null) continue;

            RectTransform rect = group.GetComponent<RectTransform>();
            if (rect == null) continue;

            rect.anchoredPosition = new Vector2(0, -currentY);
            rect.localPosition = new Vector3(
                rect.localPosition.x,
                rect.localPosition.y,
                0
            );

            // 展开组占用更多空间
            if (group == expandedGroup)
                currentY += rect.rect.height + groupSpacing;
            else
                currentY += 60f + groupSpacing; // 收起状态固定高度
        }
    }

    /// <summary>
    /// 获取所有卡片组列表
    /// </summary>
    private List<CardGroupUI> GetAllGroups()
    {
        return new List<CardGroupUI>
        {
            giftCardsGroup,
            cultureElementsGroup,
            targetCountryElementsGroup,
            taboosGroup
        };
    }
}
