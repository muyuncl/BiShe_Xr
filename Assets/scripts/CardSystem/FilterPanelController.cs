using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public void Initialize(List<string> politicalGoals)
    {
        if (recipientRepository != null)
            recipientRepository.Load();

        if (recipientSelector != null && recipientRepository != null)
            recipientSelector.Initialize(recipientRepository);

        if (politicalSelector != null)
            politicalSelector.Initialize(politicalGoals);

        if (selectedConditions != null)
            selectedConditions.Initialize(recipientSelector, politicalSelector);

        if (recipientFoldButton != null)
            recipientFoldButton.onClick.AddListener(() => TogglePanel("recipient"));
        if (politicalFoldButton != null)
            politicalFoldButton.onClick.AddListener(() => TogglePanel("political"));

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
