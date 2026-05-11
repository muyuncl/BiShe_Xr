using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 将 UI Toggle（Switch）绑定到 VR/MR 切换（PassthroughVRMRFader）。
/// isOn=true 表示进入 MR（Passthrough），false 表示 VR（虚拟环境）。
/// </summary>
[DisallowMultipleComponent]
public class VRMRSwitchToggleBinder : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private PassthroughVRMRFader fader;

    [Tooltip("启动时把 Toggle 同步到当前 MR 状态（若能取到）。")]
    [SerializeField] private bool syncFromFaderOnStart = true;

    [Header("VR/MR 时显隐（可选）")]
    [Tooltip("进入 MR 时禁用这些 VR 场景根物体（展厅/灯光/天空盒等）。")]
    [SerializeField] private GameObject[] disableWhenMixedRealityOn;

    [Tooltip("进入 MR 时启用这些物体（可选）。")]
    [SerializeField] private GameObject[] enableWhenMixedRealityOn;

    private bool _suppress;

    private void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();
        if (fader == null)
            fader = FindFirstObjectByType<PassthroughVRMRFader>();

        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        if (fader != null)
            fader.MixedRealityActiveChanged.AddListener(OnMixedRealityActiveChanged);
    }

    private void Start()
    {
        if (toggle == null)
            return;

        if (syncFromFaderOnStart && fader != null)
        {
            _suppress = true;
            toggle.isOn = fader.IsMixedRealityActive;
            _suppress = false;
            ApplyActiveStateForMode(toggle.isOn);
        }
        else if (fader != null)
        {
            fader.SmoothSetMixedReality(toggle.isOn);
        }
        else
        {
            ApplyActiveStateForMode(toggle.isOn);
        }
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
        if (fader != null)
            fader.MixedRealityActiveChanged.RemoveListener(OnMixedRealityActiveChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (_suppress)
            return;

        if (fader != null)
        {
            fader.SmoothSetMixedReality(isOn);
        }
        else
        {
            Debug.LogWarning("[VRMRSwitchToggleBinder] 未绑定 PassthroughVRMRFader。", this);
            ApplyActiveStateForMode(isOn);
        }
    }

    private void OnMixedRealityActiveChanged(bool mixedRealityOn)
    {
        if (toggle != null && toggle.isOn != mixedRealityOn)
        {
            _suppress = true;
            toggle.isOn = mixedRealityOn;
            _suppress = false;
        }
        ApplyActiveStateForMode(mixedRealityOn);
    }

    private void ApplyActiveStateForMode(bool mixedRealityOn)
    {
        // MR 开：禁用 VR 场景；VR 开：恢复
        ApplyActive(disableWhenMixedRealityOn, !mixedRealityOn);
        ApplyActive(enableWhenMixedRealityOn, mixedRealityOn);
    }

    private static void ApplyActive(GameObject[] list, bool active)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null)
                list[i].SetActive(active);
        }
    }
}

