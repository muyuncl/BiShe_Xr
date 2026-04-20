using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
public class PoliticalOptionTagUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Image background;

    [Header("布局")]
    [SerializeField] private float preferredWidth = 145f;
    [SerializeField] private float preferredHeight = 36f;

    [Header("颜色")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.05f);
    [SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 0.2f);

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
    }

    public void Setup(string label, bool isSelected, Action onClick)
    {
        EnsureLayout();

        if (labelText != null)
            labelText.text = label;

        if (background != null)
            background.color = isSelected ? selectedColor : normalColor;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
        }
    }
}
