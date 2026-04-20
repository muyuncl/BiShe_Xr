using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// XR Passthrough 开关控制器。
/// 用法：把按钮和 Passthrough 组件（如 OVRPassthroughLayer）拖入即可。
/// </summary>
public class XRPassthroughToggleController : MonoBehaviour
{
    [Header("按钮与显示")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private string onText = "Passthrough: ON";
    [SerializeField] private string offText = "Passthrough: OFF";

    [Header("Passthrough 组件（可选）")]
    [Tooltip("拖入实际负责透传的组件（例如 OVRPassthroughLayer）。")]
    [SerializeField] private Behaviour passthroughBehaviour;

    [Header("可选：切换时显隐对象")]
    [SerializeField] private GameObject[] hideWhenPassthroughOn;
    [SerializeField] private GameObject[] showWhenPassthroughOn;

    [Header("初始化")]
    [SerializeField] private bool startPassthroughOn;

    private bool isPassthroughOn;
    private bool warnedMissingBehaviour;

    private void Awake()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(TogglePassthrough);
            toggleButton.onClick.AddListener(TogglePassthrough);
        }

        SetPassthrough(startPassthroughOn);
    }

    public void TogglePassthrough()
    {
        SetPassthrough(!isPassthroughOn);
    }

    public void SetPassthrough(bool enabledState)
    {
        isPassthroughOn = enabledState;

        if (passthroughBehaviour != null)
        {
            passthroughBehaviour.enabled = isPassthroughOn;
        }
        else if (!warnedMissingBehaviour)
        {
            warnedMissingBehaviour = true;
            Debug.LogWarning("[XRPassthroughToggleController] 未绑定 Passthrough 组件，当前只会更新 UI 状态。");
        }

        ApplyActiveState(hideWhenPassthroughOn, !isPassthroughOn);
        ApplyActiveState(showWhenPassthroughOn, isPassthroughOn);

        if (stateText != null)
            stateText.text = isPassthroughOn ? onText : offText;
    }

    public bool IsPassthroughOn() => isPassthroughOn;

    private static void ApplyActiveState(GameObject[] list, bool active)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null)
                list[i].SetActive(active);
        }
    }
}
