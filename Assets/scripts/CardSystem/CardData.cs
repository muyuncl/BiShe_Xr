using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 禁忌信息
/// </summary>
[Serializable]
public class TabooInfo
{
    public string country;   // 禁忌国家
    public string reason;    // 禁忌原因
}

/// <summary>
/// 单张卡片数据
/// </summary>
[Serializable]
public class CardData
{
    public string id;                        // 唯一ID
    public string name;                      // 卡片名称
    public string description;               // 描述
    public string image;                     // 图片路径（相对Resources）
    public List<string> politicalGoals;      // 政治目标标签
    public List<TabooInfo> taboos;           // 禁忌信息
    public string targetCountry;             // 对应国家（仅 targetCountryElements 类型使用）

    /// <summary>
    /// 检查该卡片在指定国家是否有禁忌
    /// </summary>
    public bool HasTabooForCountry(string country)
    {
        if (taboos == null || taboos.Count == 0) return false;
        foreach (var taboo in taboos)
            if (taboo.country == country) return true;
        return false;
    }

    /// <summary>
    /// 检查该卡片在任意一个指定国家是否有禁忌
    /// </summary>
    public bool HasTabooForAnyCountry(List<string> countries)
    {
        if (countries == null || countries.Count == 0) return false;
        foreach (var country in countries)
            if (HasTabooForCountry(country)) return true;
        return false;
    }

    /// <summary>
    /// 检查是否匹配任意一个政治目标
    /// </summary>
    public bool MatchesAnyPoliticalGoal(List<string> goals)
    {
        if (goals == null || goals.Count == 0) return true;
        if (politicalGoals == null || politicalGoals.Count == 0) return false;
        foreach (var goal in goals)
            foreach (var myGoal in politicalGoals)
                if (myGoal == goal) return true;
        return false;
    }

    /// <summary>
    /// 获取指定国家的禁忌原因
    /// </summary>
    public string GetTabooReason(string country)
    {
        if (taboos == null) return "";
        foreach (var taboo in taboos)
            if (taboo.country == country) return taboo.reason;
        return "";
    }
}

/// <summary>
/// 卡片组数据
/// </summary>
[Serializable]
public class CardGroupData
{
    public List<CardData> giftCards;               // 国礼卡组
    public List<CardData> cultureElements;         // 传统文化元素卡组
    public List<CardData> targetCountryElements;   // 目标国家文化元素卡组
    public List<CardData> taboos;                  // 文化禁忌卡组

    public CardGroupData()
    {
        giftCards = new List<CardData>();
        cultureElements = new List<CardData>();
        targetCountryElements = new List<CardData>();
        taboos = new List<CardData>();
    }
}

/// <summary>
/// 完整的卡片数据库（对应 JSON 根节点）
/// </summary>
[Serializable]
public class CardDatabase
{
    public List<CardData> giftCards;
    public List<CardData> cultureElements;
    public List<CardData> targetCountryElements;
    public List<CardData> taboos;
}

/// <summary>
/// 卡片组类型枚举
/// </summary>
public enum CardGroupType
{
    GiftCards,              // 国礼卡组
    CultureElements,        // 传统文化元素卡组
    TargetCountryElements,  // 目标国家文化元素卡组
    Taboos                  // 文化禁忌卡组
}
