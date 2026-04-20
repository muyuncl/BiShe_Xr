using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CardUIPrefabSchemeGenerator
{
    private const string PrefabFolder = "Assets/Prefabs/Card Prefabs/NewUI";

    [MenuItem("Tools/UI/Generate New 5x8 Card Prefabs")]
    public static void GenerateNewCardPrefabs()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Card Prefabs");
        EnsureFolder(PrefabFolder);

        var gift = CreateCardPrefab("giftCardsPrefab_NewUI", new Color(0.93f, 0.87f, 0.72f, 1f));
        var culture = CreateCardPrefab("cultureElementsCardsPrefab_NewUI", new Color(0.84f, 0.92f, 0.86f, 1f));
        var target = CreateCardPrefab("targetCountryElementsCardsPrefab_NewUI", new Color(0.84f, 0.89f, 0.95f, 1f));
        var taboo = CreateCardPrefab("taboosCardsPrefab_NewUI", new Color(0.95f, 0.84f, 0.84f, 1f));

        TryAssignToPanelViews(gift, culture, target, taboo);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ 新 5:8 卡片 UI prefab 已生成并尝试自动绑定到 CardGroupPanelView。");
    }

    private static GameObject CreateCardPrefab(string prefabName, Color bgColor)
    {
        // 基准尺寸保持 5:8，实际显示尺寸由 CardGroupPanelView 的布局控制
        var root = new GameObject(prefabName, typeof(RectTransform), typeof(Image), typeof(CardUI));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(500f, 800f);
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.localScale = Vector3.one;

        var rootImage = root.GetComponent<Image>();
        rootImage.color = bgColor;
        rootImage.raycastTarget = true;

        var imageGo = new GameObject("CardImage", typeof(RectTransform), typeof(Image));
        imageGo.transform.SetParent(root.transform, false);
        var imageRect = imageGo.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.08f, 0.44f);
        imageRect.anchorMax = new Vector2(0.92f, 0.90f);
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        var image = imageGo.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 1f);
        image.preserveAspect = true;

        var titleGo = CreateTMP("TitleText", root.transform, 34f, FontStyles.Bold);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.08f, 0.34f);
        titleRect.anchorMax = new Vector2(0.92f, 0.42f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleGo.GetComponent<TextMeshProUGUI>();
        titleText.text = "卡片标题";
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.enableWordWrapping = false;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 10f;
        titleText.fontSizeMax = 30f;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        titleText.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        var descGo = CreateTMP("DescriptionText", root.transform, 24f, FontStyles.Normal);
        var descRect = descGo.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.08f, 0.08f);
        descRect.anchorMax = new Vector2(0.92f, 0.32f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        var descText = descGo.GetComponent<TextMeshProUGUI>();
        descText.text = "卡片描述";
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.enableWordWrapping = true;
        descText.enableAutoSizing = true;
        descText.fontSizeMin = 8f;
        descText.fontSizeMax = 20f;
        descText.overflowMode = TextOverflowModes.Ellipsis;
        descText.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var cardUI = root.GetComponent<CardUI>();
        cardUI.cardImage = image;
        cardUI.cardNameText = titleText;
        cardUI.descriptionText = descText;

        string savePath = Path.Combine(PrefabFolder, $"{prefabName}.prefab").Replace("\\", "/");
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, savePath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateTMP(string name, Transform parent, float fontSize, FontStyles fontStyles)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyles;
        tmp.raycastTarget = false;
        return go;
    }

    private static void TryAssignToPanelViews(GameObject gift, GameObject culture, GameObject target, GameObject taboo)
    {
        var panels = Object.FindObjectsOfType<CardGroupPanelView>(true);
        int assignedCount = 0;
        foreach (var panel in panels)
        {
            if (panel == null) continue;
            var prefab = ResolvePrefab(panel.GroupType, gift, culture, target, taboo);
            if (prefab == null) continue;

            var field = typeof(CardGroupPanelView).GetField("cardPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) continue;

            field.SetValue(panel, prefab);
            EditorUtility.SetDirty(panel);
            assignedCount++;
        }

        if (assignedCount > 0)
            Debug.Log($"[CardUIPrefabSchemeGenerator] 已自动绑定 {assignedCount} 个 CardGroupPanelView.cardPrefab");
        else
            Debug.LogWarning("[CardUIPrefabSchemeGenerator] 未找到可绑定的 CardGroupPanelView，请在场景中手动拖拽绑定。");
    }

    private static GameObject ResolvePrefab(CardGroupType groupType, GameObject gift, GameObject culture, GameObject target, GameObject taboo)
    {
        switch (groupType)
        {
            case CardGroupType.GiftCards:
                return gift;
            case CardGroupType.CultureElements:
                return culture;
            case CardGroupType.TargetCountryElements:
                return target;
            case CardGroupType.Taboos:
                return taboo;
            default:
                return gift;
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        var parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        var name = Path.GetFileName(folderPath);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;
        AssetDatabase.CreateFolder(parent, name);
    }
}
