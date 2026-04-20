using System;
using System.Collections.Generic;
using UnityEngine;

public class PoliticalSelectorController : MonoBehaviour
{
    [SerializeField] private Transform optionsContent;
    [SerializeField] private PoliticalOptionTagUI optionPrefab;

    private readonly HashSet<string> selectedGoals = new HashSet<string>();
    private List<string> allGoals = new List<string>();

    public event Action OnSelectionChanged;

    public void Initialize(List<string> goals)
    {
        allGoals = goals ?? new List<string>();
        RefreshUI();
    }

    public List<string> GetSelectedGoals()
    {
        return new List<string>(selectedGoals);
    }

    public void ToggleGoal(string goal)
    {
        if (selectedGoals.Contains(goal))
            selectedGoals.Remove(goal);
        else
            selectedGoals.Add(goal);

        RefreshUI();
        OnSelectionChanged?.Invoke();
    }

    public void RemoveGoal(string goal)
    {
        if (!selectedGoals.Remove(goal)) return;
        RefreshUI();
        OnSelectionChanged?.Invoke();
    }

    public void ClearSelection()
    {
        selectedGoals.Clear();
        RefreshUI();
        OnSelectionChanged?.Invoke();
    }

    private void RefreshUI()
    {
        if (optionsContent == null || optionPrefab == null) return;

        for (int i = optionsContent.childCount - 1; i >= 0; i--)
            Destroy(optionsContent.GetChild(i).gameObject);

        foreach (var goal in allGoals)
        {
            var item = Instantiate(optionPrefab, optionsContent);
            item.Setup(goal, selectedGoals.Contains(goal), () => ToggleGoal(goal));
        }
    }
}
