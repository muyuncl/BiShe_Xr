using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡片筛选逻辑
/// 根据用户选择的政治目标和国家，筛选并分类卡片
///
/// 禁忌卡组来源：
///   1. 推荐卡片（giftCards/cultureElements）在选中国家有禁忌 → 移入禁忌组
///   2. 本身就是某国禁忌元素（taboos类型）→ 匹配选中国家则显示在禁忌组
///
/// targetCountryElements：
///   只显示 targetCountry 匹配选中国家的卡片
/// </summary>
public class CardFilter
{
    /// <summary>
    /// 执行筛选
    /// </summary>
    public CardGroupData Filter(
        CardDatabase database,
        List<string> selectedCountries,
        List<string> selectedPoliticalGoals)
    {
        CardGroupData result = new CardGroupData();

        if (database == null)
        {
            Debug.LogError("❌ CardFilter: 数据库为空！");
            return result;
        }

        // ① 筛选国礼卡组
        // 政治目标匹配 + 无禁忌 → 推荐；有禁忌 → 禁忌组
        FilterRecommendGroup(
            database.giftCards,
            result.giftCards,
            result.taboos,
            selectedCountries,
            selectedPoliticalGoals,
            "国礼卡组"
        );

        // ② 筛选传统文化元素卡组
        FilterRecommendGroup(
            database.cultureElements,
            result.cultureElements,
            result.taboos,
            selectedCountries,
            selectedPoliticalGoals,
            "传统文化元素卡组"
        );

        // ③ 筛选目标国家文化元素卡组
        // 只显示 targetCountry 匹配选中国家的卡片
        FilterTargetCountryGroup(
            database.targetCountryElements,
            result.targetCountryElements,
            selectedCountries
        );

        // ④ 筛选禁忌元素卡组（本身就是某国禁忌）
        // targetCountry 匹配选中国家 → 显示在禁忌组
        FilterNativeTabooGroup(
            database.taboos,
            result.taboos,
            selectedCountries
        );

        Debug.Log($"✅ 筛选完成：" +
            $"国礼={result.giftCards.Count}张，" +
            $"传统文化={result.cultureElements.Count}张，" +
            $"目标国家={result.targetCountryElements.Count}张，" +
            $"禁忌={result.taboos.Count}张");

        return result;
    }

    /// <summary>
    /// 筛选推荐组（giftCards / cultureElements）
    /// 政治目标匹配：
    ///   - 在选中国家有禁忌 → 移入禁忌组
    ///   - 无禁忌 → 保留在推荐组
    /// </summary>
    private void FilterRecommendGroup(
        List<CardData> source,
        List<CardData> recommendTarget,
        List<CardData> tabooTarget,
        List<string> selectedCountries,
        List<string> selectedPoliticalGoals,
        string groupName)
    {
        if (source == null) return;

        foreach (var card in source)
        {
            // 检查政治目标是否匹配
            if (!card.MatchesAnyPoliticalGoal(selectedPoliticalGoals))
                continue;

            // 检查是否在选中国家有禁忌
            if (card.HasTabooForAnyCountry(selectedCountries))
            {
                tabooTarget.Add(card);
                Debug.Log($"  ⚠️ [{groupName}] '{card.name}' 在选中国家有禁忌 → 移入禁忌组");
            }
            else
            {
                recommendTarget.Add(card);
            }
        }
    }

    /// <summary>
    /// 筛选目标国家文化元素卡组
    /// 只显示 targetCountry 包含在 selectedCountries 中的卡片
    /// </summary>
    private void FilterTargetCountryGroup(
        List<CardData> source,
        List<CardData> target,
        List<string> selectedCountries)
    {
        if (source == null) return;

        foreach (var card in source)
        {
            // targetCountry 必须匹配选中的国家
            if (!string.IsNullOrEmpty(card.targetCountry) &&
                selectedCountries.Contains(card.targetCountry))
            {
                target.Add(card);
            }
        }
    }

    /// <summary>
    /// 筛选本身就是禁忌的元素（taboos类型）
    /// 禁忌国家字段匹配选中国家 → 显示在禁忌组
    /// </summary>
    private void FilterNativeTabooGroup(
        List<CardData> source,
        List<CardData> tabooTarget,
        List<string> selectedCountries)
    {
        if (source == null) return;

        foreach (var card in source)
        {
            // 用禁忌国家字段匹配选中国家
            if (card.HasTabooForAnyCountry(selectedCountries))
            {
                tabooTarget.Add(card);
                Debug.Log($"  ⚠️ [禁忌元素] '{card.name}' 是选中国家的禁忌元素 → 加入禁忌组");
            }
        }
    }

    /// <summary>
    /// 获取所有可用的政治目标标签
    /// </summary>
    public List<string> GetAllPoliticalGoals(CardDatabase database)
    {
        HashSet<string> goals = new HashSet<string>();
        CollectGoals(database.giftCards, goals);
        CollectGoals(database.cultureElements, goals);
        return new List<string>(goals);
    }

    /// <summary>
    /// 获取所有涉及的国家
    /// 来源：推荐卡的禁忌国家 + targetCountryElements 的 targetCountry + taboos 的 targetCountry
    /// </summary>
    public List<string> GetAllCountries(CardDatabase database)
    {
        HashSet<string> countries = new HashSet<string>();

        // 从推荐卡片的禁忌信息中收集国家
        CollectTabooCountries(database.giftCards, countries);
        CollectTabooCountries(database.cultureElements, countries);

        // 从目标国家卡组的 targetCountry 收集
        CollectTargetCountries(database.targetCountryElements, countries);

        // 从禁忌元素的禁忌国家字段收集
        CollectTabooCountries(database.taboos, countries);

        return new List<string>(countries);
    }

    private void CollectGoals(List<CardData> cards, HashSet<string> goals)
    {
        if (cards == null) return;
        foreach (var card in cards)
            if (card.politicalGoals != null)
                foreach (var goal in card.politicalGoals)
                    goals.Add(goal);
    }

    private void CollectTabooCountries(List<CardData> cards, HashSet<string> countries)
    {
        if (cards == null) return;
        foreach (var card in cards)
            if (card.taboos != null)
                foreach (var taboo in card.taboos)
                    countries.Add(taboo.country);
    }

    private void CollectTargetCountries(List<CardData> cards, HashSet<string> countries)
    {
        if (cards == null) return;
        foreach (var card in cards)
            if (!string.IsNullOrEmpty(card.targetCountry))
                countries.Add(card.targetCountry);
    }
}
