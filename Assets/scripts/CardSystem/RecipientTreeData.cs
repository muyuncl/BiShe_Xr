using System;
using System.Collections.Generic;

[Serializable]
public class RecipientNodeData
{
    public string id;
    public string label;
    public string parentId;
    public int level;
    public bool selectable;
    public string nodeType;
    public List<string> coveredRecipients;
}

[Serializable]
public class RecipientNodeCollection
{
    public List<RecipientNodeData> nodes;
}
