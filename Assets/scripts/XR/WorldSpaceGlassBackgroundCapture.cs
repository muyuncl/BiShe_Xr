using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 在指定 <see cref="Camera"/> 上通过 <see cref="CommandBuffer"/> 将当前帧颜色缓冲复制到全局纹理
/// <c>_WorldSpaceGlass_BG</c>，供 <c>UI/World Space Glass</c> 着色器采样（无需 GrabPass）。
/// 请将本组件挂在实际渲染 World Space UI 的那台相机上（例如 XR rig 上的主相机）。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class WorldSpaceGlassBackgroundCapture : MonoBehaviour
{
    [Tooltip("背景缓冲的宽高缩小倍数（越大越快、越糊）。")]
    [Min(1)]
    public int downsample = 1;

    [Tooltip("抓取时机：Opaque 之后、透明 UI 之前，便于毛玻璃看到身后已绘制的场景。")]
    public CameraEvent captureEvent = CameraEvent.AfterForwardOpaque;

    static readonly int GlobalBgId = Shader.PropertyToID("_WorldSpaceGlass_BG");

    Camera _cam;
    CommandBuffer _cmd;
    RenderTexture _rt;
    CameraEvent _boundEvent;
    int _lastW;
    int _lastH;
    int _lastDownsample = -1;

    void OnEnable()
    {
        _cam = GetComponent<Camera>();
        RebuildIfNeeded(force: true);
    }

    void OnDisable()
    {
        Teardown();
    }

    void LateUpdate()
    {
        if (!isActiveAndEnabled || _cam == null)
            return;
        RebuildIfNeeded(force: false);
    }

    void OnValidate()
    {
        _cam = GetComponent<Camera>();
        if (isActiveAndEnabled)
            RebuildIfNeeded(force: true);
    }

    void RebuildIfNeeded(bool force)
    {
        if (_cam == null)
            return;

        int w = Mathf.Max(1, _cam.pixelWidth / Mathf.Max(1, downsample));
        int h = Mathf.Max(1, _cam.pixelHeight / Mathf.Max(1, downsample));

        if (!force && _rt != null && w == _lastW && h == _lastH && downsample == _lastDownsample && _cmd != null &&
            _boundEvent == captureEvent)
            return;

        Teardown();

        _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
        {
            name = "WorldSpaceGlass_BG",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
        };
        _rt.Create();

        _cmd = new CommandBuffer { name = "WorldSpaceGlass Capture" };
        _cmd.Blit(BuiltinRenderTextureType.CameraTarget, _rt);
        _cmd.SetGlobalTexture(GlobalBgId, _rt);

        _boundEvent = captureEvent;
        _cam.AddCommandBuffer(_boundEvent, _cmd);

        _lastW = w;
        _lastH = h;
        _lastDownsample = downsample;
    }

    void Teardown()
    {
        if (_cam != null && _cmd != null)
            _cam.RemoveCommandBuffer(_boundEvent, _cmd);

        if (_cmd != null)
        {
            _cmd.Release();
            _cmd = null;
        }

        if (_rt != null)
        {
            _rt.Release();
            if (Application.isPlaying)
                Destroy(_rt);
            else
                DestroyImmediate(_rt);
            _rt = null;
        }
    }
}
