using UnityEngine;

/// <summary>
/// 让目标物体在运行时保持与相机的相对位姿一致。
/// 典型用途：保证编辑器预览和XR运行后，相机到UI/物体的相对关系一致。
/// </summary>
public class XRRelativePlacement : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform target;

    [Header("相对位姿（相机局部空间）")]
    [SerializeField] private Vector3 localPositionOffset = new Vector3(0f, 0f, 1f);
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;

    [Header("自动采样（推荐）")]
    [Tooltip("运行开始时，自动以“当前场景里 target 相对 cameraTransform 的位姿”采样 offset（不需要手动点击 Capture）。")]
    [SerializeField] private bool autoCaptureOffsetOnStart = true;

    [Header("应用时机")]
    [Tooltip("运行开始时应用一次。")]
    [SerializeField] private bool applyOnStart = true;
    [Tooltip("每帧持续应用（物体会一直跟随相机）。")]
    [SerializeField] private bool followContinuously = false;

    [Header("XR 启动延迟（推荐）")]
    [Tooltip("等待若干帧再首次应用，避免 Start 时相机姿态尚未更新导致“预览与运行不一致”。")]
    [SerializeField] private int startDelayFrames = 2;

    [Header("平滑与防抖（连续跟随时生效）")]
    [Tooltip("位置变化小于该阈值（米）时不更新目标位姿。")]
    [SerializeField] private float positionDeadzone = 0.0015f;
    [Tooltip("位置平滑时间（秒）。0 表示不平滑。")]
    [SerializeField] private float positionSmoothTime = 0.06f;
    [Tooltip("旋转平滑速度（越大越跟手）。0 表示不平滑。")]
    [SerializeField] private float rotationLerpSpeed = 12f;

    private Vector3 smoothVelocity;
    private bool started;

    private void Awake()
    {
        ResolveRefs();
    }

    private void Start()
    {
        started = true;
        if (autoCaptureOffsetOnStart)
            CaptureCurrentAsOffset();
        if (applyOnStart)
            StartCoroutine(ApplyAfterDelay());
    }

    private void LateUpdate()
    {
        if (followContinuously)
            ApplyPlacement(smoothed: true);
    }

    [ContextMenu("Capture Current As Offset")]
    public void CaptureCurrentAsOffset()
    {
        ResolveRefs();
        if (cameraTransform == null || target == null)
        {
            Debug.LogWarning("[XRRelativePlacement] 缺少 cameraTransform 或 target，无法采样偏移。");
            return;
        }

        localPositionOffset = cameraTransform.InverseTransformPoint(target.position);
        Quaternion relativeRot = Quaternion.Inverse(cameraTransform.rotation) * target.rotation;
        localEulerOffset = relativeRot.eulerAngles;
    }

    [ContextMenu("Apply Placement Now")]
    public void ApplyPlacement()
    {
        ApplyPlacement(smoothed: false);
    }

    private void ResolveRefs()
    {
        if (target == null)
            target = transform;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private System.Collections.IEnumerator ApplyAfterDelay()
    {
        ResolveRefs();
        if (cameraTransform == null || target == null)
            yield break;

        int frames = Mathf.Max(0, startDelayFrames);
        for (int i = 0; i < frames; i++)
            yield return null;

        // 再等一帧 EndOfFrame，尽量避开 XR late-latching 的初始化阶段
        yield return new WaitForEndOfFrame();
        ApplyPlacement(smoothed: false);
    }

    private void ApplyPlacement(bool smoothed)
    {
        ResolveRefs();
        if (cameraTransform == null || target == null)
            return;

        Vector3 desiredPos = cameraTransform.TransformPoint(localPositionOffset);
        Quaternion desiredRot = cameraTransform.rotation * Quaternion.Euler(localEulerOffset);

        if (!smoothed || !started)
        {
            target.SetPositionAndRotation(desiredPos, desiredRot);
            smoothVelocity = Vector3.zero;
            return;
        }

        // 防抖：位置变化太小则不更新（避免轻微追踪噪声导致 UI 抖动）
        if ((desiredPos - target.position).magnitude < positionDeadzone)
            desiredPos = target.position;

        if (positionSmoothTime <= 0f)
        {
            target.position = desiredPos;
        }
        else
        {
            target.position = Vector3.SmoothDamp(
                target.position,
                desiredPos,
                ref smoothVelocity,
                Mathf.Max(0.0001f, positionSmoothTime));
        }

        if (rotationLerpSpeed <= 0f)
        {
            target.rotation = desiredRot;
        }
        else
        {
            float t = 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);
            target.rotation = Quaternion.Slerp(target.rotation, desiredRot, t);
        }
    }
}
