using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 折叠按钮的显隐只应由本脚本的 <see cref="TogglePanel"/> / <see cref="RefreshPanels"/> 控制。
/// 请勿在 Inspector 里给「赠送对象 / 政治目标」折叠按钮再绑 GameObject.SetActive，否则会与代码监听冲突（例如政治下拉被强制设为 false）。
/// </summary>
public class FilterPanelController : MonoBehaviour
{
    [Header("折叠按钮")]
    [SerializeField] private Button recipientFoldButton;
    [SerializeField] private Button politicalFoldButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button applyButton;

    [Header("下拉面板")]
    [SerializeField] private GameObject recipientDropdown;
    [SerializeField] private GameObject politicalDropdown;

    [Header("依赖")]
    [SerializeField] private RecipientRepository recipientRepository;
    [SerializeField] private RecipientSelectorController recipientSelector;
    [SerializeField] private PoliticalSelectorController politicalSelector;
    [SerializeField] private SelectedConditionsController selectedConditions;
    [SerializeField] private CardManager cardManager;

    private string openedPanel = string.Empty;
    private bool _foldListenersAdded;

    private void Awake()
    {
        EnsureFoldButtonListeners();
    }

    /// <summary>
    /// 折叠按钮必须在 Awake 就挂上：原先只在 Initialize 里挂，而 Initialize 要等 CardManager 成功加载 JSON 才会调，
    /// 若未绑定 CardManager、JSON 失败或时序不对，两个按钮会一直点不出下拉。
    /// </summary>
    private void EnsureFoldButtonListeners()
    {
        if (_foldListenersAdded)
            return;
        if (recipientFoldButton == null && politicalFoldButton == null)
            return;

        if (recipientFoldButton != null)
            recipientFoldButton.onClick.AddListener(() => TogglePanel("recipient"));
        if (politicalFoldButton != null)
            politicalFoldButton.onClick.AddListener(() => TogglePanel("political"));

        _foldListenersAdded = true;
    }

    public void Initialize(List<string> politicalGoals)
    {
        EnsureFoldButtonListeners();

        if (recipientRepository != null)
            recipientRepository.Load();

        if (recipientSelector != null && recipientRepository != null)
            recipientSelector.Initialize(recipientRepository);

        if (politicalSelector != null)
            politicalSelector.Initialize(politicalGoals);

        if (selectedConditions != null)
            selectedConditions.Initialize(recipientSelector, politicalSelector);

        RefreshPanels();
    }

    public void TogglePanel(string panelName)
    {
        openedPanel = openedPanel == panelName ? string.Empty : panelName;
        RefreshPanels();
    }

    public void ResetFilter()
    {
        openedPanel = string.Empty;
        recipientSelector?.ClearSelection();
        politicalSelector?.ClearSelection();
        selectedConditions?.Refresh();
        cardManager?.ClearDisplayedResult();
        RefreshPanels();
    }

    public void ApplyFilter()
    {
        if (cardManager == null || recipientSelector == null || politicalSelector == null || recipientRepository == null)
            return;

        var selectedNodeIds = recipientSelector.GetSelectedNodeIds();
        var expandedRecipients = recipientRepository.ExpandRecipients(selectedNodeIds);
        var selectedGoals = politicalSelector.GetSelectedGoals();
        cardManager.ApplyFilter(expandedRecipients, selectedGoals);
    }

    private void RefreshPanels()
    {
        if (recipientDropdown != null)
            recipientDropdown.SetActive(openedPanel == "recipient");
        if (politicalDropdown != null)
            politicalDropdown.SetActive(openedPanel == "political");
    }
}
