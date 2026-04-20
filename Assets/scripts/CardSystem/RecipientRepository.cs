using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class RecipientRepository : MonoBehaviour
{
    [SerializeField] private string recipientsJsonPath = "CardData/recipients";

    private RecipientNodeCollection data;
    private readonly Dictionary<string, RecipientNodeData> nodesById = new Dictionary<string, RecipientNodeData>();
    private readonly Dictionary<string, List<RecipientNodeData>> childrenByParentId = new Dictionary<string, List<RecipientNodeData>>();

    public void Load()
    {
        nodesById.Clear();
        childrenByParentId.Clear();

        TextAsset json = Resources.Load<TextAsset>(recipientsJsonPath);
        if (json == null)
        {
            Debug.LogError($"❌ 找不到 recipient 配置: Resources/{recipientsJsonPath}.json");
            return;
        }

        data = JsonConvert.DeserializeObject<RecipientNodeCollection>(json.text);
        if (data?.nodes == null)
        {
            Debug.LogError("❌ recipient 配置解析失败或为空");
            return;
        }

        foreach (var node in data.nodes)
        {
            nodesById[node.id] = node;
            var parentId = string.IsNullOrEmpty(node.parentId) ? "ROOT" : node.parentId;
            if (!childrenByParentId.ContainsKey(parentId))
                childrenByParentId[parentId] = new List<RecipientNodeData>();
            childrenByParentId[parentId].Add(node);
        }
    }

    public List<RecipientNodeData> GetRootNodes()
    {
        return GetChildren("ROOT");
    }

    public List<RecipientNodeData> GetChildren(string parentId)
    {
        if (string.IsNullOrEmpty(parentId))
            parentId = "ROOT";

        return childrenByParentId.TryGetValue(parentId, out var children)
            ? new List<RecipientNodeData>(children)
            : new List<RecipientNodeData>();
    }

    public RecipientNodeData GetNode(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        nodesById.TryGetValue(id, out var node);
        return node;
    }

    public List<string> ExpandRecipients(IEnumerable<string> selectedNodeIds)
    {
        HashSet<string> result = new HashSet<string>();
        if (selectedNodeIds == null) return new List<string>();

        foreach (var id in selectedNodeIds)
        {
            var node = GetNode(id);
            if (node?.coveredRecipients == null) continue;
            foreach (var recipient in node.coveredRecipients)
                result.Add(recipient);
        }

        return new List<string>(result);
    }
}
