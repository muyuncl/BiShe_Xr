using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// VR（虚拟场景）与 MR（透视 Underlay）平滑切换：与 Meta MR Motifs PassthroughFader 相同思路——
/// Center Eye 下挂带 BiShe/PassthroughVRMRFader 材质的球体 + OVRPassthroughLayer（Underlay）。
/// UI 按钮可绑定 <see cref="ToggleSmooth"/> 或 <see cref="SmoothSetMixedReality"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class PassthroughVRMRFader : MonoBehaviour
{
    public enum FadeDirectionStyle
    {
        TextureNormal = 0,
        RightToLeft = 1,
        TopToBottom = 2,
        InsideOut = 3,
    }

    private const float AlphaTolerance = 0.001f;

    private static readonly int InvertedAlphaId = Shader.PropertyToID("_InvertedAlpha");
    private static readonly int FadeDirectionId = Shader.PropertyToID("_FadeDirection");

    [Header("References")]
    [SerializeField] private Camera xrCamera;
    [SerializeField] private OVRPassthroughLayer passthroughLayer;
    [Tooltip("通常与本脚本在同一物体上：覆盖视野的球体 MeshRenderer。")]
    [SerializeField] private MeshRenderer faderSphereRenderer;

    [Header("Fade")]
    [SerializeField] private float fadeSpeed = 1f;
    [SerializeField] private FadeDirectionStyle fadeDirection = FadeDirectionStyle.TopToBottom;
    [SerializeField] private bool startInMixedReality;

    [Tooltip("若透视层迟迟未触发 resumed（例如部分编辑器环境），在此秒数后开始淡入。")]
    [SerializeField] private float resumeFallbackSeconds = 2f;

    [Header("Events")]
    [SerializeField] private UnityEvent onFadeOutStarted;
    [SerializeField] private UnityEvent onFadeInStarted;
    [SerializeField] private UnityEvent onFadeOutCompleted;
    [SerializeField] private UnityEvent onFadeInCompleted;
    [Tooltip("每次过渡结束：true = 已进入 MR，false = 已进入 VR")]
    [SerializeField] private UnityEvent<bool> onMixedRealityActiveChanged;

    /// <summary>代码订阅用；Inspector 仍绑定上方同名 UnityEvent。</summary>
    public UnityEvent<bool> MixedRealityActiveChanged => onMixedRealityActiveChanged;

    private Material _material;
    private Color _savedBgColor;
    private CameraClearFlags _savedClearFlags;
    private bool _pendingFadeToMixedReality;
    private bool _inTransition;
    private Coroutine _fallbackRoutine;

    public bool IsMixedRealityActive =>
        passthroughLayer != null && passthroughLayer.enabled &&
        _material != null && Mathf.Approximately(_material.GetFloat(InvertedAlphaId), 1f);

    private void Awake()
    {
        if (xrCamera == null)
            xrCamera = GetComponentInParent<Camera>();

        if (faderSphereRenderer == null)
            faderSphereRenderer = GetComponent<MeshRenderer>();

        if (xrCamera != null)
        {
            _savedClearFlags = xrCamera.clearFlags;
            _savedBgColor = xrCamera.backgroundColor;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
#endif

        if (faderSphereRenderer != null)
        {
            _material = faderSphereRenderer.material;
            ApplyFadeDirectionToMaterial();
            ScaleSphereForUnderlay();
            faderSphereRenderer.enabled = false;
            _material.SetFloat(InvertedAlphaId, 0f);
        }

        if (passthroughLayer != null)
            passthroughLayer.passthroughLayerResumed.AddListener(OnPassthroughLayerResumed);

        if (_material == null || passthroughLayer == null)
            Debug.LogError("[PassthroughVRMRFader] 请指定 xrCamera / passthroughLayer / faderSphereRenderer。", this);

        if (!startInMixedReality && passthroughLayer != null)
        {
            passthroughLayer.enabled = false;
            RestoreCameraForVirtualReality();
        }
    }

    private void Start()
    {
        if (_material == null || passthroughLayer == null)
            return;

        if (startInMixedReality)
        {
            _pendingFadeToMixedReality = true;
            passthroughLayer.enabled = true;
            if (_fallbackRoutine != null)
                StopCoroutine(_fallbackRoutine);
            _fallbackRoutine = StartCoroutine(CoResumeFallback());
        }
    }

    private void RestoreCameraForVirtualReality()
    {
        if (xrCamera == null)
            return;
        xrCamera.clearFlags = _savedClearFlags;
        xrCamera.backgroundColor = _savedBgColor;
    }

    private void OnDestroy()
    {
        if (passthroughLayer != null)
            passthroughLayer.passthroughLayerResumed.RemoveListener(OnPassthroughLayerResumed);
    }

    /// <summary>在 MR / VR 之间平滑切换。</summary>
    public void ToggleSmooth()
    {
        if (_material == null || passthroughLayer == null || _inTransition)
            return;

        if (IsMixedRealityActive)
            BeginFadeToVirtualReality();
        else
            BeginFadeToMixedReality();
    }

    /// <summary>平滑切换到 MR（true）或 VR（false）。已在目标模式且无过渡中时忽略。</summary>
    public void SmoothSetMixedReality(bool mixedReality)
    {
        if (_material == null || passthroughLayer == null || _inTransition)
            return;

        if (mixedReality && !IsMixedRealityActive)
            BeginFadeToMixedReality();
        else if (!mixedReality && IsMixedRealityActive)
            BeginFadeToVirtualReality();
    }

    private void ScaleSphereForUnderlay()
    {
        if (xrCamera == null || faderSphereRenderer == null)
            return;
        float r = Mathf.Max(0.01f, xrCamera.farClipPlane - 0.01f);
        transform.localScale = new Vector3(r, r, r);
    }

    private void ApplyFadeDirectionToMaterial()
    {
        if (_material != null)
            _material.SetInt(FadeDirectionId, (int)fadeDirection);
    }

    private void OnPassthroughLayerResumed(OVRPassthroughLayer _)
    {
        if (!_pendingFadeToMixedReality || _material == null || passthroughLayer == null)
            return;

        if (_fallbackRoutine != null)
        {
            StopCoroutine(_fallbackRoutine);
            _fallbackRoutine = null;
        }

        _pendingFadeToMixedReality = false;
        StartCoroutine(FadeAlphaTowards(1f, isFadeInToMixedReality: true));
    }

    private IEnumerator CoResumeFallback()
    {
        float t = 0f;
        while (t < resumeFallbackSeconds && _pendingFadeToMixedReality)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!_pendingFadeToMixedReality)
            yield break;

        _pendingFadeToMixedReality = false;
        Debug.LogWarning("[PassthroughVRMRFader] passthroughLayerResumed 未及时触发，使用回退并开始淡入。", this);
        StartCoroutine(FadeAlphaTowards(1f, isFadeInToMixedReality: true));
        _fallbackRoutine = null;
    }

    private void BeginFadeToMixedReality()
    {
        ApplyFadeDirectionToMaterial();
        onFadeInStarted?.Invoke();
        _pendingFadeToMixedReality = true;
        passthroughLayer.enabled = true;

        if (_fallbackRoutine != null)
            StopCoroutine(_fallbackRoutine);
        _fallbackRoutine = StartCoroutine(CoResumeFallback());
    }

    private void BeginFadeToVirtualReality()
    {
        _pendingFadeToMixedReality = false;
        if (_fallbackRoutine != null)
        {
            StopCoroutine(_fallbackRoutine);
            _fallbackRoutine = null;
        }

        ApplyFadeDirectionToMaterial();
        onFadeOutStarted?.Invoke();

        if (xrCamera != null)
        {
            faderSphereRenderer.enabled = true;
            xrCamera.clearFlags = CameraClearFlags.Skybox;
            xrCamera.backgroundColor = _savedBgColor;
        }

        StartCoroutine(FadeAlphaTowards(0f, isFadeInToMixedReality: false));
    }

    private IEnumerator FadeAlphaTowards(float targetAlpha, bool isFadeInToMixedReality)
    {
        _inTransition = true;

        if (isFadeInToMixedReality && faderSphereRenderer != null)
            faderSphereRenderer.enabled = true;

        float current = _material.GetFloat(InvertedAlphaId);
        while (Mathf.Abs(current - targetAlpha) > AlphaTolerance)
        {
            current = Mathf.MoveTowards(current, targetAlpha, fadeSpeed * Time.deltaTime);
            _material.SetFloat(InvertedAlphaId, current);
            yield return null;
        }

        _material.SetFloat(InvertedAlphaId, targetAlpha);

        if (Mathf.Abs(targetAlpha - 1f) < AlphaTolerance)
        {
            if (xrCamera != null)
            {
                xrCamera.clearFlags = CameraClearFlags.SolidColor;
                xrCamera.backgroundColor = Color.clear;
            }

            onFadeInCompleted?.Invoke();
            onMixedRealityActiveChanged?.Invoke(true);
        }
        else
        {
            passthroughLayer.enabled = false;
            RestoreCameraForVirtualReality();

            onFadeOutCompleted?.Invoke();
            onMixedRealityActiveChanged?.Invoke(false);
        }

        if (faderSphereRenderer != null)
            faderSphereRenderer.enabled = false;

        _inTransition = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        fadeSpeed = Mathf.Max(0.01f, fadeSpeed);
        resumeFallbackSeconds = Mathf.Max(0.05f, resumeFallbackSeconds);
    }
#endif
}
