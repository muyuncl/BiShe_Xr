using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 锚点编辑/锁定切换器。
/// 锁定后禁止手势继续移动桌面锚点。
/// </summary>
public class DeskAnchorLockToggle : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private HandDeskAnchorPlacer anchorPlacer;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI stateText;

    [Header("文案")]
    [SerializeField] private string editModeText = "桌面锚点: 编辑中";
    [SerializeField] private string lockedModeText = "桌面锚点: 已锁定";

    [Header("初始化")]
    [Tooltip("true=启动即锁定，false=启动即可编辑")]
    [SerializeField] private bool startLocked = false;

    public bool IsLocked => anchorPlacer != null && !anchorPlacer.PlacementEnabled;

    private void Awake()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleLockState);
            toggleButton.onClick.AddListener(ToggleLockState);
        }

        if (anchorPlacer != null)
            anchorPlacer.SetPlacementEnabled(!startLocked);

        RefreshStateText();
    }

    public void ToggleLockState()
    {
        if (anchorPlacer == null) return;
        anchorPlacer.TogglePlacementEnabled();
        RefreshStateText();
    }

    public void SetLocked(bool locked)
    {
        if (anchorPlacer == null) return;
        anchorPlacer.SetPlacementEnabled(!locked);
        RefreshStateText();
    }

    private void RefreshStateText()
    {
        if (stateText == null || anchorPlacer == null) return;
        bool locked = IsLocked;
        stateText.text = locked ? lockedModeText : editModeText;
    }
}
