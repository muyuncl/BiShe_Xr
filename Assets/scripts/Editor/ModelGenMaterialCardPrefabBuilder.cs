using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 材质卡片 UI 层级构建（126×126，预览 55×55，底部双行文案）。供预制体生成与场景生成器共用。
/// </summary>
public static class ModelGenMaterialCardPrefabBuilder
{
    public const float CardSize = 126f;
    public const float PreviewSize = 55f;
    public const string PrefabPath = "Assets/Prefabs/UI/ModelGenMaterialCard.prefab";

    /// <summary>
    /// 若工程内已有预制体则实例化到 parent；否则现场构建一份（与预制体结构一致）。
    /// </summary>
    public static GameObject InstantiateTemplate(Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.name = "MaterialCardTemplate";
            return inst;
        }

        Debug.LogWarning($"未找到预制体 {PrefabPath}，请菜单 Tools/UI/Create or Update ModelGen Material Card Prefab。本次使用运行时构建的卡片。");
        return BuildCard(parent);
    }

    public static GameObject BuildCard(Transform parent)
    {
        var root = NewRect("ModelGenMaterialCard", parent);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(CardSize, CardSize);
        root.anchoredPosition = Vector2.zero;

        var btn = root.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        var le = root.gameObject.AddComponent<LayoutElement>();
        le.minWidth = le.minHeight = CardSize;
        le.preferredWidth = le.preferredHeight = CardSize;
        le.flexibleWidth = le.flexibleHeight = 0f;

        var cardUi = root.gameObject.AddComponent<ModelGenMaterialCardUI>();

        var backgroundRt = NewRect("Background", root.transform);
        Stretch(backgroundRt, 0f, 0f, 0f, 0f);
        backgroundRt.SetAsFirstSibling();
        var bg = backgroundRt.gameObject.AddComponent<Image>();
        bg.color = new Color(0.35f, 0.35f, 0.38f, 0.92f);
        var outline = backgroundRt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.5f, 0.52f, 1f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;

        btn.targetGraphic = bg;

        var previewHolder = NewRect("PreviewHolder", root.transform);
        previewHolder.anchorMin = previewHolder.anchorMax = new Vector2(0.5f, 1f);
        previewHolder.pivot = new Vector2(0.5f, 1f);
        previewHolder.anchoredPosition = new Vector2(0f, -12f);
        previewHolder.sizeDelta = new Vector2(PreviewSize, PreviewSize);

        var preview = NewRect("Preview", previewHolder.transform);
        Stretch(preview, 0f, 0f, 0f, 0f);
        var prevImg = preview.gameObject.AddComponent<Image>();
        prevImg.color = Color.white;
        prevImg.raycastTarget = false;
        prevImg.preserveAspect = true;

        var bottom = NewRect("Bottom", root.transform);
        bottom.anchorMin = new Vector2(0f, 0f);
        bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.anchoredPosition = new Vector2(0f, 6f);
        bottom.sizeDelta = new Vector2(0f, 40f);

        var label = CreateTmp("Label", bottom.transform, "材质名称", 13f);
        label.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        var labelRt = label.rectTransform;
        labelRt.anchorMin = new Vector2(0f, 0.55f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        var subLabel = CreateTmp("SubLabel", bottom.transform, "Material Name", 9f);
        subLabel.color = new Color(0.72f, 0.72f, 0.72f, 1f);
        subLabel.alignment = TextAlignmentOptions.Center;
        subLabel.enableWordWrapping = false;
        subLabel.overflowMode = TextOverflowModes.Ellipsis;
        subLabel.raycastTarget = false;
        var subRt = subLabel.rectTransform;
        subRt.anchorMin = new Vector2(0f, 0f);
        subRt.anchorMax = new Vector2(1f, 0.5f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;

        using (var so = new SerializedObject(cardUi))
        {
            so.FindProperty("button").objectReferenceValue = btn;
            so.FindProperty("background").objectReferenceValue = bg;
            so.FindProperty("preview").objectReferenceValue = prevImg;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("subLabel").objectReferenceValue = subLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        return root.gameObject;
    }

    private static TextMeshProUGUI CreateTmp(string name, Transform parent, string text, float size)
    {
        var rt = NewRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = Color.white;
        return tmp;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }
}
