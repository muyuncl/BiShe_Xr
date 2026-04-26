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
    [SerializeField] private MonoBehaviour placementController;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI stateText;

    [Header("文案")]
    [SerializeField] private string editModeText = "桌面锚点: 编辑中";
    [SerializeField] private string lockedModeText = "桌面锚点: 已锁定";

    [Header("初始化")]
    [Tooltip("true=启动即锁定，false=启动即可编辑")]
    [SerializeField] private bool startLocked = false;
    [SerializeField] private float toggleDebounceSeconds = 0.2f;

    private float lastToggleTime = -999f;

    public bool IsLocked
    {
        get
        {
            var controller = GetController();
            return controller != null && !controller.PlacementEnabled;
        }
    }

    private void Awake()
    {
        if (toggleButton != null)
        {
            // 若按钮已在 Inspector 的 OnClick 里绑定了 ToggleLockState，就不要重复 AddListener。
            // 否则一次点击会触发两次切换（锁定->解锁），表现为“看起来没有变化”。
            bool hasPersistent = toggleButton.onClick.GetPersistentEventCount() > 0;
            if (!hasPersistent)
            {
                toggleButton.onClick.RemoveListener(ToggleLockState);
                toggleButton.onClick.AddListener(ToggleLockState);
            }
        }

        var controller = GetController();
        if (controller != null)
            controller.SetPlacementEnabled(!startLocked);

        RefreshStateText();
    }

    public void ToggleLockState()
    {
        if (Time.unscaledTime - lastToggleTime < Mathf.Max(0f, toggleDebounceSeconds))
            return;
        lastToggleTime = Time.unscaledTime;

        var controller = GetController();
        if (controller == null) return;
        controller.TogglePlacementEnabled();
        RefreshStateText();
    }

    public void SetLocked(bool locked)
    {
        var controller = GetController();
        if (controller == null) return;
        controller.SetPlacementEnabled(!locked);
        RefreshStateText();
    }

    private void RefreshStateText()
    {
        if (stateText == null || GetController() == null) return;
        bool locked = IsLocked;
        stateText.text = locked ? lockedModeText : editModeText;
    }

    private IDeskAnchorPlacementController GetController()
    {
        if (placementController is IDeskAnchorPlacementController typedController)
            return typedController;

        if (placementController != null)
        {
            var components = placementController.GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IDeskAnchorPlacementController c)
                    return c;
            }
        }

        return null;
    }
}
