using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景选择面板：用 ScrollRect 动态生成卡片，点击后调用 <see cref="EnvironmentRuntimeSwitcher.Apply"/>。
/// </summary>
[DisallowMultipleComponent]
public class EnvironmentSelectionPanelController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnvironmentRuntimeSwitcher switcher;

    [Tooltip("ScrollRect 的 Content（卡片挂在其下）。")]
    [SerializeField] private RectTransform contentRoot;

    [Tooltip("卡片预制体：需要至少包含 Button；可选 Image(缩略图) 与 TMP_Text(标题)。")]
    [SerializeField] private GameObject cardPrefab;

    [Header("Optional: selection visuals")]
    [SerializeField] private Color selectedTint = Color.white;
    [SerializeField] private Color normalTint = new Color(1f, 1f, 1f, 0.6f);

    private readonly List<GameObject> _spawnedCards = new();

    private void Start()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        if (contentRoot == null || cardPrefab == null || switcher == null)
        {
            Debug.LogWarning("[EnvironmentSelectionPanelController] 未绑定 contentRoot/cardPrefab/switcher。", this);
            return;
        }

        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            if (_spawnedCards[i] != null)
                Destroy(_spawnedCards[i]);
        }
        _spawnedCards.Clear();

        int count = switcher.Count;
        for (int i = 0; i < count; i++)
        {
            var entry = switcher.Get(i);
            var go = Instantiate(cardPrefab, contentRoot);
            go.name = $"EnvCard_{i}";
            _spawnedCards.Add(go);

            var btn = go.GetComponentInChildren<Button>(true);
            if (btn != null)
            {
                int idx = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[EnvironmentSelectionPanelController] 点击环境卡片 index={idx}");
                    switcher.Apply(idx);
                    RefreshSelection();
                });
            }

            var img = go.GetComponentInChildren<Image>(true);
            if (img != null && entry != null && entry.thumbnail != null)
                img.sprite = entry.thumbnail;

            var text = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.text = string.IsNullOrWhiteSpace(entry?.displayName) ? $"环境 {i + 1}" : entry.displayName;
        }

        RefreshSelection();
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            var go = _spawnedCards[i];
            if (go == null) continue;

            var selectable = go.GetComponentInChildren<Graphic>(true);
            if (selectable != null)
                selectable.color = (switcher.CurrentIndex == i) ? selectedTint : normalTint;
        }
    }
}

