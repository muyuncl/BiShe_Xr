using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 将毛玻璃参数写入同节点上 <see cref="Graphic"/> 的材质实例（与 <c>UI/World Space Glass</c> 配套）。
/// 方向光感、折射、深度、色散、磨砂、扩散均可独立调节。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
[ExecuteAlways]
public class WorldSpaceGlassPanel : MonoBehaviour
{
    [SerializeField]
    Graphic targetGraphic;

    [Header("光")]
    [Tooltip("高光/色散参考方向（度），沿表面切平面旋转。")]
    [Range(-180f, 180f)]
    public float lightAngle = -51f;

    [Tooltip("镜面高光强度（0–100%）。")]
    [Range(0f, 100f)]
    public float lightIntensityPercent = 35f;

    [Header("折射")]
    [Tooltip("折射强度（0–1）。")]
    [Range(0f, 1f)]
    public float refraction = 0.12f;

    [Header("深度")]
    [Tooltip("视差/折射随深度的放大系数。")]
    [Range(0f, 2f)]
    public float depth = 0.35f;

    [Header("色散")]
    [Tooltip("RGB 分离强度（0–10）。")]
    [Range(0f, 10f)]
    public float dispersion = 2.5f;

    [Header("磨砂")]
    [Tooltip("磨砂模糊（0–100%）。")]
    [Range(0f, 100f)]
    public float frostPercent = 45f;

    [Header("扩散")]
    [Tooltip("模糊采样与色散的扩散半径倍率。")]
    [Range(0f, 3f)]
    public float splay = 1f;

    [Header("底色")]
    public Color tint = new Color(1f, 1f, 1f, 0.65f);

    static readonly int IdLightAngle = Shader.PropertyToID("_LightAngle");
    static readonly int IdLightIntensity = Shader.PropertyToID("_LightIntensity");
    static readonly int IdRefraction = Shader.PropertyToID("_Refraction");
    static readonly int IdDepthScale = Shader.PropertyToID("_DepthScale");
    static readonly int IdDispersion = Shader.PropertyToID("_Dispersion");
    static readonly int IdFrost = Shader.PropertyToID("_Frost");
    static readonly int IdSplay = Shader.PropertyToID("_Splay");
    static readonly int IdColor = Shader.PropertyToID("_Color");

    void OnEnable()
    {
        ResolveGraphic();
        Apply();
    }

    void OnValidate()
    {
        ResolveGraphic();
        Apply();
    }

    void LateUpdate()
    {
        Apply();
    }

    void ResolveGraphic()
    {
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();
    }

    void Apply()
    {
        if (targetGraphic == null)
            return;

        Material m = targetGraphic.material;
        if (m == null)
            return;

        if (!m.HasProperty(IdLightAngle))
            return;

        m.SetFloat(IdLightAngle, lightAngle);
        m.SetFloat(IdLightIntensity, lightIntensityPercent * 0.01f);
        m.SetFloat(IdRefraction, refraction);
        m.SetFloat(IdDepthScale, depth);
        m.SetFloat(IdDispersion, dispersion);
        m.SetFloat(IdFrost, frostPercent * 0.01f);
        m.SetFloat(IdSplay, splay);
        m.SetColor(IdColor, tint);
    }
}
