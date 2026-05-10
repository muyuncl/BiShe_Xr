using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CardGroupPanelLayoutMode { Horizontal, Grid, Vertical }

[System.Serializable]
public class CardGroupPanelLayoutSettings
{
    public Vector2 panelSize = new Vector2(320f, 420f);
    public Vector2 titlePlateSize = new Vector2(160f, 28f);
    public Vector2 titlePlateAnchoredPosition = new Vector2(0f, -14f);
    public bool verticalTitle;
    public float paddingLeft = 14f;
    public float paddingRight = 14f;
    public float paddingTop = 56f;
    public float paddingBottom = 14f;
    public float cardWidth = 72f;
    public float cardAspectWidth = 5f;
    public float cardAspectHeight = 8f;
    public float spacingX = 8f;
    public float spacingY = 8f;
    public int gridColumnCount = 4;

    public float GetCardHeight()
    {
        if (cardAspectWidth <= 0f) return cardWidth;
        return cardWidth * (cardAspectHeight / cardAspectWidth);
    }
}

public class CardGroupPanelView : MonoBehaviour
{
    [Header("基础配置")]
    [SerializeField] private CardGroupType groupType;
    [SerializeField] private string groupTitleOverride = string.Empty;
    [SerializeField] private CardGroupPanelLayoutMode layoutMode = CardGroupPanelLayoutMode.Grid;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private CardGroupPanelLayoutSettings layoutSettings = new CardGroupPanelLayoutSettings();

    [Header("UI 引用")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private RectTransform titlePlateRect;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private HorizontalLayoutGroup horizontalLayoutGroup;
    [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;
    [SerializeField] private ContentSizeFitter contentSizeFitter;

    private readonly List<GameObject> spawnedCards = new();
    public CardGroupType GroupType => groupType;

    private void Reset() { CacheDefaults(); ApplyLayout(); }
    private void Awake() { CacheDefaults(); ApplyLayout(); }
    private void OnValidate() { CacheDefaults(); ApplyLayout(); }

    private void CacheDefaults()
    {
        if (panelRect == null) panelRect = transform as RectTransform;
        if (scrollRect != null && viewportRect == null) viewportRect = scrollRect.viewport;
        if (scrollRect != null && contentRect == null && scrollRect.content != null) contentRect = scrollRect.content;
        if (contentRect != null && horizontalLayoutGroup == null) horizontalLayoutGroup = contentRect.GetComponent<HorizontalLayoutGroup>();
        if (contentRect != null && verticalLayoutGroup == null) verticalLayoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        if (contentRect != null && gridLayoutGroup == null) gridLayoutGroup = contentRect.GetComponent<GridLayoutGroup>();
        if (contentRect != null && contentSizeFitter == null) contentSizeFitter = contentRect.GetComponent<ContentSizeFitter>();
    }

    public void ApplyLayout()
    {
        if (panelRect != null) panelRect.sizeDelta = layoutSettings.panelSize;
        if (titlePlateRect != null)
        {
            titlePlateRect.sizeDelta = layoutSettings.titlePlateSize;
            titlePlateRect.anchorMin = new Vector2(0.5f, 1f);
            titlePlateRect.anchorMax = new Vector2(0.5f, 1f);
            titlePlateRect.pivot = new Vector2(0.5f, 1f);
            titlePlateRect.anchoredPosition = layoutSettings.titlePlateAnchoredPosition;
        }
        if (titleText != null) titleText.text = FormatTitle(GetResolvedTitle(), layoutSettings.verticalTitle);
        if (viewportRect != null)
        {
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(layoutSettings.paddingLeft, layoutSettings.paddingBottom);
            viewportRect.offsetMax = new Vector2(-layoutSettings.paddingRight, -layoutSettings.paddingTop);
        }
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
        }
        ConfigureScrollRect();
        ConfigureLayoutGroup();
    }

    public void Display(List<CardData> cards)
    {
        ApplyLayout();
        ClearCards();
        if (cards == null || cards.Count == 0 || contentRect == null || cardPrefab == null) { RebuildLayout(); return; }

        for (int i = 0; i < cards.Count; i++)
        {
            var root = CreateDisplayRoot();
            spawnedCards.Add(root);
            var cardObject = Instantiate(cardPrefab, root.transform, false);
            cardObject.transform.localScale = Vector3.one;
            cardObject.transform.localPosition = Vector3.zero;
            cardObject.transform.localRotation = Quaternion.identity;

            ApplyCardSizing(root.GetComponent<RectTransform>());
            StretchChildIfPossible(cardObject);

            var cardUI = cardObject.GetComponent<CardUI>() ?? cardObject.GetComponentInChildren<CardUI>(true);
            if (cardUI != null)
            {
                cardUI.Initialize(cards[i]);
                cardUI.ApplyScaledTypography(layoutSettings.GetCardHeight());
            }
        }

        RebuildLayout();
        ResetScrollPosition();
    }

    public void Configure(CardGroupType type, string title, CardGroupPanelLayoutMode mode, CardGroupPanelLayoutSettings settings, RectTransform panel, RectTransform titlePlate, TextMeshProUGUI titleLabel, ScrollRect scroll, RectTransform viewport, RectTransform content, HorizontalLayoutGroup horizontal, VerticalLayoutGroup vertical, GridLayoutGroup grid, ContentSizeFitter fitter)
    {
        groupType = type; groupTitleOverride = title; layoutMode = mode; layoutSettings = settings ?? new CardGroupPanelLayoutSettings();
        panelRect = panel; titlePlateRect = titlePlate; titleText = titleLabel; scrollRect = scroll; viewportRect = viewport; contentRect = content;
        horizontalLayoutGroup = horizontal; verticalLayoutGroup = vertical; gridLayoutGroup = grid; contentSizeFitter = fitter;
        ApplyLayout();
    }

    public void SetGroupType(CardGroupType type)
    {
        groupType = type;
        if (titleText != null) titleText.text = FormatTitle(GetResolvedTitle(), layoutSettings.verticalTitle);
    }

    public void SetGroupTitle(string title)
    {
        groupTitleOverride = title;
        if (titleText != null) titleText.text = FormatTitle(GetResolvedTitle(), layoutSettings.verticalTitle);
    }

    private void ApplyCardSizing(RectTransform rect)
    {
        if (rect == null) return;
        float w = layoutSettings.cardWidth;
        float h = layoutSettings.GetCardHeight();
        rect.sizeDelta = new Vector2(w, h);
        var le = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.preferredHeight = h; le.minWidth = w; le.minHeight = h; le.flexibleWidth = 0f; le.flexibleHeight = 0f;
    }

    private void ConfigureScrollRect()
    {
        if (scrollRect == null) return;
        scrollRect.horizontal = layoutMode == CardGroupPanelLayoutMode.Horizontal;
        scrollRect.vertical = layoutMode != CardGroupPanelLayoutMode.Horizontal;
    }

    private void ConfigureLayoutGroup()
    {
        if (horizontalLayoutGroup != null)
        {
            horizontalLayoutGroup.enabled = layoutMode == CardGroupPanelLayoutMode.Horizontal;
            horizontalLayoutGroup.spacing = layoutSettings.spacingX;
            horizontalLayoutGroup.childAlignment = TextAnchor.UpperLeft;
            horizontalLayoutGroup.childControlWidth = false; horizontalLayoutGroup.childControlHeight = false;
            horizontalLayoutGroup.childForceExpandWidth = false; horizontalLayoutGroup.childForceExpandHeight = false;
        }
        if (verticalLayoutGroup != null)
        {
            verticalLayoutGroup.enabled = layoutMode == CardGroupPanelLayoutMode.Vertical;
            verticalLayoutGroup.spacing = layoutSettings.spacingY;
            verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            verticalLayoutGroup.childControlWidth = false; verticalLayoutGroup.childControlHeight = false;
            verticalLayoutGroup.childForceExpandWidth = false; verticalLayoutGroup.childForceExpandHeight = false;
        }
        if (gridLayoutGroup != null)
        {
            gridLayoutGroup.enabled = layoutMode == CardGroupPanelLayoutMode.Grid;
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = Mathf.Max(1, layoutSettings.gridColumnCount);
            gridLayoutGroup.cellSize = new Vector2(layoutSettings.cardWidth, layoutSettings.GetCardHeight());
            gridLayoutGroup.spacing = new Vector2(layoutSettings.spacingX, layoutSettings.spacingY);
            gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        }
        if (contentSizeFitter != null)
        {
            contentSizeFitter.horizontalFit = layoutMode == CardGroupPanelLayoutMode.Horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private void RebuildLayout()
    {
        if (contentRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        if (viewportRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);
        if (panelRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
    }

    private void ResetScrollPosition()
    {
        if (scrollRect == null) return;
        if (layoutMode == CardGroupPanelLayoutMode.Horizontal) scrollRect.horizontalNormalizedPosition = 0f;
        else scrollRect.verticalNormalizedPosition = 1f;
    }

    private GameObject CreateDisplayRoot()
    {
        var root = new GameObject("CardItemRoot", typeof(RectTransform));
        root.transform.SetParent(contentRect, false);
        return root;
    }

    private void StretchChildIfPossible(GameObject child)
    {
        if (child == null) return;
        var rect = child.GetComponent<RectTransform>();
        if (rect == null) return;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one;
    }

    private void ClearCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++) if (spawnedCards[i] != null) Destroy(spawnedCards[i]);
        spawnedCards.Clear();
    }

    private string GetResolvedTitle()
    {
        if (!string.IsNullOrEmpty(groupTitleOverride)) return groupTitleOverride;
        return groupType switch
        {
            CardGroupType.GiftCards => "过往国礼",
            CardGroupType.CultureElements => "传统文化意象",
            CardGroupType.TargetCountryElements => "异域文化意象",
            CardGroupType.Taboos => "文化禁忌",
            _ => "卡组"
        };
    }

    private static string FormatTitle(string title, bool verticalTitle)
    {
        if (!verticalTitle || string.IsNullOrEmpty(title)) return title;
        return string.Join("\n", title.ToCharArray());
    }
}
