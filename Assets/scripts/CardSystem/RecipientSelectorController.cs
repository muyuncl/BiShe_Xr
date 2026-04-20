using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipientSelectorController : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private RecipientRepository repository;

    [Header("列容器")]
    [SerializeField] private GameObject column1Root;
    [SerializeField] private Transform column1Content;
    [SerializeField] private GameObject column2Root;
    [SerializeField] private Transform column2Content;
    [SerializeField] private GameObject column3Root;
    [SerializeField] private Transform column3Content;

    [Header("预制体")]
    [SerializeField] private RecipientOptionItemUI optionItemPrefab;

    private readonly HashSet<string> selectedNodeIds = new HashSet<string>();
    private RecipientNodeData activeLevel1;
    private RecipientNodeData activeLevel2;

    public event Action OnSelectionChanged;

    public void Initialize(RecipientRepository recipientRepository)
    {
        repository = recipientRepository;
        RefreshUI();
    }

    public List<string> GetSelectedNodeIds()
    {
        return new List<string>(selectedNodeIds);
    }

    public List<string> GetSelectedLabels()
    {
        var list = new List<string>();
        foreach (var id in selectedNodeIds)
        {
            var node = repository.GetNode(id);
            if (node != null) list.Add(node.label);
        }
        return list;
    }

    public void ToggleSelection(string nodeId)
    {
        if (selectedNodeIds.Contains(nodeId))
            selectedNodeIds.Remove(nodeId);
        else
            selectedNodeIds.Add(nodeId);

        RefreshUI();
        OnSelectionChanged?.Invoke();
    }

    public void RemoveSelection(string nodeId)
    {
        if (!selectedNodeIds.Remove(nodeId)) return;
        RefreshUI();
        OnSelectionChanged?.Invoke();
    }

    public void ClearSelection()
    {
        selectedNodeIds.Clear();
        activeLevel1 = null;
        activeLevel2 = null;
        RefreshUI();
        OnSelectionChanged?.Invoke();
    }

    private void RefreshUI()
    {
        RebuildColumn(
            column1Content,
            repository.GetRootNodes(),
            node => activeLevel1 != null && activeLevel1.id == node.id,
            node =>
            {
                activeLevel1 = node;
                activeLevel2 = null;
                RefreshUI();
            });

        var level2Nodes = activeLevel1 != null ? repository.GetChildren(activeLevel1.id) : new List<RecipientNodeData>();
        SetColumnVisible(column2Root, level2Nodes.Count > 0);
        if (level2Nodes.Count > 0)
        {
            RebuildColumn(
                column2Content,
                level2Nodes,
                node => activeLevel2 != null && activeLevel2.id == node.id,
                node =>
                {
                    activeLevel2 = node;
                    RefreshUI();
                });
        }
        else
        {
            ClearChildren(column2Content);
        }

        var level3Nodes = activeLevel2 != null ? repository.GetChildren(activeLevel2.id) : new List<RecipientNodeData>();
        SetColumnVisible(column3Root, level3Nodes.Count > 0);
        if (level3Nodes.Count > 0)
        {
            RebuildColumn(
                column3Content,
                level3Nodes,
                _ => false,
                _ => { });
        }
        else
        {
            ClearChildren(column3Content);
        }

        SetColumnVisible(column1Root, true);
    }

    private void RebuildColumn(
        Transform content,
        List<RecipientNodeData> nodes,
        Func<RecipientNodeData, bool> isActive,
        Action<RecipientNodeData> onNavigate)
    {
        ClearChildren(content);

        foreach (var node in nodes)
        {
            var item = Instantiate(optionItemPrefab, content);
            var hasChildren = repository.GetChildren(node.id).Count > 0;
            item.Setup(
                node.label,
                node.selectable,
                hasChildren,
                selectedNodeIds.Contains(node.id),
                isActive(node),
                () => ToggleSelection(node.id),
                () => onNavigate(node));
        }
    }

    private void ClearChildren(Transform root)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private void SetColumnVisible(GameObject columnRoot, bool visible)
    {
        if (columnRoot != null)
            columnRoot.SetActive(visible);
    }
}
