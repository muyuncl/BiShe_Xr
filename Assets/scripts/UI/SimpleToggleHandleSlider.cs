using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 极简 Toggle “滑块”视觉：根据 isOn 把 handle 的 anchoredPosition.x 在 onX/offX 间切换。
/// </summary>
[DisallowMultipleComponent]
public class SimpleToggleHandleSlider : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private RectTransform handle;
    [SerializeField] private Image trackImage;
    [SerializeField] private Image handleImage;
    [SerializeField] private float onX = 14f;
    [SerializeField] private float offX = 2f;
    [SerializeField] private Color onTrackColor = new Color(0.19f, 0.49f, 0.93f, 1f);
    [SerializeField] private Color offTrackColor = new Color(0.33f, 0.33f, 0.37f, 1f);
    [SerializeField] private Color handleColor = Color.white;

    public void Init(RectTransform handleRt, Image track, Image handleImg, float onX, float offX)
    {
        handle = handleRt;
        trackImage = track;
        handleImage = handleImg;
        this.onX = onX;
        this.offX = offX;
    }

    private void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();
        if (trackImage == null)
            trackImage = GetComponent<Image>();
        if (handleImage == null && handle != null)
            handleImage = handle.GetComponent<Image>();
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(Apply);
            toggle.onValueChanged.AddListener(Apply);
        }
    }

    private void Start()
    {
        if (toggle != null)
            Apply(toggle.isOn);
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(Apply);
    }

    private void Apply(bool isOn)
    {
        if (handle == null)
            return;
        var p = handle.anchoredPosition;
        p.x = isOn ? onX : offX;
        handle.anchoredPosition = p;

        if (trackImage != null)
            trackImage.color = isOn ? onTrackColor : offTrackColor;
        if (handleImage != null)
            handleImage.color = handleColor;
    }
}

