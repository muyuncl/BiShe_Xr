using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
public class SelectedConditionTagUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Button removeButton;

    [Header("布局")]
    [SerializeField] private float preferredWidth = 110f;
    [SerializeField] private float preferredHeight = 34f;

    private void Awake()
    {
        var layout = GetComponent<LayoutElement>();
        layout.preferredWidth = preferredWidth;
        layout.preferredHeight = preferredHeight;
        layout.minHeight = preferredHeight;
        layout.flexibleHeight = 0f;
        layout.flexibleWidth = 0f;
    }

    public void Setup(string label, Action onRemove)
    {
        if (labelText != null)
            labelText.text = label;

        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            if (onRemove != null)
                removeButton.onClick.AddListener(() => onRemove());
        }
    }
}
