using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModelGenMaterialCardUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image preview;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI subLabel;

    [SerializeField] private Color selectedBackgroundColor = Color.black;

    public event Action<ModelGenMaterialCardUI> Clicked;

    private Color _normalBackgroundColor;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (background == null)
            background = transform.Find("Background")?.GetComponent<Image>();
        if (preview == null)
            preview = transform.Find("PreviewHolder/Preview")?.GetComponent<Image>();
        if (label == null)
            label = transform.Find("Bottom/Label")?.GetComponent<TextMeshProUGUI>();
        if (subLabel == null)
            subLabel = transform.Find("Bottom/SubLabel")?.GetComponent<TextMeshProUGUI>();

        if (background != null)
            _normalBackgroundColor = background.color;

        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            if (background != null)
                button.targetGraphic = background;
            button.onClick.AddListener(() => Clicked?.Invoke(this));
        }
    }

    public void SetData(Sprite image, string displayName, string englishName, Color previewTint)
    {
        if (label != null)
            label.text = displayName;
        if (subLabel != null)
            subLabel.text = string.IsNullOrWhiteSpace(englishName) ? "Material Name" : englishName;
        if (preview != null)
        {
            if (image != null)
            {
                preview.sprite = image;
                preview.color = Color.white;
            }
            else
            {
                preview.sprite = null;
                preview.color = previewTint;
            }
        }
    }

    public void SetSelected(bool value)
    {
        if (background == null)
            return;
        background.color = value ? selectedBackgroundColor : _normalBackgroundColor;
    }
}
