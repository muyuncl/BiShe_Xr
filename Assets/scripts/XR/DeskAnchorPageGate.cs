using UnityEngine;

/// <summary>
/// 流程门禁：只有桌面锚点锁定后才允许进入下一页。
/// 将此组件挂在“桌面锚定页面”根节点（或其子物体）上。
/// </summary>
public class DeskAnchorPageGate : MonoBehaviour, IUIFlowPageGate
{
    [SerializeField] private DeskAnchorLockToggle lockToggle;
    [SerializeField] private string blockReason = "请先完成桌面锚定并锁定，再进入下一步。";

    public bool CanProceed()
    {
        return lockToggle != null && lockToggle.IsLocked;
    }

    public string GetBlockReason()
    {
        return blockReason;
    }
}
