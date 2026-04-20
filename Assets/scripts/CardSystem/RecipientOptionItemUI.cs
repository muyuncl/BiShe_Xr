using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
public class RecipientOptionItemUI : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Button navigateButton;
    [SerializeField] private Button selectButton;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI selectButtonText;
    [SerializeField] private GameObject arrowObject;
    [SerializeField] private GameObject selectedMarkObject;
    [SerializeField] private Image background;

    [Header("布局")]
    [SerializeField] private float preferredWidth = 140f;
    [SerializeField] private float preferredHeight = 36f;

    [Header("颜色")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color activeColor = new Color(1f, 1f, 1f, 0.16f);
    [SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 0.22f);

    private void Reset()
    {
        EnsureLayout();
    }

    private void Awake()
    {
        EnsureLayout();
    }

    private void EnsureLayout()
    {
        var layout = GetComponent<LayoutElement>();
        layout.preferredWidth = preferredWidth;
        layout.preferredHeight = preferredHeight;
        layout.minHeight = preferredHeight;
        layout.flexibleHeight = 0f;
        layout.flexibleWidth = 0f;

        var rect = transform as RectTransform;
        if (rect != null)
            rect.sizeDelta = new Vector2(preferredWidth, preferredHeight);
    }

    public void Setup(
        string displayName,
        bool selectable,
        bool hasChildren,
        bool isSelected,
        bool isActiveBranch,
        Action onSelect,
        Action onNavigate)
    {
        EnsureLayout();

        if (labelText != null)
            labelText.text = displayName;

        if (selectButton != null)
        {
            selectButton.gameObject.SetActive(selectable);
            selectButton.onClick.RemoveAllListeners();
            if (selectable && onSelect != null)
                selectButton.onClick.AddListener(() => onSelect());
        }

        if (selectButtonText != null)
            selectButtonText.text = isSelected ? "已选" : "选择";

        if (navigateButton != null)
        {
            navigateButton.interactable = hasChildren;
            navigateButton.onClick.RemoveAllListeners();
            if (hasChildren && onNavigate != null)
                navigateButton.onClick.AddListener(() => onNavigate());
        }

        if (arrowObject != null)
            arrowObject.SetActive(hasChildren);

        if (selectedMarkObject != null)
            selectedMarkObject.SetActive(isSelected);

        if (background != null)
        {
            background.color = isSelected
                ? selectedColor
                : isActiveBranch ? activeColor : normalColor;
        }
    }
}
