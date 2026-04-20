using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 标签选择器UI
/// 负责显示政治目标和国家标签，供用户多选筛选
/// </summary>
public class LabelSelector : MonoBehaviour
{
    [Header("标签容器")]
    public Transform politicalGoalsContainer;
    public Transform countriesContainer;

    [Header("标签预制体")]
    public GameObject labelTogglePrefab;

    [Header("按钮")]
    public Button filterButton;
    public Button resetButton;

    [Header("提示文字")]
    public TextMeshProUGUI tipText;

    // 当前选中的政治目标
    private List<string> selectedGoals = new List<string>();
    // 当前选中的国家
    private List<string> selectedCountries = new List<string>();
    // 所有政治目标Toggle
    private List<Toggle> goalToggles = new List<Toggle>();
    // 所有国家Toggle
    private List<Toggle> countryToggles = new List<Toggle>();

    private void Start()
    {
        if (filterButton != null)
            filterButton.onClick.AddListener(OnFilterButtonClick);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClick);

        UpdateTipText();
    }

    /// <summary>
    /// 填充标签（由CardManager调用）
    /// </summary>
    public void PopulateLabels(List<string> goals, List<string> countries)
    {
        ClearLabels();

        // 生成政治目标标签
        foreach (var goal in goals)
            CreateLabelToggle(goal, politicalGoalsContainer, goalToggles, selectedGoals);

        // 生成国家标签
        foreach (var country in countries)
            CreateLabelToggle(country, countriesContainer, countryToggles, selectedCountries);

        Debug.Log($"✅ 标签加载完成：{goals.Count} 个政治目标，{countries.Count} 个国家");
    }

    /// <summary>
    /// 创建单个标签Toggle
    /// </summary>
    private void CreateLabelToggle(
        string label,
        Transform container,
        List<Toggle> toggleList,
        List<string> selectedList)
    {
        if (labelTogglePrefab == null || container == null) return;

        GameObject obj = Instantiate(labelTogglePrefab, container);

        // 设置标签文字
        var text = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = label;

        // 绑定 Toggle 事件
        var toggle = obj.GetComponent<Toggle>();
        if (toggle != null)
        {
            toggle.isOn = false;
            string labelCopy = label; // 避免闭包问题

            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    if (!selectedList.Contains(labelCopy))
                        selectedList.Add(labelCopy);
                }
                else
                {
                    selectedList.Remove(labelCopy);
                }
                UpdateTipText();
            });

            toggleList.Add(toggle);
        }
    }

    /// <summary>
    /// 点击筛选按钮
    /// </summary>
    private void OnFilterButtonClick()
    {
        if (selectedGoals.Count == 0 && selectedCountries.Count == 0)
        {
            Debug.LogWarning("⚠️ 请至少选择一个标签");
            if (tipText != null)
                tipText.text = "⚠️ 请至少选择一个标签！";
            return;
        }

        Debug.Log($"🔍 开始筛选：政治目标={string.Join(",", selectedGoals)}，国家={string.Join(",", selectedCountries)}");

        CardManager.Instance?.ApplyFilter(selectedCountries, selectedGoals);
    }

    /// <summary>
    /// 点击重置按钮
    /// </summary>
    private void OnResetButtonClick()
    {
        // 取消所有Toggle选中状态
        foreach (var toggle in goalToggles)
            if (toggle != null) toggle.isOn = false;

        foreach (var toggle in countryToggles)
            if (toggle != null) toggle.isOn = false;

        selectedGoals.Clear();
        selectedCountries.Clear();

        UpdateTipText();
        Debug.Log("🔄 标签已重置");
    }

    /// <summary>
    /// 清除所有标签
    /// </summary>
    private void ClearLabels()
    {
        // 清除政治目标标签
        foreach (var toggle in goalToggles)
            if (toggle != null) Destroy(toggle.gameObject);
        goalToggles.Clear();
        selectedGoals.Clear();

        // 清除国家标签
        foreach (var toggle in countryToggles)
            if (toggle != null) Destroy(toggle.gameObject);
        countryToggles.Clear();
        selectedCountries.Clear();
    }

    /// <summary>
    /// 更新提示文字
    /// </summary>
    private void UpdateTipText()
    {
        if (tipText == null) return;

        int total = selectedGoals.Count + selectedCountries.Count;
        if (total == 0)
            tipText.text = "请选择筛选条件";
        else
            tipText.text = $"已选择 {selectedGoals.Count} 个政治目标，{selectedCountries.Count} 个国家";
    }

    /// <summary>
    /// 获取当前选中的政治目标
    /// </summary>
    public List<string> GetSelectedGoals() => new List<string>(selectedGoals);

    /// <summary>
    /// 获取当前选中的国家
    /// </summary>
    public List<string> GetSelectedCountries() => new List<string>(selectedCountries);
}
