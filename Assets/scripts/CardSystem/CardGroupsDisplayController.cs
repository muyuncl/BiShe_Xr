using UnityEngine;

public class CardGroupsDisplayController : MonoBehaviour
{
    [Header("四个卡组面板")]
    [SerializeField] private CardGroupPanelView taboosPanel;
    [SerializeField] private CardGroupPanelView cultureElementsPanel;
    [SerializeField] private CardGroupPanelView targetCountryElementsPanel;
    [SerializeField] private CardGroupPanelView giftCardsPanel;

    [Header("调试")]
    [SerializeField] private bool displayCurrentResultOnStart;

    private void Start()
    {
        ApplyPanelTypes();

        if (displayCurrentResultOnStart && CardManager.Instance != null)
            DisplayFilterResult(CardManager.Instance.GetCurrentResult());
    }

    private void OnValidate()
    {
        ApplyPanelTypes();
    }

    public void DisplayFilterResult(CardGroupData result)
    {
        ApplyPanelTypes();

        if (result == null)
        {
            ClearAll();
            return;
        }

        if (taboosPanel != null)
            taboosPanel.Display(result.taboos);

        if (cultureElementsPanel != null)
            cultureElementsPanel.Display(result.cultureElements);

        if (targetCountryElementsPanel != null)
            targetCountryElementsPanel.Display(result.targetCountryElements);

        if (giftCardsPanel != null)
            giftCardsPanel.Display(result.giftCards);
    }

    public void ClearAll()
    {
        if (taboosPanel != null)
            taboosPanel.Display(null);

        if (cultureElementsPanel != null)
            cultureElementsPanel.Display(null);

        if (targetCountryElementsPanel != null)
            targetCountryElementsPanel.Display(null);

        if (giftCardsPanel != null)
            giftCardsPanel.Display(null);
    }

    private void ApplyPanelTypes()
    {
        if (taboosPanel != null)
            taboosPanel.SetGroupType(CardGroupType.Taboos);

        if (cultureElementsPanel != null)
            cultureElementsPanel.SetGroupType(CardGroupType.CultureElements);

        if (targetCountryElementsPanel != null)
            targetCountryElementsPanel.SetGroupType(CardGroupType.TargetCountryElements);

        if (giftCardsPanel != null)
            giftCardsPanel.SetGroupType(CardGroupType.GiftCards);
    }
}
