using System.Collections.Generic;
using UnityEngine;

public class SelectedConditionsController : MonoBehaviour
{
    [SerializeField] private Transform recipientTagsContent;
    [SerializeField] private Transform politicalTagsContent;
    [SerializeField] private SelectedConditionTagUI tagPrefab;

    private RecipientSelectorController recipientSelector;
    private PoliticalSelectorController politicalSelector;

    public void Initialize(RecipientSelectorController recipient, PoliticalSelectorController political)
    {
        recipientSelector = recipient;
        politicalSelector = political;

        if (recipientSelector != null)
            recipientSelector.OnSelectionChanged += Refresh;
        if (politicalSelector != null)
            politicalSelector.OnSelectionChanged += Refresh;

        Refresh();
    }

    public void Refresh()
    {
        RebuildRecipientTags();
        RebuildPoliticalTags();
    }

    private void RebuildRecipientTags()
    {
        ClearChildren(recipientTagsContent);
        if (recipientSelector == null || tagPrefab == null) return;

        var ids = recipientSelector.GetSelectedNodeIds();
        var labels = recipientSelector.GetSelectedLabels();
        for (int i = 0; i < Mathf.Min(ids.Count, labels.Count); i++)
        {
            string idCopy = ids[i];
            string labelCopy = labels[i];
            var item = Instantiate(tagPrefab, recipientTagsContent);
            item.Setup(labelCopy, () => recipientSelector.RemoveSelection(idCopy));
        }
    }

    private void RebuildPoliticalTags()
    {
        ClearChildren(politicalTagsContent);
        if (politicalSelector == null || tagPrefab == null) return;

        foreach (var goal in politicalSelector.GetSelectedGoals())
        {
            string goalCopy = goal;
            var item = Instantiate(tagPrefab, politicalTagsContent);
            item.Setup(goalCopy, () => politicalSelector.RemoveGoal(goalCopy));
        }
    }

    private void ClearChildren(Transform root)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}
