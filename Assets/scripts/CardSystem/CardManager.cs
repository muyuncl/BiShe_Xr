using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// 卡片管理器
/// 负责加载JSON数据、调用筛选、通知UI更新
/// </summary>
public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    [Header("数据配置")]
    [Tooltip("JSON文件路径（相对Resources，不含扩展名）")]
    public string jsonPath = "CardData/cards";

    [Header("组件引用")]
    public CardGroupContainer groupContainer;
    public CardGroupsDisplayController groupsDisplayController;
    public LabelSelector labelSelector;
    public FilterPanelController filterPanelController;

    // 原始数据库
    private CardDatabase database;
    // 筛选器
    private CardFilter filter = new CardFilter();
    // 当前筛选结果
    private CardGroupData currentResult;

    // 事件：筛选完成时通知
    public event System.Action<CardGroupData> OnFilterCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        LoadDatabase();
    }

    /// <summary>
    /// 加载卡片数据库
    /// </summary>
    public void LoadDatabase()
    {
        Debug.Log($"[CardManager] 开始加载，路径: Resources/{jsonPath}.json");
        Debug.Log($"[CardManager] labelSelector 是否为空: {labelSelector == null}");
        
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonPath);

        if (jsonFile == null)
        {
            Debug.LogError($"❌ 找不到卡片数据文件: Resources/{jsonPath}.json");
            return;
        }
        
        Debug.Log($"[CardManager] JSON 文件加载成功，长度: {jsonFile.text.Length} 字符");
        Debug.Log($"[CardManager] JSON 内容前100字: {jsonFile.text.Substring(0, Mathf.Min(100, jsonFile.text.Length))}");

        try
        {
            database = JsonConvert.DeserializeObject<CardDatabase>(jsonFile.text);
            Debug.Log($"✅ 卡片数据库加载成功");
            Debug.Log($"   国礼: {database.giftCards?.Count ?? 0} 张");
            Debug.Log($"   传统文化: {database.cultureElements?.Count ?? 0} 张");
            Debug.Log($"   目标国家: {database.targetCountryElements?.Count ?? 0} 张");
            Debug.Log($"   禁忌: {database.taboos?.Count ?? 0} 张");

            var goals = filter.GetAllPoliticalGoals(database);
            var countries = filter.GetAllCountries(database);
            Debug.Log($"[CardManager] 获取到政治目标: {goals.Count} 个: {string.Join(",", goals)}");
            Debug.Log($"[CardManager] 获取到国家: {countries.Count} 个: {string.Join(",", countries)}");

            if (filterPanelController != null)
            {
                Debug.Log("[CardManager] 初始化新的 FilterPanelController");
                filterPanelController.Initialize(goals);
            }
            else if (labelSelector != null)
            {
                Debug.Log("[CardManager] 调用旧版 PopulateLabels");
                labelSelector.PopulateLabels(goals, countries);
            }
            else
            {
                Debug.LogError("[CardManager] 未绑定筛选 UI，请在 Inspector 中拖入 FilterPanelController 或 LabelSelector！");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 解析卡片数据失败: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 执行筛选（由LabelSelector调用）
    /// </summary>
    public void ApplyFilter(List<string> selectedCountries, List<string> selectedGoals)
    {
        if (database == null)
        {
            Debug.LogError("❌ 数据库未加载，无法筛选");
            return;
        }

        Debug.Log($"🔍 开始筛选：国家={string.Join(",", selectedCountries)}，" +
                  $"政治目标={string.Join(",", selectedGoals)}");

        currentResult = filter.Filter(database, selectedCountries, selectedGoals);

        // 通知事件
        OnFilterCompleted?.Invoke(currentResult);

        // 优先使用新 WorldSpace 面板链路，避免与旧 CardGroupContainer 同时渲染
        if (groupsDisplayController != null)
        {
            groupsDisplayController.DisplayFilterResult(currentResult);
        }
        else if (groupContainer != null)
        {
            groupContainer.DisplayFilterResult(currentResult);
        }
    }

    /// <summary>
    /// 清空当前筛选结果及已显示卡片
    /// </summary>
    public void ClearDisplayedResult()
    {
        currentResult = null;
        OnFilterCompleted?.Invoke(null);

        // 与 ApplyFilter 保持一致：优先清空新链路
        if (groupsDisplayController != null)
        {
            groupsDisplayController.ClearAll();
        }
        else if (groupContainer != null)
        {
            groupContainer.CollapseAll();
        }
    }

    /// <summary>
    /// 加载卡片图片
    /// </summary>
    public Sprite LoadCardImage(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath)) return null;

        string normalizedPath = imagePath.Replace("\\", "/").Trim();
        string pathWithoutExtension = normalizedPath;
        if (!string.IsNullOrEmpty(Path.GetExtension(pathWithoutExtension)))
            pathWithoutExtension = pathWithoutExtension.Substring(0, pathWithoutExtension.LastIndexOf('.'));

        // 先尝试按 Sprite 直接加载（更贴近 UI 使用）
        Sprite sprite = Resources.Load<Sprite>(pathWithoutExtension);
        if (sprite != null)
            return sprite;

        // 再尝试 Texture2D，兼容旧资源导入配置
        Texture2D texture = Resources.Load<Texture2D>(pathWithoutExtension);
        if (texture == null)
        {
            Debug.LogWarning($"⚠️ 找不到图片: Resources/{normalizedPath}（已尝试去扩展名路径：{pathWithoutExtension}）");
            return null;
        }

        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }

    /// <summary>
    /// 获取当前筛选结果
    /// </summary>
    public CardGroupData GetCurrentResult() => currentResult;

    /// <summary>
    /// 获取数据库
    /// </summary>
    public CardDatabase GetDatabase() => database;
}
